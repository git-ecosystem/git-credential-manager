using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager.Authentication;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class GenericHostProviderTests
    {
        [Theory]
        [InlineData("http", true)]
        [InlineData("HTTP", true)]
        [InlineData("hTtP", true)]
        [InlineData("https", true)]
        [InlineData("HTTPS", true)]
        [InlineData("hTtPs", true)]
        [InlineData("ssh", true)]
        [InlineData("SSH", true)]
        [InlineData("sSh", true)]
        [InlineData("smpt", true)]
        [InlineData("SmtP", true)]
        [InlineData("SMTP", true)]
        [InlineData("imap", true)]
        [InlineData("iMAp", true)]
        [InlineData("IMAP", true)]
        [InlineData("file", true)]
        [InlineData("fIlE", true)]
        [InlineData("FILE", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void GenericHostProvider_IsSupported(string protocol, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"]     = "example.com",
                ["path"]     = "foo/bar",
            });

            var provider = new GenericHostProvider(new TestCommandContext());

            Assert.Equal(expected, provider.IsSupported(input));
        }

        [Fact]
        public void GenericHostProvider_GetCredentialServiceUrl_ReturnsCorrectKey()
        {
            const string expectedService = "https://example.com/foo/bar";

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                ["path"]     = "foo/bar",
                ["username"] = "john.doe",
            });

            var provider = new GenericHostProvider(new TestCommandContext());

            string actualService = provider.GetServiceName(input);

            Assert.Equal(expectedService, actualService);
        }

        [Fact]
        public async Task GenericHostProvider_CreateCredentialAsync_WiaNotAllowed_ReturnsBasicCredentialNoWiaCheck()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            });

            const string testUserName = "basicUser";
            const string testPassword = "basicPass";
            var basicCredential = new GitCredential(testUserName, testPassword);

            var context = new TestCommandContext
            {
                Settings = {IsWindowsIntegratedAuthenticationEnabled = false}
            };
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(basicCredential)
                .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            var oauthMock = new Mock<IOAuthAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object, oauthMock.Object);

            var result = await provider.GenerateCredentialAsync(input);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
            wiaAuthMock.Verify(x => x.GetAuthenticationTypesAsync(It.IsAny<Uri>()), Times.Never);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GenericHostProvider_CreateCredentialAsync_LegacyAuthorityBasic_ReturnsBasicCredentialNoWiaCheck()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            });

            const string testUserName = "basicUser";
            const string testPassword = "basicPass";
            var basicCredential = new GitCredential(testUserName, testPassword);

            var context = new TestCommandContext
            {
                Settings = {LegacyAuthorityOverride = "basic"}
            };
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(basicCredential)
                .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            var oauthMock = new Mock<IOAuthAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object, oauthMock.Object);

            var result = await provider.GenerateCredentialAsync(input);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
            wiaAuthMock.Verify(x => x.GetAuthenticationTypesAsync(It.IsAny<Uri>()), Times.Never);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GenericHostProvider_CreateCredentialAsync_NonHttpProtocol_ReturnsBasicCredentialNoWiaCheck()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "smtp",
                ["host"]     = "example.com",
            });

            const string testUserName = "basicUser";
            const string testPassword = "basicPass";
            var basicCredential = new GitCredential(testUserName, testPassword);

            var context = new TestCommandContext();
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(basicCredential)
                .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            var oauthMock = new Mock<IOAuthAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object, oauthMock.Object);

            var result = await provider.GenerateCredentialAsync(input);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
            wiaAuthMock.Verify(x => x.GetAuthenticationTypesAsync(It.IsAny<Uri>()), Times.Never);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [PosixFact]
        public async Task GenericHostProvider_CreateCredentialAsync_NonWindows_WiaSupported_ReturnsBasicCredential()
        {
            await TestCreateCredentialAsync_ReturnsBasicCredential(WindowsAuthenticationTypes.All);
        }

        [WindowsFact]
        public async Task GenericHostProvider_CreateCredentialAsync_Windows_WiaSupported_ReturnsEmptyCredential()
        {
            await TestCreateCredentialAsync_ReturnsEmptyCredential(WindowsAuthenticationTypes.All);
        }

        [Fact]
        public async Task GenericHostProvider_CreateCredentialAsync_WiaNotSupported_ReturnsBasicCredential()
        {
            await TestCreateCredentialAsync_ReturnsBasicCredential(WindowsAuthenticationTypes.None);
        }

        [WindowsFact]
        private static async Task GenericHostProvider_NtlmSuppressed_AllowOnce()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                [Constants.CredentialProtocol.NtlmKey] = Constants.CredentialProtocol.NtlmSuppressed,
            });

            var configKey =
                $"{Constants.GitConfiguration.Http.SectionName}.https://example.com.{Constants.GitConfiguration.Http.AllowNtlmAuth}";

            var context = new TestCommandContext();
            context.Git.Configuration.Global.Clear();

            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            wiaAuthMock.Setup(x => x.GetAuthenticationTypesAsync(It.IsAny<Uri>()))
                .ReturnsAsync(WindowsAuthenticationTypes.Ntlm);
            wiaAuthMock.Setup(x => x.AskEnableNtlmAsync(It.IsAny<Uri>()))
                .ReturnsAsync(NtlmSupport.Once);
            var oauthMock = new Mock<IOAuthAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object, oauthMock.Object);

            var result = await provider.GenerateCredentialAsync(input);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(string.Empty, credential.Account);
            Assert.Equal(string.Empty, credential.Password);
            Assert.True(result.AdditionalProperties.TryGetValue(Constants.CredentialProtocol.NtlmKey, out string ntlmValue));
            Assert.Equal(Constants.CredentialProtocol.NtlmAllow, ntlmValue);

            wiaAuthMock.Verify(x => x.AskEnableNtlmAsync(It.IsAny<Uri>()), Times.Once);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            Assert.False(context.Git.Configuration.Global.TryGetValue(configKey, out _));
        }

        [WindowsFact]
        private static async Task GenericHostProvider_NtlmSuppressed_AllowAlways()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                [Constants.CredentialProtocol.NtlmKey] = Constants.CredentialProtocol.NtlmSuppressed,
            });

            var configKey =
                $"{Constants.GitConfiguration.Http.SectionName}.https://example.com.{Constants.GitConfiguration.Http.AllowNtlmAuth}";

            var context = new TestCommandContext();
            context.Git.Configuration.Global.Clear();

            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            wiaAuthMock.Setup(x => x.GetAuthenticationTypesAsync(It.IsAny<Uri>()))
                .ReturnsAsync(WindowsAuthenticationTypes.Ntlm);
            wiaAuthMock.Setup(x => x.AskEnableNtlmAsync(It.IsAny<Uri>()))
                .ReturnsAsync(NtlmSupport.Always);
            var oauthMock = new Mock<IOAuthAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object, oauthMock.Object);

            var result = await provider.GenerateCredentialAsync(input);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(string.Empty, credential.Account);
            Assert.Equal(string.Empty, credential.Password);
            Assert.True(result.AdditionalProperties.TryGetValue(Constants.CredentialProtocol.NtlmKey, out string ntlmValue));
            Assert.Equal(Constants.CredentialProtocol.NtlmAllow, ntlmValue);

            wiaAuthMock.Verify(x => x.AskEnableNtlmAsync(It.IsAny<Uri>()), Times.Once);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            Assert.True(context.Git.Configuration.Global.TryGetValue(configKey, out IList<string> configValues));
            string configValue = Assert.Single(configValues);
            Assert.True(configValue.IsTruthy());
        }

        [WindowsFact]
        private static async Task GenericHostProvider_NtlmSuppressed_Disabled()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                [Constants.CredentialProtocol.NtlmKey] = Constants.CredentialProtocol.NtlmSuppressed,
            });

            var configKey =
                $"{Constants.GitConfiguration.Http.SectionName}.https://example.com.{Constants.GitConfiguration.Http.AllowNtlmAuth}";

            var context = new TestCommandContext();
            context.Git.Configuration.Global.Clear();

            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new GitCredential("testUser", "testPassword"));
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            wiaAuthMock.Setup(x => x.GetAuthenticationTypesAsync(It.IsAny<Uri>()))
                .ReturnsAsync(WindowsAuthenticationTypes.Ntlm);
            wiaAuthMock.Setup(x => x.AskEnableNtlmAsync(It.IsAny<Uri>()))
                .ReturnsAsync(NtlmSupport.Disabled);
            var oauthMock = new Mock<IOAuthAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object, oauthMock.Object);

            var result = await provider.GenerateCredentialAsync(input);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal("testUser", credential.Account);
            Assert.Equal("testPassword", credential.Password);
            Assert.False(result.AdditionalProperties.TryGetValue(Constants.CredentialProtocol.NtlmKey, out _));

            wiaAuthMock.Verify(x => x.AskEnableNtlmAsync(It.IsAny<Uri>()), Times.Once);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            Assert.False(context.Git.Configuration.Global.TryGetValue(configKey, out _));
        }

        [Fact]
        public async Task GenericHostProvider_GenerateCredentialAsync_OAuth_CompleteOAuthConfig_UsesOAuth()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "git.example.com",
                ["path"]     = "foo"
            });

            const string testUserName = "TEST_OAUTH_USER";
            const string testAcessToken = "OAUTH_TOKEN";
            const string testRefreshToken = "OAUTH_REFRESH_TOKEN";
            const string testResource = "https://git.example.com/foo";
            const string expectedRefreshTokenService = "https://refresh_token.git.example.com/foo";

            var authMode = OAuthAuthenticationModes.Browser;
            string[] scopes = { "code:write", "code:read" };
            string clientId = "3eadfc62-9e91-45d3-8c60-20ccd6d0c7cf";
            string clientSecret = "C1DA8B93CCB5F5B93DA";
            string redirectUri = "http://localhost";
            string authzEndpoint = "/oauth/authorize";
            string tokenEndpoint = "/oauth/token";
            string deviceEndpoint = "/oauth/device";

            string GetKey(string name) => $"{Constants.GitConfiguration.Credential.SectionName}.https://example.com.{name}";

            var context = new TestCommandContext
            {
                Git =
                {
                    Configuration =
                    {
                        Global =
                        {
                            [GetKey(Constants.GitConfiguration.Credential.OAuthClientId)] = new[] { clientId },
                            [GetKey(Constants.GitConfiguration.Credential.OAuthClientSecret)] = new[] { clientSecret },
                            [GetKey(Constants.GitConfiguration.Credential.OAuthRedirectUri)] = new[] { redirectUri },
                            [GetKey(Constants.GitConfiguration.Credential.OAuthScopes)] = new[] { string.Join(' ', scopes) },
                            [GetKey(Constants.GitConfiguration.Credential.OAuthAuthzEndpoint)] = new[] { authzEndpoint },
                            [GetKey(Constants.GitConfiguration.Credential.OAuthTokenEndpoint)] = new[] { tokenEndpoint },
                            [GetKey(Constants.GitConfiguration.Credential.OAuthDeviceEndpoint)] = new[] { deviceEndpoint },
                            [GetKey(Constants.GitConfiguration.Credential.OAuthDefaultUserName)] = new[] { testUserName },
                        }
                    }
                },
                Settings =
                {
                    RemoteUri = new Uri(testResource)
                }
            };

            var basicAuthMock = new Mock<IBasicAuthentication>();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            var oauthMock = new Mock<IOAuthAuthentication>();
            oauthMock.Setup(x =>
                x.GetAuthenticationModeAsync(It.IsAny<string>(), It.IsAny<OAuthAuthenticationModes>()))
                .ReturnsAsync(authMode);
            oauthMock.Setup(x => x.GetTokenByBrowserAsync(It.IsAny<OAuth2Client>(), It.IsAny<string[]>()))
                .ReturnsAsync(new OAuth2TokenResult(testAcessToken, "access_token")
                {
                    Scopes = scopes,
                    RefreshToken = testRefreshToken
                });

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object, oauthMock.Object);

            var result = await provider.GenerateCredentialAsync(input);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testAcessToken, credential.Password);

            Assert.True(context.CredentialStore.TryGet(expectedRefreshTokenService, null, out TestCredential refreshToken));
            Assert.Equal(testUserName, refreshToken.Account);
            Assert.Equal(testRefreshToken, refreshToken.Password);

            oauthMock.Verify(x => x.GetAuthenticationModeAsync(testResource, OAuthAuthenticationModes.All), Times.Once);
            oauthMock.Verify(x => x.GetTokenByBrowserAsync(It.IsAny<OAuth2Client>(), scopes), Times.Once);
            oauthMock.Verify(x => x.GetTokenByDeviceCodeAsync(It.IsAny<OAuth2Client>(), scopes), Times.Never);
            wiaAuthMock.Verify(x => x.GetAuthenticationTypesAsync(It.IsAny<Uri>()), Times.Never);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #region Helpers

        private static async Task TestCreateCredentialAsync_ReturnsEmptyCredential(WindowsAuthenticationTypes supportedWiaTypes)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            });

            var context = new TestCommandContext();
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                         .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            wiaAuthMock.Setup(x => x.GetAuthenticationTypesAsync(It.IsAny<Uri>()))
                       .ReturnsAsync(supportedWiaTypes);
            var oauthMock = new Mock<IOAuthAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object, oauthMock.Object);

            var result = await provider.GenerateCredentialAsync(input);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(string.Empty, credential.Account);
            Assert.Equal(string.Empty, credential.Password);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private static async Task TestCreateCredentialAsync_ReturnsBasicCredential(WindowsAuthenticationTypes supportedWiaTypes)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            });

            const string testUserName = "basicUser";
            const string testPassword = "basicPass";
            var basicCredential = new GitCredential(testUserName, testPassword);

            var context = new TestCommandContext();
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                         .ReturnsAsync(basicCredential)
                         .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            wiaAuthMock.Setup(x => x.GetAuthenticationTypesAsync(It.IsAny<Uri>()))
                       .ReturnsAsync(supportedWiaTypes);
            var oauthMock = new Mock<IOAuthAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object, oauthMock.Object);

            var result = await provider.GenerateCredentialAsync(input);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        #endregion
    }
}
