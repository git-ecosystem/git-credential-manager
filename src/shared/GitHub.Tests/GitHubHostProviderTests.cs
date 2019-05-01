// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitHub.Tests
{
    public class GitHubHostProviderTests
    {
        [Fact]
        public void GitHubHostProvider_IsSupported_GitHubHost_UnencryptedHttp_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            // We report that we support unencrypted HTTP here so that we can fail and
            // show a helpful error message in the call to `CreateCredentialAsync` instead.
            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_GistHost_UnencryptedHttp_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "gist.github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            // We report that we support unencrypted HTTP here so that we can fail and
            // show a helpful error message in the call to `CreateCredentialAsync` instead.
            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_GitHubHost_Https_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_GistHost_Https_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "gist.github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_NonHttpHttps_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "ssh",
                ["host"] = "github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_NonGitHub_ReturnsFalse()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_GetCredentialKey_GitHubHost_ReturnsCorrectKey()
        {
            const string expectedKey = "https://github.com";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());
            string actualKey = provider.GetCredentialKey(input);
            Assert.Equal(expectedKey, actualKey);
        }

        [Fact]
        public void GitHubHostProvider_GetCredentialKey_GistHost_ReturnsCorrectKey()
        {
            const string expectedKey = "https://github.com";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "gist.github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());
            string actualKey = provider.GetCredentialKey(input);
            Assert.Equal(expectedKey, actualKey);
        }

        [Fact]
        public async Task GitHubHostProvider_CreateCredentialAsync_UnencryptedHttp_ThrowsException()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "github.com",
            });

            var context = new TestCommandContext();
            var ghApi = Mock.Of<IGitHubRestApi>();
            var ghAuth = Mock.Of<IGitHubAuthentication>();

            var provider = new GitHubHostProvider(context, ghApi, ghAuth);

            await Assert.ThrowsAsync<Exception>(() => provider.CreateCredentialAsync(input));
        }

        [Fact]
        public async Task GitHubHostProvider_CreateCredentialAsync_1FAOnly_ReturnsCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var expectedTargetUri = new Uri("https://github.com/");
            var expectedUserName = "john.doe";
            var expectedPassword = "letmein123";
            IEnumerable<string> expectedPatScopes = new[]
            {
                GitHubConstants.TokenScopes.Repo,
                GitHubConstants.TokenScopes.Gist
            };

            var patValue = "PERSONAL-ACCESS-TOKEN";
            var pat = new GitCredential(Constants.PersonalAccessTokenUserName, patValue);
            var response = new AuthenticationResult(GitHubAuthenticationResultType.Success, pat);

            var context = new TestCommandContext();

            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);
            ghAuthMock.Setup(x => x.GetCredentialsAsync(expectedTargetUri))
                      .ReturnsAsync(new GitCredential(expectedUserName, expectedPassword));

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.AcquireTokenAsync(expectedTargetUri, expectedUserName, expectedPassword, null, It.IsAny<IEnumerable<string>>()))
                     .ReturnsAsync(response);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            GitCredential credential = await provider.CreateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(Constants.PersonalAccessTokenUserName, credential.UserName);
            Assert.Equal(patValue, credential.Password);
        }

        [Fact]
        public async Task GitHubHostProvider_CreateCredentialAsync_2FARequired_ReturnsCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var expectedTargetUri = new Uri("https://github.com/");
            var expectedUserName = "john.doe";
            var expectedPassword = "letmein123";
            var expectedAuthCode = "123456";
            IEnumerable<string> expectedPatScopes = new[]
            {
                GitHubConstants.TokenScopes.Repo,
                GitHubConstants.TokenScopes.Gist
            };

            var patValue = "PERSONAL-ACCESS-TOKEN";
            var pat = new GitCredential(Constants.PersonalAccessTokenUserName, patValue);
            var response1 = new AuthenticationResult(GitHubAuthenticationResultType.TwoFactorApp);
            var response2 = new AuthenticationResult(GitHubAuthenticationResultType.Success, pat);

            var context = new TestCommandContext();

            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);
            ghAuthMock.Setup(x => x.GetCredentialsAsync(expectedTargetUri))
                      .ReturnsAsync(new GitCredential(expectedUserName, expectedPassword));
            ghAuthMock.Setup(x => x.GetAuthenticationCodeAsync(expectedTargetUri, false))
                      .ReturnsAsync(expectedAuthCode);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.AcquireTokenAsync(expectedTargetUri, expectedUserName, expectedPassword, null, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(response1);
            ghApiMock.Setup(x => x.AcquireTokenAsync(expectedTargetUri, expectedUserName, expectedPassword, expectedAuthCode, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(response2);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            GitCredential credential = await provider.CreateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(Constants.PersonalAccessTokenUserName, credential.UserName);
            Assert.Equal(patValue, credential.Password);
        }
    }
}
