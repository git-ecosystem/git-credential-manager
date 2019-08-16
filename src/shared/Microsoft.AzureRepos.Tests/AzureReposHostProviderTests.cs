// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class AzureReposHostProviderTests
    {
        [Fact]
        public void AzureReposHostProvider_IsSupported_AzureHost_UnencryptedHttp_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());

            // We report that we support unencrypted HTTP here so that we can fail and
            // show a helpful error message in the call to `CreateCredentialAsync` instead.
            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void AzureReposHostProvider_IsSupported_VisualStudioHost_UnencryptedHttp_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "org.visualstudio.com",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());

            // We report that we support unencrypted HTTP here so that we can fail and
            // show a helpful error message in the call to `CreateCredentialAsync` instead.
            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void AzureReposHostProvider_IsSupported_AzureHost_WithPath_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void AzureReposHostProvider_IsSupported_AzureHost_MissingPath_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void AzureReposHostProvider_IsSupported_VisualStudioHost_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "org.visualstudio.com",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void AzureReposHostProvider_IsSupported_VisualStudioHost_MissingOrgInHost_ReturnsFalse()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "visualstudio.com",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public void AzureReposHostProvider_IsSupported_NonAzureRepos_ReturnsFalse()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
                ["path"] = "org/proj/_git/repo",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public async Task AzureReposHostProvider_GetCredentialAsync_UnencryptedHttp_ThrowsException()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var context = new TestCommandContext();
            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var msAuth = Mock.Of<IMicrosoftAuthentication>();
            var authorityCache = Mock.Of<IAzureReposAuthorityCache>();

            var provider = new AzureReposHostProvider(context, azDevOps, msAuth, authorityCache);

            await Assert.ThrowsAsync<Exception>(() => provider.GetCredentialAsync(input));
        }

        [Fact]
        public async Task AzureReposHostProvider_GetCredentialAsync_ReturnsCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var remoteUri = new Uri("https://dev.azure.com/org/proj/_git/repo");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedClientId = AzureDevOpsConstants.AadClientId;
            var expectedRedirectUri = AzureDevOpsConstants.AadRedirectUri;
            var expectedResource = AzureDevOpsConstants.AadResourceId;
            var accessToken = "ACCESS-TOKEN";
            var personalAccessToken = "PERSONAL-ACCESS-TOKEN";

            var context = new TestCommandContext();

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>();
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri))
                        .ReturnsAsync(authorityUrl);
            azDevOpsMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedOrgUri, accessToken, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(personalAccessToken);

            var msAuthMock = new Mock<IMicrosoftAuthentication>();
            msAuthMock.Setup(x => x.GetAccessTokenAsync(authorityUrl, expectedClientId, expectedRedirectUri, expectedResource, remoteUri))
                      .ReturnsAsync(accessToken);

            var authorityCache = Mock.Of<IAzureReposAuthorityCache>();

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCache);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(personalAccessToken, credential.Password);
            // We don't care about the username value
        }

        [Fact]
        public async Task AzureReposHostProvider_GetCredentialAsync_UsesAuthorityCache()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var expectedAuthority = "https://login.microsoftonline.com/common";

            var context = new TestCommandContext();

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>();
            var msAuthMock = new Mock<IMicrosoftAuthentication>();

            var authorityCacheMock = new Mock<IAzureReposAuthorityCache>();
            authorityCacheMock.Setup(x => x.GetAuthority(It.IsAny<string>()))
                              .Returns(expectedAuthority);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object);

            await provider.GetCredentialAsync(input);

            authorityCacheMock.Verify(x => x.GetAuthority("org"), Times.Once);
            authorityCacheMock.VerifyNoOtherCalls();
            azDevOpsMock.Verify(x => x.GetAuthorityAsync(It.IsAny<Uri>()), Times.Never);
        }

        [Fact]
        public async Task AzureReposHostProvider_GetCredentialAsync_UpdatesAuthorityCache()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var orgUri = new Uri("https://dev.azure.com/org");
            var expectedAuthority = "https://login.microsoftonline.com/common";

            var context = new TestCommandContext();

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>();
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(orgUri))
                        .ReturnsAsync(expectedAuthority);

            var msAuthMock = new Mock<IMicrosoftAuthentication>();

            var authorityCacheMock = new Mock<IAzureReposAuthorityCache>();
            authorityCacheMock.Setup(x => x.GetAuthority(It.IsAny<string>()))
                              .Returns((string) null);
            authorityCacheMock.Setup(x => x.UpdateAuthority("org", expectedAuthority))
                              .Verifiable();

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object);

            await provider.GetCredentialAsync(input);

            authorityCacheMock.Verify(x => x.UpdateAuthority("org", expectedAuthority), Times.Once);
        }

        [Fact]
        public async Task AzureReposHostProvider_EraseCredentialAsync_ErasesAuthorityCache()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var context = new TestCommandContext();

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>();
            var msAuthMock = new Mock<IMicrosoftAuthentication>();
            var authorityCacheMock = new Mock<IAzureReposAuthorityCache>();

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object);

            await provider.EraseCredentialAsync(input);

            authorityCacheMock.Verify(x => x.EraseAuthority("org"), Times.Once);
        }
    }
}
