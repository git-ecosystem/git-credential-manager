using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tests.Objects;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketHostProviderTest
    {
        #region Tests

        private const string MOCK_ACCESS_TOKEN = "at-0987654321";
        private const string MOCK_ACCESS_TOKEN_ALT = "at-onetwothreefour-1234";
        private const string MOCK_EXPIRED_ACCESS_TOKEN = "at-1234567890-expired";
        private const string MOCK_REFRESH_TOKEN = "rt-1234567809";
        private const string BITBUCKET_DOT_ORG_HOST = "bitbucket.org";
        private const string DC_SERVER_HOST = "example.com";
        private Mock<IBitbucketAuthentication> bitbucketAuthentication = new Mock<IBitbucketAuthentication>(MockBehavior.Strict);
        private Mock<IBitbucketRestApi> bitbucketApi = new Mock<IBitbucketRestApi>(MockBehavior.Strict);

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("    ", false)]
        [InlineData("bitbucket.org", true)]
        [InlineData("BITBUCKET.ORG", true)]
        [InlineData("BiTbUcKeT.OrG", true)]
        [InlineData("bitbucket.example.com", false)]
        [InlineData("bitbucket.example.org", false)]
        [InlineData("bitbucket.org.com", false)]
        [InlineData("bitbucket.org.org", false)]
        public void BitbucketHostProvider_IsBitbucketOrg_StringHost(string str, bool expected)
        {
            bool actual = BitbucketHostProvider.IsBitbucketOrg(str);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("http://bitbucket.org", true)]
        [InlineData("https://bitbucket.org", true)]
        [InlineData("http://bitbucket.org/path", true)]
        [InlineData("https://bitbucket.org/path", true)]
        [InlineData("http://BITBUCKET.ORG", true)]
        [InlineData("https://BITBUCKET.ORG", true)]
        [InlineData("http://BITBUCKET.ORG/PATH", true)]
        [InlineData("https://BITBUCKET.ORG/PATH", true)]
        [InlineData("http://BiTbUcKeT.OrG", true)]
        [InlineData("https://BiTbUcKeT.OrG", true)]
        [InlineData("http://BiTbUcKeT.OrG/pAtH", true)]
        [InlineData("https://BiTbUcKeT.OrG/pAtH", true)]
        [InlineData("http://bitbucket.example.com", false)]
        [InlineData("https://bitbucket.example.com", false)]
        [InlineData("http://bitbucket.example.com/path", false)]
        [InlineData("https://bitbucket.example.com/path", false)]
        [InlineData("http://bitbucket.example.org", false)]
        [InlineData("https://bitbucket.example.org", false)]
        [InlineData("http://bitbucket.example.org/path", false)]
        [InlineData("https://bitbucket.example.org/path", false)]
        [InlineData("http://bitbucket.org.com", false)]
        [InlineData("https://bitbucket.org.com", false)]
        [InlineData("http://bitbucket.org.com/path", false)]
        [InlineData("https://bitbucket.org.com/path", false)]
        [InlineData("http://bitbucket.org.org", false)]
        [InlineData("https://bitbucket.org.org", false)]
        [InlineData("http://bitbucket.org.org/path", false)]
        [InlineData("https://bitbucket.org.org/path", false)]
        public void BitbucketHostProvider_IsBitbucketOrg_Uri(string str, bool expected)
        {
            bool actual = BitbucketHostProvider.IsBitbucketOrg(new Uri(str));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("https", null, false)]
        // We report that we support unencrypted HTTP here so that we can fail and
        // show a helpful error message in the call to `GenerateCredentialAsync` instead.
        [InlineData("http", BITBUCKET_DOT_ORG_HOST, true)]
        [InlineData("ssh", BITBUCKET_DOT_ORG_HOST, false)]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, true)]
        [InlineData("https", "api.bitbucket.org", true)] // Currently does support sub domains.

        [InlineData("https", "bitbucket.ogg", false)] // No support of phony similar tld.
        [InlineData("https", "bitbucket.com", false)] // No support of wrong tld.
        [InlineData("https", DC_SERVER_HOST, false)] // No support of non bitbucket domains.

        [InlineData("http", "bitbucket.my-company-server.com", false)]  // Currently no support for named on-premise instances
        [InlineData("https", "my-company-server.com", false)]
        [InlineData("https", "bitbucket.my.company.server.com", false)]
        [InlineData("https", "api.bitbucket.my-company-server.com", false)]
        [InlineData("https", "BITBUCKET.My-Company-Server.Com", false)]
        public void BitbucketHostProvider_IsSupported(string protocol, string host, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
            });

            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(input));
        }

        [Fact]
        public void BitbucketHostProvider_IsSupported_FailsForNullInput()
        {
            InputArguments input = null;
            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public void BitbucketHostProvider_IsSupported_FailsForNullHttpResponseMessage()
        {
            HttpResponseMessage httpResponseMessage = null;
            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(httpResponseMessage));
        }

        [Theory]
        [InlineData("X-AREQUESTID", "123456789", true)] // only the specific header is acceptable
        [InlineData("X-REQUESTID", "123456789", false)]
        [InlineData(null, null, false)]
        public void BitbucketHostProvider_IsSupported_HttpResponseMessage(string header, string value, bool expected)
        {
            var input = new HttpResponseMessage();
            if (header != null)
            {
                input.Headers.Add(header, value);
            }

            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(input));
        }

        [Theory]
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_Stored_Basic(
            string protocol, string host, string username, string password)
        {
            InputArguments input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockStoredAccount(context, input, password);
            MockRemoteBasicValid(input, password);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            var credential = await provider.GetCredentialAsync(input);

            Assert.Equal(username, credential.Account);
            Assert.Equal(password, credential.Password);

            // Verify bitbucket.org credentials were validated
            if (BITBUCKET_DOT_ORG_HOST.Equals(host))
            {
                VerifyValidateBasicAuthCredentialsRan(input, password);
            }
            else
            {
                // Verify DC/Server credentials were not validated
                VerifyValidateBasicAuthCredentialsNeverRan();
            }

            // Stored credentials so don't ask for more
            VerifyInteractiveAuthNeverRan();
        }

        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_Stored_OAuth(
            string protocol, string host, string username, string token)
        {
            InputArguments input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockStoredAccount(context, input, token);
            MockRemoteAccessTokenValid(input, token);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            var credential = await provider.GetCredentialAsync(input);

            Assert.Equal(username, credential.Account);
            Assert.Equal(token, credential.Password);

            // Verify bitbucket.org credentials were validated
            VerifyValidateAccessTokenRan(input, token);

            // Stored credentials so don't ask for more
            VerifyInteractiveAuthNeverRan();
        }

        [Theory]
        // DC
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_New_Basic(
            string protocol, string host, string username, string password)
        {
            InputArguments input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockPromptBasic(input, password);
            MockRemoteBasicValid(input, password);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            var credential = await provider.GetCredentialAsync(input);

            Assert.Equal(username, credential.Account);
            Assert.Equal(password, credential.Password);

            VerifyInteractiveAuthRan(input);
        }

        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_New_OAuth(
            string protocol, string host, string username, string refreshToken, string accessToken)
        {
            InputArguments input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockPromptOAuth(input);
            MockRemoteOAuthTokenCreate(input, accessToken, refreshToken);
            MockRemoteAccessTokenValid(input, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            var credential = await provider.GetCredentialAsync(input);

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyInteractiveAuthRan(input);
            VerifyOAuthFlowRan(input, accessToken);
            VerifyValidateAccessTokenRan(input, accessToken);
            VerifyOAuthRefreshTokenStored(context, input, refreshToken);
        }

        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_MissingAT_OAuth_Refresh(
            string protocol, string host, string username, string refreshToken, string accessToken)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            // AT has does not exist, but RT is still valid
            MockStoredRefreshToken(context, input, refreshToken);
            MockRemoteAccessTokenValid(input, accessToken);
            MockRemoteRefreshTokenValid(refreshToken, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            var credential = await provider.GetCredentialAsync(input);

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyValidateAccessTokenRan(input, accessToken);
            VerifyOAuthRefreshRan(refreshToken);
            VerifyInteractiveAuthNeverRan();
        }

        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_EXPIRED_ACCESS_TOKEN, MOCK_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_ExpiredAT_OAuth_Refresh(
            string protocol, string host, string username, string refreshToken, string expiredAccessToken, string accessToken)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            // AT exists but has expired, but RT is still valid
            MockStoredAccount(context, input, expiredAccessToken);
            MockRemoteAccessTokenExpired(input, expiredAccessToken);

            MockStoredRefreshToken(context, input, refreshToken);
            MockRemoteAccessTokenValid(input, accessToken);
            MockRemoteRefreshTokenValid(refreshToken, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            var credential = await provider.GetCredentialAsync(input);

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyValidateAccessTokenRan(input, accessToken);
            VerifyOAuthRefreshRan(refreshToken);
            VerifyInteractiveAuthNeverRan();
        }

        [Theory]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_PreconfiguredMode_OAuth_ValidRT_IsRespected(
            string protocol, string host, string username, string refreshToken, string accessToken)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            context.Environment.Variables.Add(BitbucketConstants.EnvironmentVariables.AuthenticationModes, "oauth");

            // We have a stored RT so we can just use that without any prompts
            MockStoredRefreshToken(context, input, refreshToken);
            MockRemoteAccessTokenValid(input, accessToken);
            MockRemoteRefreshTokenValid(refreshToken, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            var credential = await provider.GetCredentialAsync(input);

            Assert.NotNull(credential);

            VerifyInteractiveAuthNeverRan();
            VerifyOAuthRefreshRan(refreshToken);
        }

        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_ACCESS_TOKEN, MOCK_ACCESS_TOKEN_ALT, MOCK_REFRESH_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_AlwaysRefreshCredentials_OAuth_IsRespected(
            string protocol, string host, string username, string storedToken, string newToken, string refreshToken)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            context.Environment.Variables.Add(
                BitbucketConstants.EnvironmentVariables.AlwaysRefreshCredentials, bool.TrueString);

            // User has stored access token that we shouldn't use - RT should be used to mint new AT
            MockStoredAccount(context, input, storedToken);
            MockStoredRefreshToken(context, input, refreshToken);
            MockRemoteAccessTokenValid(input, newToken);
            MockRemoteRefreshTokenValid(refreshToken, newToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            var credential = await provider.GetCredentialAsync(input);

            Assert.Equal(username, credential.Account);
            Assert.Equal(newToken, credential.Password);

            VerifyInteractiveAuthNeverRan();
            VerifyOAuthRefreshRan(refreshToken);
        }

        [Theory]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "old-password", "new-password")]
        // DC
        [InlineData("https", DC_SERVER_HOST, "jsquire", "old-password", "new-password")]
        public async Task BitbucketHostProvider_GetCredentialAsync_AlwaysRefreshCredentials_Basic_IsRespected(
            string protocol, string host, string username, string storedPassword, string freshPassword)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            context.Environment.Variables.Add(
                BitbucketConstants.EnvironmentVariables.AlwaysRefreshCredentials, bool.TrueString);

            // User has stored password that we shouldn't use
            MockStoredAccount(context, input, storedPassword);
            MockPromptBasic(input, freshPassword);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            var credential = await provider.GetCredentialAsync(input);

            Assert.Equal(username, credential.Account);
            Assert.Equal(freshPassword, credential.Password);

            VerifyInteractiveAuthRan(input);
        }

        [Theory]
        // DC - supports Basic
        [InlineData("https://example.com", "basic", AuthenticationModes.Basic)]
        [InlineData("https://example.com", "oauth", AuthenticationModes.Basic)]
        // Cloud - supports Basic, OAuth
        [InlineData("https://bitbucket.org", "oauth", AuthenticationModes.OAuth)]
        [InlineData("https://bitbucket.org", "basic", AuthenticationModes.Basic)]
        [InlineData("https://bitbucket.org", "NOT-A-REAL-VALUE", BitbucketConstants.DotOrgAuthenticationModes)]
        [InlineData("https://Bitbucket.org", "NOT-A-REAL-VALUE", BitbucketConstants.DotOrgAuthenticationModes)]
        [InlineData("https://bitbucket.org", "none", BitbucketConstants.DotOrgAuthenticationModes)]
        [InlineData("https://Bitbucket.org", "none", BitbucketConstants.DotOrgAuthenticationModes)]
        [InlineData("https://bitbucket.org", null, BitbucketConstants.DotOrgAuthenticationModes)]
        [InlineData("https://Bitbucket.org", null, BitbucketConstants.DotOrgAuthenticationModes)]
        public void BitbucketHostProvider_GetSupportedAuthenticationModes(string uriString, string bitbucketAuthModes, AuthenticationModes expectedModes)
        {
            var targetUri = new Uri(uriString);

            var context = new TestCommandContext();
            if (bitbucketAuthModes != null)
            {
                context.Environment.Variables.Add(BitbucketConstants.EnvironmentVariables.AuthenticationModes, bitbucketAuthModes);
            }

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            AuthenticationModes actualModes = provider.GetSupportedAuthenticationModes(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }

        [Theory]
        [InlineData("https", DC_SERVER_HOST, "jsquire")]
        public async Task BitbucketHostProvider_StoreCredentialAsync(string protocol, string host, string username)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            Assert.Equal(0, context.CredentialStore.Count);

            await provider.StoreCredentialAsync(input);

            Assert.Equal(1, context.CredentialStore.Count);
        }

        [Theory]
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_EraseCredentialAsync(string protocol, string host, string username, string password)
        {
            var input = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockStoredAccount(context, input, password);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            Assert.Equal(1, context.CredentialStore.Count);

            await provider.EraseCredentialAsync(input);

            Assert.Equal(0, context.CredentialStore.Count);
        }

        #endregion

        #region Test helpers

        private static InputArguments MockInput(string protocol, string host, string username)
        {
            return new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
                ["username"] = username
            });
        }

        private void VerifyOAuthFlowRan(InputArguments input, string token)
        {
            var remoteUri = input.GetRemoteUri();

            // Get new access token and refresh token
            bitbucketAuthentication.Verify(m => m.CreateOAuthCredentialsAsync(remoteUri), Times.Once);

            // Check access token works/resolve username
            bitbucketApi.Verify(m => m.GetUserInformationAsync(null, token, true), Times.Once);
        }

        private void VerifyValidateBasicAuthCredentialsNeverRan()
        {
            // Never check username/password works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(It.IsAny<string>(), It.IsAny<string>(), false), Times.Never);
        }

        private void VerifyValidateBasicAuthCredentialsRan(InputArguments input, string password)
        {
            // Check username/password works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(input.UserName, password, false), Times.Once);
        }

        private void VerifyValidateAccessTokenRan(InputArguments input, string token)
        {
            // Check tokens works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(null, token, true), Times.Once);
        }

        private void VerifyInteractiveAuthRan(InputArguments input)
        {
            var remoteUri = input.GetRemoteUri();

            bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()), Times.Once);
        }

        private void VerifyInteractiveAuthNeverRan()
        {
            bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<AuthenticationModes>()), Times.Never);
        }

        private void VerifyOAuthRefreshRan(string refreshToken)
        {
            // Check refresh was called
            bitbucketAuthentication.Verify(m => m.RefreshOAuthCredentialsAsync(refreshToken), Times.Once);
        }

        private void MockRemoteRefreshTokenValid(string refreshToken, string accessToken)
        {
            bitbucketAuthentication.Setup(m => m.RefreshOAuthCredentialsAsync(refreshToken)).ReturnsAsync(new OAuth2TokenResult(accessToken, "access_token"));
        }

        private void MockPromptBasic(InputArguments input, string password)
        {
            var remoteUri = input.GetRemoteUri();
            bitbucketAuthentication.Setup(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()))
                .ReturnsAsync(new CredentialsPromptResult(AuthenticationModes.Basic, new TestCredential(input.Host, input.UserName, password)));
        }

        private void MockPromptOAuth(InputArguments input)
        {
            var remoteUri = input.GetRemoteUri();
            bitbucketAuthentication.Setup(m => m.GetCredentialsAsync(remoteUri, input.UserName, It.IsAny<AuthenticationModes>()))
                .ReturnsAsync(new CredentialsPromptResult(AuthenticationModes.OAuth));
        }

        private void MockRemoteBasicValid(InputArguments input, string password, bool twoFactor = true)
        {
            var userInfo = new UserInfo
            {
                UserName = input.UserName,
                IsTwoFactorAuthenticationEnabled = twoFactor
            };

            // Basic
            bitbucketApi.Setup(x => x.GetUserInformationAsync(input.UserName, password, false))
                .ReturnsAsync(new RestApiResult<UserInfo>(System.Net.HttpStatusCode.OK, userInfo));
        }

        private void MockRemoteAccessTokenExpired(InputArguments input, string token)
        {
            // OAuth
            bitbucketApi.Setup(x => x.GetUserInformationAsync(null, token, true))
                .ReturnsAsync(new RestApiResult<UserInfo>(System.Net.HttpStatusCode.Unauthorized));
        }

        private void MockRemoteAccessTokenValid(InputArguments input, string token, bool twoFactor = true)
        {
            var userInfo = new UserInfo
            {
                UserName = input.UserName,
                IsTwoFactorAuthenticationEnabled = twoFactor
            };

            // OAuth
            bitbucketApi.Setup(x => x.GetUserInformationAsync(null, token, true))
                .ReturnsAsync(new RestApiResult<UserInfo>(System.Net.HttpStatusCode.OK, userInfo));
        }

        private static void MockStoredAccount(TestCommandContext context, InputArguments input, string password)
        {
            var remoteUri = input.GetRemoteUri();
            var remoteUrl = remoteUri.AbsoluteUri.Substring(0, remoteUri.AbsoluteUri.Length - 1);
            context.CredentialStore.Add(remoteUrl, new TestCredential(input.Host, input.UserName, password));
        }

        private static void MockStoredRefreshToken(TestCommandContext context, InputArguments input, string token)
        {
            var remoteUri = input.GetRemoteUri();
            var refreshService = BitbucketHostProvider.GetRefreshTokenServiceName(remoteUri);
            context.CredentialStore.Add(refreshService, new TestCredential(refreshService, input.UserName, token));
        }

        private void MockRemoteOAuthTokenCreate(InputArguments input, string accessToken, string refreshToken)
        {
            var remoteUri = input.GetRemoteUri();
            bitbucketAuthentication.Setup(x => x.CreateOAuthCredentialsAsync(remoteUri))
                .ReturnsAsync(new OAuth2TokenResult(accessToken, "access_token") { RefreshToken = refreshToken });
        }

        private void VerifyOAuthRefreshTokenStored(TestCommandContext context, InputArguments input, string refreshToken)
        {
            var remoteUri = input.GetRemoteUri();
            string refreshService = BitbucketHostProvider.GetRefreshTokenServiceName(remoteUri);
            bool result = context.CredentialStore.TryGet(refreshService, input.UserName, out var credential);

            Assert.True(result);
            Assert.Equal(refreshToken, credential.Password);
        }

        #endregion
    }
}
