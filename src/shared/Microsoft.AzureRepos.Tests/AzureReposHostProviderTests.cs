using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Tests;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class AzureReposHostProviderTests
    {
        private static readonly string HelperKey =
            $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
        private static readonly string AzDevUseHttpPathKey =
            $"{Constants.GitConfiguration.Credential.SectionName}.https://dev.azure.com.{Constants.GitConfiguration.Credential.UseHttpPath}";
        private static readonly string OrgName = "org";

        [Fact]
        public void AzureReposProvider_IsSupported_AzureHost_UnencryptedHttp_ReturnsTrue()
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
        public void AzureReposProvider_IsSupported_VisualStudioHost_UnencryptedHttp_ReturnsTrue()
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
        public void AzureReposProvider_IsSupported_AzureHost_WithPath_ReturnsTrue()
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
        public void AzureReposProvider_IsSupported_AzureHost_MissingPath_ReturnsTrue()
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
        public void AzureReposProvider_IsSupported_VisualStudioHost_ReturnsTrue()
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
        public void AzureReposProvider_IsSupported_VisualStudioHost_MissingOrgInHost_ReturnsFalse()
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
        public void AzureReposProvider_IsSupported_NonAzureRepos_ReturnsFalse()
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
        public async Task AzureReposProvider_GetCredentialAsync_UnencryptedHttp_ThrowsException()
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
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();

            var provider = new AzureReposHostProvider(context, azDevOps, msAuth, authorityCache, userMgr);

            await Assert.ThrowsAsync<Trace2Exception>(() => provider.GetCredentialAsync(input));
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_VsComUrlUser_ReturnsCredential()
        {
            var urlAccount = "jane.doe";

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "org.visualstudio.com",
                ["username"] = urlAccount
            });

            var expectedOrgUri = new Uri("https://org.visualstudio.com");
            var remoteUri = new Uri("https://org.visualstudio.com/");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedClientId = AzureDevOpsConstants.AadClientId;
            var expectedRedirectUri = AzureDevOpsConstants.AadRedirectUri;
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var authResult = CreateAuthResult(urlAccount, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock.Setup(x => x.GetTokenForUserAsync(authorityUrl, expectedClientId, expectedRedirectUri, expectedScopes, urlAccount, true))
                      .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object, userMgrMock.Object);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(urlAccount, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_DevAzureUrlUser_ReturnsCredential()
        {
            var urlAccount = "jane.doe";

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/project/_git/repo",
                ["username"] = urlAccount
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var remoteUri = new Uri("https://dev.azure.com/org/project/_git/repo");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedClientId = AzureDevOpsConstants.AadClientId;
            var expectedRedirectUri = AzureDevOpsConstants.AadRedirectUri;
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var authResult = CreateAuthResult(urlAccount, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock.Setup(x => x.GetTokenForUserAsync(authorityUrl, expectedClientId, expectedRedirectUri, expectedScopes, urlAccount, true))
                      .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object, userMgrMock.Object);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(urlAccount, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_DevAzureUrlOrgName_ReturnsCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["username"] = "org"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedClientId = AzureDevOpsConstants.AadClientId;
            var expectedRedirectUri = AzureDevOpsConstants.AadRedirectUri;
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var account = "jane.doe";
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock.Setup(x => x.GetTokenForUserAsync(authorityUrl, expectedClientId, expectedRedirectUri, expectedScopes, null, true))
                      .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);
            userMgrMock.Setup(x => x.GetBinding(OrgName)).Returns((AzureReposBinding)null);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object, userMgrMock.Object);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_NoUser_ReturnsCredential()
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
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var account = "john.doe";
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock.Setup(x => x.GetTokenForUserAsync(authorityUrl, expectedClientId, expectedRedirectUri, expectedScopes, null, true))
                      .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);
            userMgrMock.Setup(x => x.GetBinding(OrgName)).Returns((AzureReposBinding)null);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object, userMgrMock.Object);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_BoundUser_ReturnsCredential()
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
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var account = "john.doe";
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock.Setup(x => x.GetTokenForUserAsync(authorityUrl, expectedClientId, expectedRedirectUri, expectedScopes, account, true))
                      .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);
            userMgrMock.Setup(x => x.GetBinding(OrgName))
                .Returns(new AzureReposBinding(OrgName, account, null));

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object, userMgrMock.Object);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_NoCachedAuthority_NoUser_ReturnsCredential()
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
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var account = "john.doe";
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock.Setup(x => x.GetTokenForUserAsync(authorityUrl, expectedClientId, expectedRedirectUri, expectedScopes, null, true))
                      .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(It.IsAny<string>())).Returns((string)null);
            authorityCacheMock.Setup(x => x.UpdateAuthority(OrgName, authorityUrl));

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);
            userMgrMock.Setup(x => x.GetBinding(OrgName)).Returns((AzureReposBinding)null);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object, userMgrMock.Object);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_PatMode_OrgInUserName_NoExistingPat_GeneratesCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["username"] = "org"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedClientId = AzureDevOpsConstants.AadClientId;
            var expectedRedirectUri = AzureDevOpsConstants.AadRedirectUri;
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var personalAccessToken = "PERSONAL-ACCESS-TOKEN";
            var account = "john.doe";
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);
            azDevOpsMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedOrgUri, accessToken, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(personalAccessToken);

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock.Setup(x => x.GetTokenForUserAsync(authorityUrl, expectedClientId, expectedRedirectUri, expectedScopes, null, true))
                      .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object, userMgrMock.Object);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(personalAccessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_PatMode_NoExistingPat_GeneratesCredential()
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
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var personalAccessToken = "PERSONAL-ACCESS-TOKEN";
            var account = "john.doe";
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);
            azDevOpsMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedOrgUri, accessToken, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(personalAccessToken);

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock.Setup(x => x.GetTokenForUserAsync(authorityUrl, expectedClientId, expectedRedirectUri, expectedScopes, null, true))
                      .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, msAuthMock.Object, authorityCacheMock.Object, userMgrMock.Object);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(personalAccessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_PatMode_ExistingPat_ReturnsExistingCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var remoteUri = new Uri("https://dev.azure.com/org/proj/_git/repo");
            var personalAccessToken = "PERSONAL-ACCESS-TOKEN";
            const string service = "https://dev.azure.com/org";
            const string account = "john.doe";

            var context = new TestCommandContext();

            context.CredentialStore.Add(service, account, personalAccessToken);

            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var msAuth = Mock.Of<IMicrosoftAuthentication>();
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();

            var provider = new AzureReposHostProvider(context, azDevOps, msAuth, authorityCache, userMgr);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(personalAccessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_ManagedIdentity_ReturnsManagedIdCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            const string accessToken = "MANAGED-IDENTITY-TOKEN";
            const string managedIdentity = "MANAGED-IDENTITY";

            var context = new TestCommandContext
            {
                Environment =
                {
                    Variables =
                    {
                        [AzureDevOpsConstants.EnvironmentVariables.ManagedIdentity] = managedIdentity
                    }
                }
            };

            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();
            var msAuthMock = new Mock<IMicrosoftAuthentication>();

            msAuthMock.Setup(x => x.GetTokenForManagedIdentityAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new MockMsAuthResult { AccessToken = accessToken });

            var provider = new AzureReposHostProvider(context, azDevOps, msAuthMock.Object, authorityCache, userMgr);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(managedIdentity, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            msAuthMock.Verify(
                x => x.GetTokenForManagedIdentityAsync(managedIdentity,
                    AzureDevOpsConstants.AzureDevOpsResourceId), Times.Once);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_ServicePrincipal_ReturnsSPCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            const string accessToken = "SP-TOKEN";
            const string tenantId = "78B1822F-107D-40A3-A29C-AB68D8066074";
            const string clientId = "49B4DC1A-58A8-4EEE-A81B-616A40D0BA64";
            const string servicePrincipal = $"{tenantId}/{clientId}";
            const string servicePrincipalSecret = "CLIENT-SECRET";

            var context = new TestCommandContext
            {
                Environment =
                {
                    Variables =
                    {
                        [AzureDevOpsConstants.EnvironmentVariables.ServicePrincipalId] = servicePrincipal,
                        [AzureDevOpsConstants.EnvironmentVariables.ServicePrincipalSecret] = servicePrincipalSecret
                    }
                }
            };

            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();
            var msAuthMock = new Mock<IMicrosoftAuthentication>();

            msAuthMock.Setup(x =>
                    x.GetTokenForServicePrincipalAsync(It.IsAny<ServicePrincipalIdentity>(), It.IsAny<string[]>()))
                .ReturnsAsync(new MockMsAuthResult { AccessToken = accessToken });

            var provider = new AzureReposHostProvider(context, azDevOps, msAuthMock.Object, authorityCache, userMgr);

            ICredential credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(clientId, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            msAuthMock.Verify(x => x.GetTokenForServicePrincipalAsync(
                It.Is<ServicePrincipalIdentity>(sp => sp.TenantId == tenantId && sp.Id == clientId),
                It.Is<string[]>(scopes => scopes.Length == 1 && scopes[0] == AzureDevOpsConstants.AzureDevOpsDefaultScopes[0])),
                Times.Once);
        }

        [Fact]
        public async Task AzureReposHostProvider_ConfigureAsync_UseHttpPathSetTrue_DoesNothing()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.Global[AzDevUseHttpPathKey] = new List<string> {"true"};

            await provider.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(AzDevUseHttpPathKey, out IList<string> actualValues));
            Assert.Single(actualValues);
            Assert.Equal("true", actualValues[0]);
        }

        [Fact]
        public async Task AzureReposHostProvider_ConfigureAsync_UseHttpPathSetFalse_SetsUseHttpPathTrue()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.Global[AzDevUseHttpPathKey] = new List<string> {"false"};

            await provider.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(AzDevUseHttpPathKey, out IList<string> actualValues));
            Assert.Single(actualValues);
            Assert.Equal("true", actualValues[0]);
        }

        [Fact]
        public async Task AzureReposHostProvider_ConfigureAsync_UseHttpPathUnset_SetsUseHttpPathTrue()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            await provider.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(AzDevUseHttpPathKey, out IList<string> actualValues));
            Assert.Single(actualValues);
            Assert.Equal("true", actualValues[0]);
        }

        [Fact]
        public async Task AzureReposHostProvider_UnconfigureAsync_UseHttpPathSet_RemovesEntry()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.Global[AzDevUseHttpPathKey] = new List<string> {"true"};

            await provider.UnconfigureAsync(ConfigurationTarget.User);

            Assert.Empty(context.Git.Configuration.Global);
        }

        [PlatformFact(Platforms.Windows)]
        public async Task AzureReposHostProvider_UnconfigureAsync_System_Windows_UseHttpPathSetAndManagerHelper_DoesNotRemoveEntry()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.System[HelperKey] = new List<string> {"manager"};
            context.Git.Configuration.System[AzDevUseHttpPathKey] = new List<string> {"true"};

            await provider.UnconfigureAsync(ConfigurationTarget.System);

            Assert.True(context.Git.Configuration.System.TryGetValue(AzDevUseHttpPathKey, out IList<string> actualValues));
            Assert.Single(actualValues);
            Assert.Equal("true", actualValues[0]);
        }

        [PlatformFact(Platforms.Windows)]
        public async Task AzureReposHostProvider_UnconfigureAsync_System_Windows_UseHttpPathSetAndManagerCoreHelper_DoesNotRemoveEntry()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.System[HelperKey] = new List<string> {"manager-core"};
            context.Git.Configuration.System[AzDevUseHttpPathKey] = new List<string> {"true"};

            await provider.UnconfigureAsync(ConfigurationTarget.System);

            Assert.True(context.Git.Configuration.System.TryGetValue(AzDevUseHttpPathKey, out IList<string> actualValues));
            Assert.Single(actualValues);
            Assert.Equal("true", actualValues[0]);
        }

        [PlatformFact(Platforms.Windows)]
        public async Task AzureReposHostProvider_UnconfigureAsync_System_Windows_UseHttpPathSetNoManagerCoreHelper_RemovesEntry()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.System[AzDevUseHttpPathKey] = new List<string> {"true"};

            await provider.UnconfigureAsync(ConfigurationTarget.System);

            Assert.Empty(context.Git.Configuration.System);
        }

        [PlatformFact(Platforms.Windows)]
        public async Task AzureReposHostProvider_UnconfigureAsync_User_Windows_UseHttpPathSetAndManagerHelper_RemovesEntry()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.Global[HelperKey] = new List<string> {"manager"};
            context.Git.Configuration.Global[AzDevUseHttpPathKey] = new List<string> {"true"};

            await provider.UnconfigureAsync(ConfigurationTarget.User);

            Assert.False(context.Git.Configuration.Global.TryGetValue(AzDevUseHttpPathKey, out _));
        }

        [PlatformFact(Platforms.Windows)]
        public async Task AzureReposHostProvider_UnconfigureAsync_User_Windows_UseHttpPathSetAndManagerCoreHelper_RemovesEntry()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.Global[HelperKey] = new List<string> {"manager-core"};
            context.Git.Configuration.Global[AzDevUseHttpPathKey] = new List<string> {"true"};

            await provider.UnconfigureAsync(ConfigurationTarget.User);

            Assert.False(context.Git.Configuration.Global.TryGetValue(AzDevUseHttpPathKey, out _));
        }

        [Theory]
        [InlineData(false, null, "")]
        [InlineData(false, null, "   ")]
        [InlineData(false, null, null)]
        [InlineData(false, null, "Basic realm=\"test\"")]
        [InlineData(false, null, "Basic realm=\"https://tfsprodwcus0.app.visualstudio.com/\"")]
        [InlineData(false, null, "TFS-Federated")]
        [InlineData(true, "https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3",
            "Bearer authorization_uri=https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3")]
        [InlineData(true, "https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3",
            "bEArEr auThORizAtIoN_uRi=https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3")]
        [InlineData(true, "https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3",
            "\"Bearer authorization_uri=https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3\"")]
        [InlineData(true, "https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3",
            "'Bearer authorization_uri=https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3'")]
        [InlineData(true, "https://login.microsoftonline.com/tenant1",
            "Bearer authorization_uri=https://login.microsoftonline.com/tenant1",
            "Bearer authorization_uri=https://login.microsoftonline.com/tenant2",
            "Bearer authorization_uri=https://login.microsoftonline.com/tenant3")]
        [InlineData(true, "https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3",
            "Bearer authorization_uri=https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3",
            "Basic realm=\"https://tfsprodwcus0.app.visualstudio.com/\"",
            "TFS-Federated")]
        [InlineData(true, "https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3",
            "TFS-Federated",
            "Basic realm=\"https://tfsprodwcus0.app.visualstudio.com/\"",
            "Bearer authorization_uri=https://login.microsoftonline.com/79c4d065-d599-442e-b0ea-c4ab36ad63c3")]
        public void AzureReposHostProvider_TryGetAuthorityFromHeaders(
            bool expectedResult, string expectedAuthority, params string[] headers)
        {
            bool actualResult = AzureReposHostProvider.TryGetAuthorityFromHeaders(headers, out string actualAuthority);

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedAuthority, actualAuthority);
        }

        private static IMicrosoftAuthenticationResult CreateAuthResult(string upn, string token)
        {
            return new MockMsAuthResult
            {
                AccountUpn = upn,
                AccessToken = token,
            };
        }

        private class MockMsAuthResult : IMicrosoftAuthenticationResult
        {
            public string AccessToken { get; set; }
            public string AccountUpn { get; set; }
            public string TokenSource { get; set; }
        }
    }
}
