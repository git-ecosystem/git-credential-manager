using Atlassian.Bitbucket.Cloud;
using Atlassian.Bitbucket.DataCenter;
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
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
            });

            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(request));
        }

        [Theory]
        [InlineData("Basic realm=\"Atlassian Bitbucket\"", true)]
        [InlineData("Basic realm=\"GitSponge\"", false)]
        public void BitbucketHostProvider_IsSupported_WWWAuth(string wwwauth, bool expected)
        {
            var request = new GitRequest(new Dictionary<string, string>
            {
                ["wwwauth"] = wwwauth,
            });

            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(request));
        }

        [Fact]
        public void BitbucketHostProvider_IsSupported_FailsForNullInput()
        {
            GitRequest request = null;
            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(request));
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
            var request = new HttpResponseMessage();
            if (header != null)
            {
                request.Headers.Add(header, value);
            }

            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(request));
        }

        [Theory]
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_Stored_Basic(
            string protocol, string host, string username, string password)
        {
            GitRequest request = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            if (DC_SERVER_HOST.Equals(host))
            {
                MockDCSSOEnabled();
            }
            MockStoredAccount(context, request, password);
            MockRemoteBasicValid(request, password);
            // HACK rebase MockRemoteBasicAuthAccountIsValidNo2FA(bitbucketApi, request, password, username);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(password, credential.Password);

            // Verify bitbucket.org credentials were validated
                VerifyValidateBasicAuthCredentialsRan(request, password);
                // Verify DC/Server credentials were not validated

            // Stored credentials so don't ask for more
            VerifyInteractiveAuthNeverRan();
        }

        public Mock<IBitbucketRestApi> GetBitbucketApi()
        {
            return bitbucketApi;
        }

        [Theory]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_Stored_OAuth(
            string protocol, string host, string username, string token)
        {
            GitRequest request = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            if (DC_SERVER_HOST.Equals(host))
            {
                MockDCSSOEnabled();
            }
            MockStoredAccount(context, request, token);
            MockRemoteAccessTokenValid(request, token);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(token, credential.Password);

            // Verify bitbucket.org credentials were validated
            VerifyValidateAccessTokenRan(request, token);

            // Stored credentials so don't ask for more
            VerifyInteractiveAuthNeverRan();
        }

        private void MockDCSSOEnabled()
        {
            bitbucketApi.Setup(ba => ba.GetAuthenticationMethodsAsync()).Returns(Task.FromResult(new List<AuthenticationMethod>(){AuthenticationMethod.BasicAuth, AuthenticationMethod.Sso}));
            bitbucketApi.Setup(ba => ba.IsOAuthInstalledAsync()).Returns(Task.FromResult(true));
        }

        [Theory]
        // DC
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_New_Basic(
            string protocol, string host, string username, string password)
        {
            GitRequest request = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockPromptBasic(request, password);
            MockRemoteBasicValid(request, password);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(password, credential.Password);

            VerifyInteractiveAuthRan(request);
        }

        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_New_OAuth(
            string protocol, string host, string username, string refreshToken, string accessToken)
        {
            GitRequest request = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockPromptOAuth(request);
            MockRemoteOAuthTokenCreate(request, accessToken, refreshToken);
            MockRemoteAccessTokenValid(request, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyInteractiveAuthRan(request);
            VerifyOAuthFlowRan(request, accessToken);
            VerifyValidateAccessTokenRan(request, accessToken);
            VerifyOAuthRefreshTokenStored(context, request, refreshToken);
        }

        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_MissingAT_OAuth_Refresh(
            string protocol, string host, string username, string refreshToken, string accessToken)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            // AT has does not exist, but RT is still valid
            MockStoredRefreshToken(context, request, refreshToken);
            MockRemoteAccessTokenValid(request, accessToken);
            MockRemoteRefreshTokenValid(request, refreshToken, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyValidateAccessTokenRan(request, accessToken);
            VerifyOAuthRefreshRan(request, refreshToken);
            VerifyInteractiveAuthNeverRan();
        }

        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_EXPIRED_ACCESS_TOKEN, MOCK_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_ExpiredAT_OAuth_Refresh(
            string protocol, string host, string username, string refreshToken, string expiredAccessToken, string accessToken)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            // AT exists but has expired, but RT is still valid
            MockStoredAccount(context, request, expiredAccessToken);
            MockRemoteAccessTokenExpired(request, expiredAccessToken);

            MockStoredRefreshToken(context, request, refreshToken);
            MockRemoteAccessTokenValid(request, accessToken);
            MockRemoteRefreshTokenValid(request, refreshToken, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyValidateAccessTokenRan(request, accessToken);
            VerifyOAuthRefreshRan(request, refreshToken);
            VerifyInteractiveAuthNeverRan();
        }

        [Theory]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_PreconfiguredMode_OAuth_ValidRT_IsRespected(
            string protocol, string host, string username, string refreshToken, string accessToken)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            context.Environment.Variables.Add(BitbucketConstants.EnvironmentVariables.AuthenticationModes, "oauth");

            // We have a stored RT so we can just use that without any prompts
            MockStoredRefreshToken(context, request, refreshToken);
            MockRemoteAccessTokenValid(request, accessToken);
            MockRemoteRefreshTokenValid(request, refreshToken, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.NotNull(credential);

            VerifyInteractiveAuthNeverRan();
            VerifyOAuthRefreshRan(request, refreshToken);
        }

        [Theory]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_ACCESS_TOKEN, MOCK_ACCESS_TOKEN_ALT, MOCK_REFRESH_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_AlwaysRefreshCredentials_OAuth_IsRespected(
            string protocol, string host, string username, string storedToken, string newToken, string refreshToken)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            context.Environment.Variables.Add(
                BitbucketConstants.EnvironmentVariables.AlwaysRefreshCredentials, bool.TrueString);

            // User has stored access token that we shouldn't use - RT should be used to mint new AT
            MockStoredAccount(context, request, storedToken);
            MockStoredRefreshToken(context, request, refreshToken);
            MockRemoteAccessTokenValid(request, newToken);
            MockRemoteRefreshTokenValid(request, refreshToken, newToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(newToken, credential.Password);

            VerifyInteractiveAuthNeverRan();
            VerifyOAuthRefreshRan(request, refreshToken);
        }

        [Theory]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", "old-password", "new-password")]
        // DC
        [InlineData("https", DC_SERVER_HOST, "jsquire", "old-password", "new-password")]
        public async Task BitbucketHostProvider_GetCredentialAsync_AlwaysRefreshCredentials_Basic_IsRespected(
            string protocol, string host, string username, string storedPassword, string freshPassword)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            context.Environment.Variables.Add(
                BitbucketConstants.EnvironmentVariables.AlwaysRefreshCredentials, bool.TrueString);

            // User has stored password that we shouldn't use
            MockStoredAccount(context, request, storedPassword);
            MockPromptBasic(request, freshPassword);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(freshPassword, credential.Password);

            VerifyInteractiveAuthRan(request);
        }

        [Theory]
        // DC - supports Basic, OAuth
        [InlineData("https", "example.com", "basic", AuthenticationModes.Basic)]
        [InlineData("https", "example.com", "oauth", AuthenticationModes.OAuth)]
        [InlineData("https", "example.com", "NOT-A-REAL-VALUE", DataCenterConstants.ServerAuthenticationModes)]
        [InlineData("https", "example.com", "none", DataCenterConstants.ServerAuthenticationModes)]
        [InlineData("https", "example.com", null, DataCenterConstants.ServerAuthenticationModes)]
        // Cloud - supports Basic, OAuth
        [InlineData("https", "bitbucket.org", "oauth", AuthenticationModes.OAuth)]
        [InlineData("https", "bitbucket.org", "basic", AuthenticationModes.Basic)]
        [InlineData("https", "bitbucket.org", "NOT-A-REAL-VALUE", CloudConstants.DotOrgAuthenticationModes)]
        [InlineData("https", "bitbucket.org", "none", CloudConstants.DotOrgAuthenticationModes)]
        [InlineData("https", "bitbucket.org", null, CloudConstants.DotOrgAuthenticationModes)]
        public async Task BitbucketHostProvider_GetSupportedAuthenticationModes(string protocol, string host, string bitbucketAuthModes, AuthenticationModes expectedModes)
        {
            var request = MockInput(protocol, host, null);

            var context = new TestCommandContext();
            if (bitbucketAuthModes != null)
            {
                context.Environment.Variables.Add(BitbucketConstants.EnvironmentVariables.AuthenticationModes, bitbucketAuthModes);
            }

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(request);

            Assert.Equal(expectedModes, actualModes);
        }

        [Theory]
        [InlineData("https", DC_SERVER_HOST, "jsquire")]
        public async Task BitbucketHostProvider_StoreCredentialAsync(string protocol, string host, string username)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            Assert.Equal(0, context.CredentialStore.Count);

            await provider.StoreCredentialAsync(request);

            Assert.Equal(1, context.CredentialStore.Count);
        }

        [Theory]
        [InlineData("https", DC_SERVER_HOST, "jsquire", "password")]
        public async Task BitbucketHostProvider_EraseCredentialAsync(string protocol, string host, string username, string password)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            MockStoredAccount(context, request, password);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            Assert.Equal(1, context.CredentialStore.Count);

            await provider.EraseCredentialAsync(request);

            Assert.Equal(0, context.CredentialStore.Count);
        }

        #endregion

        #region Test helpers

        private static GitRequest MockInput(string protocol, string host, string username)
        {
            return new GitRequest(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
                ["username"] = username
            });
        }

        private void VerifyOAuthFlowRan(GitRequest request, string token)
        {
            // Get new access token and refresh token
            bitbucketAuthentication.Verify(m => m.CreateOAuthCredentialsAsync(request), Times.Once);

            // Check access token works/resolve username
            bitbucketApi.Verify(m => m.GetUserInformationAsync(null, token, true), Times.Once);
        }

        private void VerifyValidateBasicAuthCredentialsNeverRan()
        {
            // Never check username/password works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(It.IsAny<string>(), It.IsAny<string>(), false), Times.Never);
        }

        private void VerifyValidateBasicAuthCredentialsRan(GitRequest request, string password)
        {
            // Check username/password works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(request.UserName, password, false), Times.Once);
        }

        private void VerifyValidateAccessTokenRan(GitRequest request, string token)
        {
            // Check tokens works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(null, token, true), Times.Once);
        }

        private void VerifyInteractiveAuthRan(GitRequest request)
        {
            var remoteUri = request.GetRemoteUri();

            bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(remoteUri, request.UserName, It.IsAny<AuthenticationModes>()), Times.Once);
        }

        private void VerifyInteractiveAuthNeverRan()
        {
            bitbucketAuthentication.Verify(m => m.GetCredentialsAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<AuthenticationModes>()), Times.Never);
        }

        private void VerifyOAuthRefreshRan(GitRequest request, string refreshToken)
        {
            // Check refresh was called
            bitbucketAuthentication.Verify(m => m.RefreshOAuthCredentialsAsync(request, refreshToken), Times.Once);
        }

        private void MockRemoteRefreshTokenValid(GitRequest request, string refreshToken, string accessToken)
        {
            bitbucketAuthentication.Setup(m => m.RefreshOAuthCredentialsAsync(request, refreshToken)).ReturnsAsync(new OAuth2TokenResult(accessToken, "access_token"));
        }

        private void MockPromptBasic(GitRequest request, string password)
        {
            var remoteUri = request.GetRemoteUri();
            bitbucketAuthentication.Setup(m => m.GetCredentialsAsync(remoteUri, request.UserName, It.IsAny<AuthenticationModes>()))
                .ReturnsAsync(new CredentialsPromptResult(AuthenticationModes.Basic, new TestCredential(request.Host, request.UserName, password)));
        }

        private void MockPromptOAuth(GitRequest request)
        {
            var remoteUri = request.GetRemoteUri();
            bitbucketAuthentication.Setup(m => m.GetCredentialsAsync(remoteUri, request.UserName, It.IsAny<AuthenticationModes>()))
                .ReturnsAsync(new CredentialsPromptResult(AuthenticationModes.OAuth));
        }

        private void MockRemoteBasicValid(GitRequest request, string password)
        {
            var userInfo = new Mock<IUserInfo>(MockBehavior.Strict);
            userInfo.Setup(ui => ui.UserName).Returns(request.UserName);

            // Basic
            bitbucketApi.Setup(x => x.GetUserInformationAsync(request.UserName, password, false))
                .ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.OK, userInfo.Object));
        }

        private void MockRemoteAccessTokenExpired(GitRequest request, string token)
        {
            // OAuth
            bitbucketApi.Setup(x => x.GetUserInformationAsync(null, token, true))
                .ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.Unauthorized));
        }

        private void MockRemoteAccessTokenValid(GitRequest request, string token)
        {
            var userInfo = new Mock<IUserInfo>(MockBehavior.Strict);
            userInfo.Setup(ui => ui.UserName).Returns(request.UserName);

            // OAuth
            bitbucketApi.Setup(x => x.GetUserInformationAsync(null, token, true))
                .ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.OK, userInfo.Object));
        }

        private static void MockRemoteOAuthAccountIsInvalid(Mock<IBitbucketRestApi> bitbucketApi)
        {
            // OAuth
            bitbucketApi.Setup(x => x.GetUserInformationAsync(null, It.IsAny<string>(), true)).ReturnsAsync(new RestApiResult<IUserInfo>(System.Net.HttpStatusCode.BadRequest));
        }

        private static void MockStoredAccount(TestCommandContext context, GitRequest request, string password)
        {
            var remoteUri = request.GetRemoteUri();
            var remoteUrl = remoteUri.AbsoluteUri.Substring(0, remoteUri.AbsoluteUri.Length - 1);
            context.CredentialStore.Add(remoteUrl, new TestCredential(request.Host, request.UserName, password));
        }

        private static void MockStoredRefreshToken(TestCommandContext context, GitRequest request, string token)
        {
            var remoteUri = request.GetRemoteUri();
            var refreshService = BitbucketHostProvider.GetRefreshTokenServiceName(remoteUri);
            context.CredentialStore.Add(refreshService, new TestCredential(refreshService, request.UserName, token));
        }

        private void MockRemoteOAuthTokenCreate(GitRequest request, string accessToken, string refreshToken)
        {
            bitbucketAuthentication.Setup(x => x.CreateOAuthCredentialsAsync(request))
                .ReturnsAsync(new OAuth2TokenResult(accessToken, "access_token") { RefreshToken = refreshToken });
        }

        private void VerifyOAuthRefreshTokenStored(TestCommandContext context, GitRequest request, string refreshToken)
        {
            var remoteUri = request.GetRemoteUri();
            string refreshService = BitbucketHostProvider.GetRefreshTokenServiceName(remoteUri);
            bool result = context.CredentialStore.TryGet(refreshService, request.UserName, out var credential);

            Assert.True(result);
            Assert.Equal(refreshToken, credential.Password);
        }

        private static Mock<IRegistry<IBitbucketRestApi>> MockRestApiRegistry(GitRequest request, Mock<IBitbucketRestApi> bitbucketApi)
        {
            var restApiRegistry = new Mock<IRegistry<IBitbucketRestApi>>(MockBehavior.Strict);

            restApiRegistry.Setup(rar => rar.Get(request)).Returns(bitbucketApi.Object);

            return restApiRegistry;
        }

        #endregion
    }
}
