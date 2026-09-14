using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.Entra;
using GitCredentialManager.Tests;
using GitCredentialManager.Tests.Objects;
using Microsoft.Identity.Client;
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
        private static readonly string AzDevUseSharedCacheKey =
            $"{Constants.GitConfiguration.Credential.SectionName}.{AzureDevOpsConstants.GitConfiguration.Credential.UseSharedCache}";
        private static readonly string OrgName = "org";

        [Fact]
        public void AzureReposProvider_GetUseSharedCache_NoConfiguration_ReturnsTrue()
        {
            var provider = new AzureReposHostProvider(new TestCommandContext());

            Assert.True(provider.GetUseSharedCache());
        }

        [Fact]
        public void AzureReposProvider_GetUseSharedCache_GitConfigFalse_ReturnsFalse()
        {
            var context = new TestCommandContext();
            context.Git.Configuration.Global[AzDevUseSharedCacheKey] = new List<string> {"false"};
            var provider = new AzureReposHostProvider(context);

            Assert.False(provider.GetUseSharedCache());
        }

        [Fact]
        public void AzureReposProvider_GetUseSharedCache_EnvironmentOverridesGitConfig()
        {
            var context = new TestCommandContext();
            context.Git.Configuration.Global[AzDevUseSharedCacheKey] = new List<string> {"false"};
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.UseSharedCache] = "true";
            var provider = new AzureReposHostProvider(context);

            Assert.True(provider.GetUseSharedCache());
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData(null, "oauth", false)]
        [InlineData(null, "OAUTH", false)]
        [InlineData(null, "pat", true)]
        [InlineData(null, "PAT", true)]
        [InlineData(null, "invalid", false)]
        [InlineData(null, "", false)]
        [InlineData(null, " ", false)]
        [InlineData("oauth", null, false)]
        [InlineData("OAUTH", null, false)]
        [InlineData("pat", null, true)]
        [InlineData("PAT", null, true)]
        [InlineData("invalid", null, false)]
        [InlineData("", null, false)]
        [InlineData(" ", null, false)]
        [InlineData("pat", "oauth", true)]
        [InlineData("oauth", "pat", false)]
        [InlineData("invalid", "pat", false)]
        [InlineData("", "pat", false)]
        public void AzureReposProvider_UsePersonalAccessTokens(
            string environmentValue, string gitConfigValue, bool expected)
        {
            var context = new TestCommandContext();
            if (gitConfigValue is not null)
            {
                string key =
                    $"{Constants.GitConfiguration.Credential.SectionName}.{AzureDevOpsConstants.GitConfiguration.Credential.CredentialType}";
                context.Git.Configuration.Global[key] = new List<string> {gitConfigValue};
            }
            if (environmentValue is not null)
            {
                context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                    environmentValue;
            }
            var provider = new AzureReposHostProvider(context);

            Assert.Equal(expected, provider.UsePersonalAccessTokens());
        }

        [Fact]
        public void AzureReposProvider_IsSupported_AzureHost_UnencryptedHttp_ReturnsTrue()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());

            // We report that we support unencrypted HTTP here so that we can fail and
            // show a helpful error message in the call to `CreateCredentialAsync` instead.
            Assert.True(provider.IsSupported(request));
        }

        [Fact]
        public void AzureReposProvider_IsSupported_VisualStudioHost_UnencryptedHttp_ReturnsTrue()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "org.visualstudio.com",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());

            // We report that we support unencrypted HTTP here so that we can fail and
            // show a helpful error message in the call to `CreateCredentialAsync` instead.
            Assert.True(provider.IsSupported(request));
        }

        [Fact]
        public void AzureReposProvider_IsSupported_AzureHost_WithPath_ReturnsTrue()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.True(provider.IsSupported(request));
        }

        [Fact]
        public void AzureReposProvider_IsSupported_AzureHost_MissingPath_ReturnsTrue()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.True(provider.IsSupported(request));
        }

        [Fact]
        public void AzureReposProvider_IsSupported_VisualStudioHost_ReturnsTrue()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "org.visualstudio.com",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.True(provider.IsSupported(request));
        }

        [Fact]
        public void AzureReposProvider_IsSupported_VisualStudioHost_MissingOrgInHost_ReturnsFalse()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "visualstudio.com",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(request));
        }

        [Fact]
        public void AzureReposProvider_IsSupported_NonAzureRepos_ReturnsFalse()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
                ["path"] = "org/proj/_git/repo",
            });

            var provider = new AzureReposHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(request));
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_UnencryptedHttp_ThrowsException()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var context = new TestCommandContext();
            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var entraAuth = Mock.Of<IEntraAuthentication>();
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();

            var provider = new AzureReposHostProvider(context, azDevOps, entraAuth, authorityCache, userMgr);

            await Assert.ThrowsAsync<Exception>(() => provider.GetCredentialAsync(request));
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_VsComUrlUser_ReturnsCredential()
        {
            var urlAccount = "jane.doe";

            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "org.visualstudio.com",
                ["username"] = urlAccount
            });

            var expectedOrgUri = new Uri("https://org.visualstudio.com");
            var remoteUri = new Uri("https://org.visualstudio.com/");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var expectedAccount = new EntraAccount("account-id", urlAccount);
            var authResult = CreateAuthResult(urlAccount, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);

            var entraAuthMock = new Mock<IEntraAuthentication>(MockBehavior.Strict);
            entraAuthMock.Setup(x => x.GetUserAccountsAsync(CancellationToken.None))
                .ReturnsAsync(new IEntraAccount[] { expectedAccount });
            entraAuthMock.Setup(x => x.GetTokenForUserAsync(
                    expectedScopes, authorityUrl, expectedAccount, InteractionMode.Auto, CancellationToken.None))
                .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, entraAuthMock.Object,
                authorityCacheMock.Object, userMgrMock.Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(urlAccount, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_DevAzureUrlUser_ReturnsCredential()
        {
            var urlAccount = "jane.doe";

            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/project/_git/repo",
                ["username"] = urlAccount
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var remoteUri = new Uri("https://dev.azure.com/org/project/_git/repo");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var expectedAccount = new EntraAccount("account-id", urlAccount);
            var authResult = CreateAuthResult(urlAccount, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);

            var entraAuthMock = new Mock<IEntraAuthentication>(MockBehavior.Strict);
            entraAuthMock.Setup(x => x.GetUserAccountsAsync(CancellationToken.None))
                .ReturnsAsync(new IEntraAccount[] { expectedAccount });
            entraAuthMock.Setup(x => x.GetTokenForUserAsync(
                    expectedScopes, authorityUrl, expectedAccount, InteractionMode.Auto, CancellationToken.None))
                .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, entraAuthMock.Object,
                authorityCacheMock.Object, userMgrMock.Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(urlAccount, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_DevAzureUrlOrgName_ReturnsCredential()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["username"] = "org"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var authorityUrl = "https://login.microsoftonline.com/common";
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

            var entraAuthMock = new Mock<IEntraAuthentication>(MockBehavior.Strict);
            entraAuthMock.Setup(x => x.GetTokenForUserAsync(
                    expectedScopes, authorityUrl, null, InteractionMode.Auto, CancellationToken.None))
                .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);
            userMgrMock.Setup(x => x.GetBinding(OrgName)).Returns((AzureReposBinding)null);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, entraAuthMock.Object,
                authorityCacheMock.Object, userMgrMock.Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_NoUser_ReturnsCredential()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var remoteUri = new Uri("https://dev.azure.com/org/proj/_git/repo");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var account = "john.doe";
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);

            var entraAuthMock = new Mock<IEntraAuthentication>(MockBehavior.Strict);
            entraAuthMock.Setup(x => x.GetTokenForUserAsync(
                    expectedScopes, authorityUrl, null, InteractionMode.Auto, CancellationToken.None))
                .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);
            userMgrMock.Setup(x => x.GetBinding(OrgName)).Returns((AzureReposBinding)null);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, entraAuthMock.Object,
                authorityCacheMock.Object, userMgrMock.Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_CachedAuthority_BoundUser_ReturnsCredential()
        {

            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var remoteUri = new Uri("https://dev.azure.com/org/proj/_git/repo");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var account = "john.doe";
            var expectedAccount = new EntraAccount("account-id", account);
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();

            // Use OAuth Access Tokens
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.OAuthCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);

            var entraAuthMock = new Mock<IEntraAuthentication>(MockBehavior.Strict);
            entraAuthMock.Setup(x => x.GetUserAccountsAsync(CancellationToken.None))
                .ReturnsAsync(new IEntraAccount[] { expectedAccount });
            entraAuthMock.Setup(x => x.GetTokenForUserAsync(
                    expectedScopes, authorityUrl, expectedAccount, InteractionMode.Auto, CancellationToken.None))
                .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);
            userMgrMock.Setup(x => x.GetBinding(OrgName))
                .Returns(new AzureReposBinding(OrgName, account, null));

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, entraAuthMock.Object,
                authorityCacheMock.Object, userMgrMock.Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_JwtMode_NoCachedAuthority_NoUser_ReturnsCredential()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var remoteUri = new Uri("https://dev.azure.com/org/proj/_git/repo");
            var authorityUrl = "https://login.microsoftonline.com/common";
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

            var entraAuthMock = new Mock<IEntraAuthentication>(MockBehavior.Strict);
            entraAuthMock.Setup(x => x.GetTokenForUserAsync(
                    expectedScopes, authorityUrl, null, InteractionMode.Auto, CancellationToken.None))
                .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(It.IsAny<string>())).Returns((string)null);
            authorityCacheMock.Setup(x => x.UpdateAuthority(OrgName, authorityUrl));

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);
            userMgrMock.Setup(x => x.GetBinding(OrgName)).Returns((AzureReposBinding)null);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, entraAuthMock.Object,
                authorityCacheMock.Object, userMgrMock.Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(accessToken, credential.Password);
        }

        [Theory]
        [InlineData(false, false, MsalError.InvalidClient, true)]
        [InlineData(true, false, MsalError.InvalidClient, true)]
        [InlineData(false, false, MsalError.UnauthorizedClient, true)]
        [InlineData(true, false, MsalError.UnauthorizedClient, true)]
        [InlineData(false, true, MsalError.InvalidClient, false)]
        [InlineData(true, true, MsalError.InvalidClient, false)]
        [InlineData(false, true, MsalError.UnauthorizedClient, false)]
        [InlineData(true, true, MsalError.UnauthorizedClient, false)]
        [InlineData(false, false, MsalError.AuthenticationCanceledError, false)]
        [InlineData(true, false, MsalError.AuthenticationCanceledError, false)]
        [InlineData(false, false, MsalError.AccessDenied, false)]
        [InlineData(true, false, MsalError.AccessDenied, false)]
        [InlineData(false, false, "authorization_declined", false)]
        [InlineData(true, false, "authorization_declined", false)]
        [InlineData(false, false, MsalError.InvalidRequest, false)]
        [InlineData(true, false, MsalError.InvalidRequest, false)]
        [InlineData(false, false, MsalError.InvalidGrantError, false)]
        [InlineData(true, false, MsalError.InvalidGrantError, false)]
        [InlineData(false, false, MsalError.InteractionRequired, false)]
        [InlineData(true, false, MsalError.InteractionRequired, false)]
        [InlineData(false, false, "invalid_scope", false)]
        [InlineData(true, false, "invalid_scope", false)]
        [InlineData(false, false, "temporarily_unavailable", false)]
        [InlineData(true, false, "temporarily_unavailable", false)]
        [InlineData(false, false, "test_error", false)]
        [InlineData(true, false, "test_error", false)]
        public async Task AzureReposProvider_GetCredentialAsync_MsalServiceFailure_WarnsOnlyForClientConfiguration(
            bool usePat, bool useLegacyClient, string errorCode, bool expectWarning)
        {
            var exception = new MsalServiceException(errorCode, "Test failure");

            await AssertUserCredentialFailureAsync(usePat, useLegacyClient, exception, expectWarning);
        }

        [Theory]
        [InlineData(false, MsalError.InvalidClient)]
        [InlineData(true, MsalError.InvalidClient)]
        [InlineData(false, MsalError.UnauthorizedClient)]
        [InlineData(true, MsalError.UnauthorizedClient)]
        [InlineData(false, MsalError.AuthenticationCanceledError)]
        [InlineData(true, MsalError.AuthenticationCanceledError)]
        [InlineData(false, MsalError.RedirectUriValidationFailed)]
        [InlineData(true, MsalError.RedirectUriValidationFailed)]
        [InlineData(false, MsalError.CodeExpired)]
        [InlineData(true, MsalError.CodeExpired)]
        public async Task AzureReposProvider_GetCredentialAsync_MsalClientFailure_DoesNotWarn(
            bool usePat, string errorCode)
        {
            var exception = new MsalClientException(errorCode, "Test failure");

            await AssertUserCredentialFailureAsync(usePat, false, exception, false);
        }

        [Theory]
        [InlineData(false, MsalError.InvalidClient)]
        [InlineData(true, MsalError.InvalidClient)]
        [InlineData(false, MsalError.UnauthorizedClient)]
        [InlineData(true, MsalError.UnauthorizedClient)]
        public async Task AzureReposProvider_GetCredentialAsync_MsalBaseFailure_DoesNotWarn(
            bool usePat, string errorCode)
        {
            var exception = new MsalException(errorCode, "Test failure");

            await AssertUserCredentialFailureAsync(usePat, false, exception, false);
        }

        [Theory]
        [InlineData(false, MsalError.InvalidClient)]
        [InlineData(true, MsalError.InvalidClient)]
        [InlineData(false, MsalError.UnauthorizedClient)]
        [InlineData(true, MsalError.UnauthorizedClient)]
        public async Task AzureReposProvider_GetCredentialAsync_MsalUiRequiredFailure_DoesNotWarn(
            bool usePat, string errorCode)
        {
            var exception = new MsalUiRequiredException(errorCode, "Test failure");

            await AssertUserCredentialFailureAsync(usePat, false, exception, false);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AzureReposProvider_GetCredentialAsync_MsalPolicyFailure_DoesNotWarn(bool usePat)
        {
            var exception = new IntuneAppProtectionPolicyRequiredException(
                MsalError.UnauthorizedClient, "Test failure");

            await AssertUserCredentialFailureAsync(usePat, false, exception, false);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AzureReposProvider_GetCredentialAsync_MsalThrottledFailure_DoesNotWarn(bool usePat)
        {
            var exception = new MsalThrottledServiceException(
                new MsalServiceException(MsalError.InvalidClient, "Test failure"));

            await AssertUserCredentialFailureAsync(usePat, false, exception, false);
        }

        [Theory]
        [InlineData(false, 408)]
        [InlineData(true, 408)]
        [InlineData(false, 429)]
        [InlineData(true, 429)]
        [InlineData(false, 500)]
        [InlineData(true, 500)]
        public async Task AzureReposProvider_GetCredentialAsync_MsalRetryableFailure_DoesNotWarn(
            bool usePat, int statusCode)
        {
            var exception = new MsalServiceException(MsalError.InvalidClient, "Test failure", statusCode);

            await AssertUserCredentialFailureAsync(usePat, false, exception, false);
        }

        [Theory]
        [InlineData(false, null, true)]
        [InlineData(true, null, true)]
        [InlineData(false, "", true)]
        [InlineData(true, "", true)]
        [InlineData(false, " \t\r\n", true)]
        [InlineData(true, " \t\r\n", true)]
        [InlineData(false, "CLAIMS", false)]
        [InlineData(true, "CLAIMS", false)]
        public async Task AzureReposProvider_GetCredentialAsync_MsalClaimsFailure_WarnsOnlyWithoutClaims(
            bool usePat, string claims, bool expectWarning)
        {
            var exception = new MsalServiceException(
                MsalError.InvalidClient, "Test failure", 400, claims, null);

            await AssertUserCredentialFailureAsync(usePat, false, exception, expectWarning);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AzureReposProvider_GetCredentialAsync_UserCancellation_DoesNotWarn(bool usePat)
        {
            var exception = new OperationCanceledException("Test failure");

            await AssertUserCredentialFailureAsync(usePat, false, exception, false);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AzureReposProvider_GetCredentialAsync_NonMsalFailure_DoesNotWarn(bool usePat)
        {
            var exception = new InvalidOperationException("Test failure");

            await AssertUserCredentialFailureAsync(usePat, false, exception, false);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_PatMode_OrgInUserName_NoExistingPat_GeneratesCredential()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["username"] = "org"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var personalAccessToken = "PERSONAL-ACCESS-TOKEN";
            var account = "john.doe";
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.PatCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);
            azDevOpsMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedOrgUri, accessToken, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(personalAccessToken);

            var entraAuthMock = new Mock<IEntraAuthentication>(MockBehavior.Strict);
            entraAuthMock.Setup(x => x.GetTokenForUserAsync(
                    expectedScopes, authorityUrl, null, InteractionMode.Auto, CancellationToken.None))
                .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, entraAuthMock.Object,
                authorityCacheMock.Object, userMgrMock.Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(personalAccessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_PatMode_NoExistingPat_GeneratesCredential()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var remoteUri = new Uri("https://dev.azure.com/org/proj/_git/repo");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var expectedScopes = AzureDevOpsConstants.AzureDevOpsDefaultScopes;
            var accessToken = "ACCESS-TOKEN";
            var personalAccessToken = "PERSONAL-ACCESS-TOKEN";
            var account = "john.doe";
            var authResult = CreateAuthResult(account, accessToken);

            var context = new TestCommandContext();
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.PatCredentialType;

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);
            azDevOpsMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedOrgUri, accessToken, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(personalAccessToken);

            var entraAuthMock = new Mock<IEntraAuthentication>(MockBehavior.Strict);
            entraAuthMock.Setup(x => x.GetTokenForUserAsync(
                    expectedScopes, authorityUrl, null, InteractionMode.Auto, CancellationToken.None))
                .ReturnsAsync(authResult);

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, entraAuthMock.Object,
                authorityCacheMock.Object, userMgrMock.Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(personalAccessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_PatMode_ExistingPat_ReturnsExistingCredential()
        {
            var request = new GitRequest(new Dictionary<string, string>
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
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                AzureDevOpsConstants.PatCredentialType;

            context.CredentialStore.Add(service, account, personalAccessToken);

            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var entraAuth = Mock.Of<IEntraAuthentication>();
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();

            var provider = new AzureReposHostProvider(context, azDevOps, entraAuth, authorityCache, userMgr);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(account, credential.Account);
            Assert.Equal(personalAccessToken, credential.Password);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_ManagedIdentity_ReturnsManagedIdCredential()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            const string accessToken = "MANAGED-IDENTITY-TOKEN";
            const string managedIdentity = "22222222-2222-2222-2222-222222222222";
            ManagedIdentity expectedIdentity = ManagedIdentity.Create(managedIdentity);

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
            var entraAuthMock = new Mock<IEntraAuthentication>();

            entraAuthMock.Setup(x => x.GetTokenForManagedIdentityAsync(
                    It.IsAny<string>(), It.IsAny<ManagedIdentity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MockEntraAuthResult { AccessToken = accessToken });

            var provider = new AzureReposHostProvider(
                context, azDevOps, entraAuthMock.Object, authorityCache, userMgr);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(expectedIdentity.Id, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            entraAuthMock.Verify(
                x => x.GetTokenForManagedIdentityAsync(
                    AzureDevOpsConstants.AzureDevOpsResourceId,
                    It.Is<ManagedIdentity>(mi => mi.Id == expectedIdentity.Id),
                    CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_InvalidManagedIdentity_ThrowsException()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });
            var context = new TestCommandContext
            {
                Environment =
                {
                    Variables =
                    {
                        [AzureDevOpsConstants.EnvironmentVariables.ManagedIdentity] = "not-a-managed-identity"
                    }
                }
            };
            var provider = new AzureReposHostProvider(
                context,
                Mock.Of<IAzureDevOpsRestApi>(),
                Mock.Of<IEntraAuthentication>(),
                Mock.Of<IAzureDevOpsAuthorityCache>(),
                Mock.Of<IAzureReposBindingManager>());

            await Assert.ThrowsAsync<ArgumentException>(() => provider.GetCredentialAsync(request));
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_WorkloadFederation_Generic_ReturnsFederationOptions()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            const string accessToken = "FEDERATED-IDENTITY-TOKEN";
            const string wifScenario = "generic";
            const string tenantId = "00000000-0000-0000-0000-000000000000";
            const string clientId = "11111111-1111-1111-1111-111111111111";
            const string assertion = "CLIENT-ASSERTION";

            var context = new TestCommandContext
            {
                Environment =
                {
                    Variables =
                    {
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederation] = wifScenario,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationTenantId] = tenantId,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationClientId] = clientId,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationAssertion] = assertion,
                    }
                }
            };

            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();
            var entraAuthMock = new Mock<IEntraAuthentication>();

            entraAuthMock.Setup(x => x.GetTokenUsingWorkloadFederationAsync(
                    It.IsAny<string[]>(), It.IsAny<WorkloadFederationOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MockEntraAuthResult { AccessToken = accessToken });

            var provider = new AzureReposHostProvider(
                context, azDevOps, entraAuthMock.Object, authorityCache, userMgr);

            GitResponse result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(clientId, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            entraAuthMock.Verify(
                x => x.GetTokenUsingWorkloadFederationAsync(
                    AzureDevOpsConstants.AzureDevOpsDefaultScopes,
                    It.Is<WorkloadFederationOptions>(
                        fed => fed.Scenario == WorkloadFederationScenario.Generic &&
                              fed.TenantId == tenantId &&
                              fed.ClientId == clientId &&
                              fed.Audience == WorkloadFederationOptions.DefaultAudience &&
                              fed.GenericClientAssertion == assertion),
                    CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_WorkloadFederation_GenericFileAssertion_ReadsFromFile()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            const string accessToken = "FEDERATED-IDENTITY-TOKEN";
            const string wifScenario = "generic";
            const string tenantId = "00000000-0000-0000-0000-000000000000";
            const string clientId = "11111111-1111-1111-1111-111111111111";
            const string assertion = "CLIENT-ASSERTION-FROM-FILE";
            const string filePath = "/tmp/assertion-token.txt";

            var context = new TestCommandContext
            {
                Environment =
                {
                    Variables =
                    {
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederation] = wifScenario,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationTenantId] = tenantId,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationClientId] = clientId,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationAssertion] = $"file://{filePath}",
                    }
                }
            };

            context.FileSystem.Files[filePath] = System.Text.Encoding.UTF8.GetBytes(assertion);

            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();
            var entraAuthMock = new Mock<IEntraAuthentication>();

            entraAuthMock.Setup(x => x.GetTokenUsingWorkloadFederationAsync(
                    It.IsAny<string[]>(), It.IsAny<WorkloadFederationOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MockEntraAuthResult { AccessToken = accessToken });

            var provider = new AzureReposHostProvider(
                context, azDevOps, entraAuthMock.Object, authorityCache, userMgr);

            GitResponse result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(clientId, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            entraAuthMock.Verify(
                x => x.GetTokenUsingWorkloadFederationAsync(
                    AzureDevOpsConstants.AzureDevOpsDefaultScopes,
                    It.Is<WorkloadFederationOptions>(
                        fed => fed.Scenario == WorkloadFederationScenario.Generic &&
                              fed.TenantId == tenantId &&
                              fed.ClientId == clientId &&
                              fed.Audience == WorkloadFederationOptions.DefaultAudience &&
                              fed.GenericClientAssertion == assertion),
                    CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_WorkloadFederation_MI_ReturnsFederationOptions()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            const string accessToken = "FEDERATED-IDENTITY-TOKEN";
            const string wifScenario = "managedidentity";
            const string tenantId = "00000000-0000-0000-0000-000000000000";
            const string clientId = "11111111-1111-1111-1111-111111111111";
            const string managedIdentity = "22222222-2222-2222-2222-222222222222";

            var context = new TestCommandContext
            {
                Environment =
                {
                    Variables =
                    {
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederation] = wifScenario,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationTenantId] = tenantId,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationClientId] = clientId,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationManagedIdentity] = managedIdentity,
                    }
                }
            };

            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();
            var entraAuthMock = new Mock<IEntraAuthentication>();

            entraAuthMock.Setup(x => x.GetTokenUsingWorkloadFederationAsync(
                    It.IsAny<string[]>(), It.IsAny<WorkloadFederationOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MockEntraAuthResult { AccessToken = accessToken });

            var provider = new AzureReposHostProvider(
                context, azDevOps, entraAuthMock.Object, authorityCache, userMgr);

            GitResponse result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(clientId, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            entraAuthMock.Verify(
                x => x.GetTokenUsingWorkloadFederationAsync(
                    AzureDevOpsConstants.AzureDevOpsDefaultScopes,
                    It.Is<WorkloadFederationOptions>(
                        fed => fed.Scenario == WorkloadFederationScenario.ManagedIdentity &&
                              fed.TenantId == tenantId &&
                              fed.ClientId == clientId &&
                              fed.Audience == WorkloadFederationOptions.DefaultAudience &&
                              fed.ManagedIdentityId == managedIdentity),
                    CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_WorkloadFederation_GitHubActions_ReturnsFederationOptions()
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            const string accessToken = "FEDERATED-IDENTITY-TOKEN";
            const string wifScenario = "githubactions";
            const string tenantId = "00000000-0000-0000-0000-000000000000";
            const string clientId = "11111111-1111-1111-1111-111111111111";
            const string ghRequestUrl = "https://token.actions.example.com/oidc/example?param=value";
            const string ghRequestToken = "OIDC-TOKEN";

            var context = new TestCommandContext
            {
                Environment =
                {
                    Variables =
                    {
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederation] = wifScenario,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationTenantId] = tenantId,
                        [AzureDevOpsConstants.EnvironmentVariables.WorkloadFederationClientId] = clientId,
                        [Constants.EnvironmentVariables.GitHubActionsTokenRequestUrl] = ghRequestUrl,
                        [Constants.EnvironmentVariables.GitHubActionsTokenRequestToken] = ghRequestToken,
                    }
                }
            };

            var azDevOps = Mock.Of<IAzureDevOpsRestApi>();
            var authorityCache = Mock.Of<IAzureDevOpsAuthorityCache>();
            var userMgr = Mock.Of<IAzureReposBindingManager>();
            var entraAuthMock = new Mock<IEntraAuthentication>();

            entraAuthMock.Setup(x => x.GetTokenUsingWorkloadFederationAsync(
                    It.IsAny<string[]>(), It.IsAny<WorkloadFederationOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MockEntraAuthResult { AccessToken = accessToken });

            var provider = new AzureReposHostProvider(
                context, azDevOps, entraAuthMock.Object, authorityCache, userMgr);

            GitResponse result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(clientId, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            entraAuthMock.Verify(
                x => x.GetTokenUsingWorkloadFederationAsync(
                    AzureDevOpsConstants.AzureDevOpsDefaultScopes,
                    It.Is<WorkloadFederationOptions>(
                        fed => fed.Scenario == WorkloadFederationScenario.GitHubActions &&
                              fed.TenantId == tenantId &&
                              fed.ClientId == clientId &&
                              fed.GitHubTokenRequestUrl == new Uri(ghRequestUrl) &&
                              fed.GitHubTokenRequestToken == ghRequestToken &&
                              fed.Audience == WorkloadFederationOptions.DefaultAudience),
                    CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task AzureReposProvider_GetCredentialAsync_ServicePrincipal_ReturnsSPCredential()
        {
            var request = new GitRequest(new Dictionary<string, string>
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
            var entraAuthMock = new Mock<IEntraAuthentication>();

            entraAuthMock.Setup(x => x.GetTokenForServicePrincipalAsync(
                    It.IsAny<string[]>(), It.IsAny<ServicePrincipalIdentity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MockEntraAuthResult { AccessToken = accessToken });

            var provider = new AzureReposHostProvider(
                context, azDevOps, entraAuthMock.Object, authorityCache, userMgr);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(clientId, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            entraAuthMock.Verify(x => x.GetTokenForServicePrincipalAsync(
                It.Is<string[]>(scopes =>
                    scopes.Length == 1 && scopes[0] == AzureDevOpsConstants.AzureDevOpsDefaultScopes[0]),
                It.Is<ServicePrincipalIdentity>(sp => sp.TenantId == tenantId && sp.Id == clientId),
                CancellationToken.None),
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

        [WindowsFact]
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

        [WindowsFact]
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

        [WindowsFact]
        public async Task AzureReposHostProvider_UnconfigureAsync_System_Windows_UseHttpPathSetNoManagerCoreHelper_RemovesEntry()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.System[AzDevUseHttpPathKey] = new List<string> {"true"};

            await provider.UnconfigureAsync(ConfigurationTarget.System);

            Assert.Empty(context.Git.Configuration.System);
        }

        [WindowsFact]
        public async Task AzureReposHostProvider_UnconfigureAsync_User_Windows_UseHttpPathSetAndManagerHelper_RemovesEntry()
        {
            var context = new TestCommandContext();
            var provider = new AzureReposHostProvider(context);

            context.Git.Configuration.Global[HelperKey] = new List<string> {"manager"};
            context.Git.Configuration.Global[AzDevUseHttpPathKey] = new List<string> {"true"};

            await provider.UnconfigureAsync(ConfigurationTarget.User);

            Assert.False(context.Git.Configuration.Global.TryGetValue(AzDevUseHttpPathKey, out _));
        }

        [WindowsFact]
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

        private static async Task AssertUserCredentialFailureAsync(
            bool usePat, bool useLegacyClient, Exception failure, bool expectWarning)
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "dev.azure.com",
                ["path"] = "org/proj/_git/repo"
            });

            var expectedOrgUri = new Uri("https://dev.azure.com/org");
            var authorityUrl = "https://login.microsoftonline.com/common";
            var clientIds = new List<string>();

            var context = new TestCommandContext();
            context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.CredentialType] =
                usePat ? AzureDevOpsConstants.PatCredentialType : AzureDevOpsConstants.OAuthCredentialType;
            if (useLegacyClient)
            {
                context.Environment.Variables[AzureDevOpsConstants.EnvironmentVariables.UseLegacyClientId] = "true";
            }

            var azDevOpsMock = new Mock<IAzureDevOpsRestApi>(MockBehavior.Strict);
            if (usePat)
            {
                azDevOpsMock.Setup(x => x.GetAuthorityAsync(expectedOrgUri)).ReturnsAsync(authorityUrl);
            }

            var entraAuthMock = new Mock<IEntraAuthentication>(MockBehavior.Strict);
            entraAuthMock.Setup(x => x.GetTokenForUserAsync(
                    AzureDevOpsConstants.AzureDevOpsDefaultScopes, authorityUrl, null,
                    InteractionMode.Auto, CancellationToken.None))
                .ThrowsAsync(failure);

            IEntraAuthentication EntraAuthFactory(PublicClientConfig config)
            {
                clientIds.Add(config.ClientId);
                entraAuthMock.SetupGet(x => x.PublicClientConfig).Returns(config);
                return entraAuthMock.Object;
            }

            var authorityCacheMock = new Mock<IAzureDevOpsAuthorityCache>(MockBehavior.Strict);
            authorityCacheMock.Setup(x => x.GetAuthority(OrgName)).Returns(authorityUrl);

            var userMgrMock = new Mock<IAzureReposBindingManager>(MockBehavior.Strict);
            userMgrMock.Setup(x => x.GetBinding(OrgName)).Returns((AzureReposBinding)null);

            var provider = new AzureReposHostProvider(context, azDevOpsMock.Object, EntraAuthFactory,
                authorityCacheMock.Object, userMgrMock.Object);

            Exception exception = await Record.ExceptionAsync(() => provider.GetCredentialAsync(request));

            Assert.Same(failure, exception);
            Assert.Equal(
                new[] { useLegacyClient ? AzureDevOpsConstants.LegacyClientId : AzureDevOpsConstants.ClientId },
                clientIds);
            entraAuthMock.Verify(x => x.GetTokenForUserAsync(
                    AzureDevOpsConstants.AzureDevOpsDefaultScopes, authorityUrl, null,
                    InteractionMode.Auto, CancellationToken.None),
                Times.Once);

            if (expectWarning)
            {
                Assert.Equal(new[]
                {
                    "Authentication using the new GCM Entra application failed. " +
                    "To retry this command with the legacy Entra application, " +
                    "set 'credential.azreposUseLegacyClientId' to 'true' in Git configuration.",
                    $"If that succeeds, please report the original failure at {Constants.HelpUrls.GcmNewIssue}"
                }, context.Console.WrittenMessages);
            }
            else
            {
                Assert.Empty(context.Console.WrittenMessages);
            }
        }

        private static IEntraAuthenticationResult CreateAuthResult(string upn, string token)
        {
            return new MockEntraAuthResult
            {
                Account = new EntraAccount(homeAccountId: null, userName: upn),
                AccessToken = token,
            };
        }

        private class MockEntraAuthResult : IEntraAuthenticationResult
        {
            public string AccessToken { get; set; }
            public IEntraAccount Account { get; set; }
        }
    }
}
