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
        private const string MOCK_ROTATED_REFRESH_TOKEN = "rt-01189998819991197253";
        
        private const string MOCK_CHUNKED_ACCESS_TOKEN_DESCRIPTOR = "chunks=3";
        private const string MOCK_CHUNKED_ACCESS_TOKEN_1 = "at-12";
        private const string MOCK_CHUNKED_ACCESS_TOKEN_2 = "45678";
        private const string MOCK_CHUNKED_ACCESS_TOKEN_3 = "9";
        private const string COMBINED_CHUNKED_ACCESS_TOKEN =
            MOCK_CHUNKED_ACCESS_TOKEN_1 + MOCK_CHUNKED_ACCESS_TOKEN_2 + MOCK_CHUNKED_ACCESS_TOKEN_3;

        private const int MOCK_MAX_CREDENTIAL_SIZE = 5;
        private const string MOCK_CHUNKED_REFRESH_TOKEN_DESCRIPTOR = "chunks=2";
        private const string MOCK_CHUNKED_REFRESH_TOKEN_1 = "rt-98";
        private const string MOCK_CHUNKED_REFRESH_TOKEN_2 = "76543";
        private const string COMBINED_CHUNKED_REFRESH_TOKEN =
            MOCK_CHUNKED_REFRESH_TOKEN_1 + MOCK_CHUNKED_REFRESH_TOKEN_2;

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
            MockRemoteAccessTokenValid(username, token);

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
        
        [Theory]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_CHUNKED_ACCESS_TOKEN_DESCRIPTOR,COMBINED_CHUNKED_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_Stored_ChunkedOAuth(
            string protocol, string host, string username, string storedAccessToken, string expectedAccessToken)
        {
            GitRequest request = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            // The MaxCredentialSize is not used outside of being a gate to enforce whether to support de-chunking a token.
            context.CredentialStore.MaxCredentialSize = 100;

            if (DC_SERVER_HOST.Equals(host))
            {
                MockDCSSOEnabled();
            }
            MockStoredAccount(context, request, storedAccessToken);
            var chunkServiceName = GetAccessTokenChunkServiceName(request);
            MockStoredToken(context, chunkServiceName,
                BitbucketHostProvider.GetChunkAccount(username, 0), MOCK_CHUNKED_ACCESS_TOKEN_1);
            MockStoredToken(context, chunkServiceName,
                BitbucketHostProvider.GetChunkAccount(username, 1), MOCK_CHUNKED_ACCESS_TOKEN_2);
            MockStoredToken(context, chunkServiceName,
                BitbucketHostProvider.GetChunkAccount(username, 2), MOCK_CHUNKED_ACCESS_TOKEN_3);
            
            MockRemoteAccessTokenValid(username, expectedAccessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(expectedAccessToken, credential.Password);

            // Verify bitbucket.org credentials were validated
            VerifyValidateAccessTokenRan(request, expectedAccessToken);

            // Stored credentials so don't ask for more
            VerifyInteractiveAuthNeverRan();
        }
        
        [Theory]
        // Cloud
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_CHUNKED_ACCESS_TOKEN_DESCRIPTOR)]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_IgnoresChunkedOAuthForUnsupportedCredentialStore(
            string protocol, string host, string username, string token)
        {
            // This is a contrived test - but it's showing that the password even though it's got the appearance of
            // being chunked, is not read as chunked password, it's passed directly on and the CredentialStore doesn't
            // need to support chunking.
            GitRequest request = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            // Credential store doesn't support chunking
            context.CredentialStore.MaxCredentialSize = 0;

            if (DC_SERVER_HOST.Equals(host))
            {
                MockDCSSOEnabled();
            }
            MockStoredAccount(context, request, token);
            
            MockRemoteAccessTokenValid(username, token);

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
            GitRequest request = MockInput(protocol, host, null);

            var context = new TestCommandContext();
            MockPromptOAuth(request);
            MockRemoteOAuthTokenCreate(request, accessToken, refreshToken);
            MockRemoteAccessTokenValid(username, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyInteractiveAuthRan(request);
            VerifyOAuthFlowRan(request, accessToken);
            VerifyValidateAccessTokenRan(request, accessToken);

            var refreshService = GetRefreshTokenServiceNameForRequest(request);
            VerifyOAuthTokenStored(context, refreshService, username, refreshToken);
        }


        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_Valid_New_OAuth_With_Null_Request_Username(
            string protocol, string host, string username, string refreshToken, string accessToken)
        {
            GitRequest request = MockInput(protocol, host, null);

            var context = new TestCommandContext();
            MockPromptOAuth(request);
            MockRemoteOAuthTokenCreate(request, accessToken, refreshToken);
            MockRemoteAccessTokenValid(username, accessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyInteractiveAuthRan(request);
            VerifyOAuthFlowRan(request, accessToken);
            VerifyValidateAccessTokenRan(request, accessToken);
            
            var refreshService = GetRefreshTokenServiceNameForRequest(request);
            VerifyOAuthTokenStored(context, refreshService, username, refreshToken);
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
            MockStoredRefreshTokenForRequest(context, request, refreshToken);
            MockRemoteAccessTokenValid(username, accessToken);
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
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN, MOCK_ROTATED_REFRESH_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_Invalid_MalformedChunkedAT_OAuth_Refresh(
            string protocol, string host, string username, string refreshToken, string rotatedAccessToken, string rotatedRefreshToken)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();

            // A chunked AT record exists, but we're missing a part of it. This is treated as if we have no AT
            var chunkServiceName = GetAccessTokenChunkServiceName(request);
            MockStoredToken(context, chunkServiceName,
                BitbucketHostProvider.GetChunkAccount(username, 0), MOCK_CHUNKED_ACCESS_TOKEN_1);
            MockStoredToken(context, chunkServiceName,
                BitbucketHostProvider.GetChunkAccount(username, 1), MOCK_CHUNKED_ACCESS_TOKEN_2);
            
            MockStoredRefreshTokenForRequest(context, request, refreshToken);
            MockRemoteAccessTokenValid(username, rotatedAccessToken);
            MockRemoteRefreshTokenValid(request, refreshToken, rotatedAccessToken, rotatedRefreshToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(rotatedAccessToken, credential.Password);

            VerifyValidateAccessTokenRan(request, rotatedAccessToken);
            VerifyOAuthRefreshRan(request, refreshToken);
            VerifyInteractiveAuthNeverRan();

            var refreshService = GetRefreshTokenServiceNameForRequest(request);
            VerifyOAuthTokenStored(context, refreshService, username, rotatedRefreshToken);
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

            MockStoredRefreshTokenForRequest(context, request, refreshToken);
            MockRemoteAccessTokenValid(username, accessToken);
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
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_EXPIRED_ACCESS_TOKEN, MOCK_CHUNKED_REFRESH_TOKEN_DESCRIPTOR, COMBINED_CHUNKED_REFRESH_TOKEN, MOCK_ACCESS_TOKEN, MOCK_ROTATED_REFRESH_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_ExpiredAT_OAuth_Refresh_WithChunkedRT(
            string protocol, string host, string username, string expiredAccessToken, string storedRefreshToken, string expectedRefreshToken, string rotatedAccessToken, string rotatedRefreshToken)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            context.CredentialStore.MaxCredentialSize = 100;

            // AT exists but has expired, but RT is still valid
            MockStoredAccount(context, request, expiredAccessToken);
            
            var refreshTokenChunkServiceName = GetRefreshTokenChunkServiceName(request);
            MockStoredToken(context, refreshTokenChunkServiceName,
                BitbucketHostProvider.GetChunkAccount(username, 0), MOCK_CHUNKED_REFRESH_TOKEN_1);
            MockStoredToken(context, refreshTokenChunkServiceName,
                BitbucketHostProvider.GetChunkAccount(username, 1), MOCK_CHUNKED_REFRESH_TOKEN_2);
            MockStoredRefreshTokenForRequest(context, request, storedRefreshToken);
            
            MockRemoteAccessTokenExpired(request, expiredAccessToken);
            MockRemoteAccessTokenValid(username, rotatedAccessToken);
            MockRemoteRefreshTokenValid(request, expectedRefreshToken, rotatedAccessToken, rotatedRefreshToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(rotatedAccessToken, credential.Password);

            VerifyValidateAccessTokenRan(request, rotatedAccessToken);
            VerifyOAuthRefreshRan(request, expectedRefreshToken);
            VerifyInteractiveAuthNeverRan();
            VerifyOAuthTokenStored(context, GetRefreshTokenServiceNameForRequest(request), username, rotatedRefreshToken);
        }
        
        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_EXPIRED_ACCESS_TOKEN, MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN, COMBINED_CHUNKED_REFRESH_TOKEN, MOCK_MAX_CREDENTIAL_SIZE)]
        public async Task BitbucketHostProvider_GetCredentialAsync_ExpiredAT_OAuth_Refresh_StoresChunkedRT(
            string protocol, string host, string username, string expiredAccessToken, string refreshToken, string accessToken, string rotatedRefreshToken, int maxCredentialSize)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            context.CredentialStore.MaxCredentialSize = maxCredentialSize;

            // AT exists but has expired, but RT is still valid
            MockStoredAccount(context, request, expiredAccessToken);
            MockStoredRefreshTokenForRequest(context, request, refreshToken);
            
            MockRemoteAccessTokenExpired(request, expiredAccessToken);
            MockRemoteAccessTokenValid(username, accessToken);
            // After refreshing we're given back a wide rotated refresh token which would need chunking on supported credentialStores
            MockRemoteRefreshTokenValid(request, refreshToken, accessToken, rotatedRefreshToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyValidateAccessTokenRan(request, accessToken);
            VerifyOAuthRefreshRan(request, refreshToken);
            VerifyInteractiveAuthNeverRan();
            // Stored RT as chunked
            VerifyOAuthTokenStored(context, GetRefreshTokenServiceNameForRequest(request), username, MOCK_CHUNKED_REFRESH_TOKEN_DESCRIPTOR);
            var chunkRtServiceName = GetRefreshTokenChunkServiceName(request);
            VerifyOAuthTokenStored(context, chunkRtServiceName, $"{username}_0", MOCK_CHUNKED_REFRESH_TOKEN_1);
            VerifyOAuthTokenStored(context, chunkRtServiceName, $"{username}_1", MOCK_CHUNKED_REFRESH_TOKEN_2);
        }
        
        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_EXPIRED_ACCESS_TOKEN, MOCK_REFRESH_TOKEN, MOCK_ACCESS_TOKEN, COMBINED_CHUNKED_REFRESH_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_ExpiredAT_OAuth_Refresh_DoesNotChunkLargeRTForUnsupportedCredentialStore(
            string protocol, string host, string username, string expiredAccessToken, string refreshToken, string accessToken,  string rotatedRefreshToken)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            // Credential store doesn't support chunking
            context.CredentialStore.MaxCredentialSize = 0;

            // AT exists but has expired, but RT is still valid
            MockStoredAccount(context, request, expiredAccessToken);
            MockStoredRefreshTokenForRequest(context, request, refreshToken);
            
            MockRemoteAccessTokenExpired(request, expiredAccessToken);
            MockRemoteAccessTokenValid(username, accessToken);
            MockRemoteRefreshTokenValid(request, refreshToken, accessToken, rotatedRefreshToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(accessToken, credential.Password);

            VerifyValidateAccessTokenRan(request, accessToken);
            VerifyOAuthRefreshRan(request, refreshToken);
            VerifyInteractiveAuthNeverRan();
            // Stored RT without chunking
            VerifyOAuthTokenStored(context, GetRefreshTokenServiceNameForRequest(request), username, rotatedRefreshToken);
        }
        
        [Theory]
        // DC/Server does not currently support OAuth
        [InlineData("https", BITBUCKET_DOT_ORG_HOST, "jsquire", MOCK_EXPIRED_ACCESS_TOKEN, MOCK_ACCESS_TOKEN, MOCK_REFRESH_TOKEN)]
        public async Task BitbucketHostProvider_GetCredentialAsync_ExpiredAT_OAuth_InvalidChunkedRefresh_PromptsConsent(
            string protocol, string host, string username, string expiredAccessToken, string rotatedAccessToken, string rotatedRefreshToken)
        {
            var request = MockInput(protocol, host, username);

            var context = new TestCommandContext();
            context.CredentialStore.MaxCredentialSize = 100;

            // AT exists but has expired.
            MockStoredAccount(context, request, expiredAccessToken);
            // RT is missing a chunk - so it should be ignored
            var refreshTokenChunkServiceName = GetRefreshTokenChunkServiceName(request);
            MockStoredToken(context, refreshTokenChunkServiceName,
                BitbucketHostProvider.GetChunkAccount(username, 0), MOCK_CHUNKED_REFRESH_TOKEN_1);
            MockStoredRefreshTokenForRequest(context, request, MOCK_CHUNKED_REFRESH_TOKEN_DESCRIPTOR);
            
            
            MockRemoteAccessTokenExpired(request, expiredAccessToken);
            MockPromptOAuth(request);
            // On the OAuth prompt - we will create and store the rotated refresh token
            MockRemoteOAuthTokenCreate(request, rotatedAccessToken, rotatedRefreshToken);
            MockRemoteAccessTokenValid(username, rotatedAccessToken);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            var result = await provider.GetCredentialAsync(request);
            ICredential credential = result.Credential;

            Assert.Equal(username, credential.Account);
            Assert.Equal(rotatedAccessToken, credential.Password);

            VerifyInteractiveAuthRan(request);
            VerifyOAuthFlowRan(request, rotatedAccessToken);
            VerifyValidateAccessTokenRan(request, rotatedAccessToken);

            var refreshService = GetRefreshTokenServiceNameForRequest(request);
            VerifyOAuthTokenStored(context, refreshService, username, rotatedRefreshToken);
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
            MockStoredRefreshTokenForRequest(context, request, refreshToken);
            MockRemoteAccessTokenValid(username, accessToken);
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
            MockStoredRefreshTokenForRequest(context, request, refreshToken);
            MockRemoteAccessTokenValid(username, newToken);
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
        [InlineData("https", BITBUCKET_DOT_ORG_HOST,  "jsquire",COMBINED_CHUNKED_ACCESS_TOKEN, MOCK_MAX_CREDENTIAL_SIZE)]
        public async Task BitbucketHostProvider_StoreCredentialAsync_SplitsIntoChunksForSupportedCredentialStores(string protocol, string host, string username, string password, int credentialWidth)
        {
            var request = MockInput(protocol, host, username, password);
            var context = new TestCommandContext();
            context.CredentialStore.MaxCredentialSize = credentialWidth;
            

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            Assert.Equal(0, context.CredentialStore.Count);

            await provider.StoreCredentialAsync(request);
            // The chunk descriptor and 3 chunks
            Assert.Equal(4, context.CredentialStore.Count);
            var serviceName = GetAccessTokenServiceName(request);
            VerifyOAuthTokenStored(context, serviceName, username, MOCK_CHUNKED_ACCESS_TOKEN_DESCRIPTOR);
            var chunkServiceName = GetAccessTokenChunkServiceName(request);
            VerifyOAuthTokenStored(context, chunkServiceName, $"{username}_0", MOCK_CHUNKED_ACCESS_TOKEN_1);
            VerifyOAuthTokenStored(context, chunkServiceName, $"{username}_1", MOCK_CHUNKED_ACCESS_TOKEN_2);
            VerifyOAuthTokenStored(context, chunkServiceName, $"{username}_2", MOCK_CHUNKED_ACCESS_TOKEN_3);
        }
        
        [Theory]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST,"jsquire", COMBINED_CHUNKED_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_StoreCredentialAsync_DoesNotChunk(string protocol, string host,string username, string password) 
        {
            var request = MockInput(protocol, host, username, password);

            var context = new TestCommandContext();
            // Credential store doesn't support chunking
            context.CredentialStore.MaxCredentialSize = 0;

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            Assert.Equal(0, context.CredentialStore.Count);

            await provider.StoreCredentialAsync(request);
            Assert.Equal(1, context.CredentialStore.Count);
            var serviceName = GetAccessTokenServiceName(request);
            VerifyOAuthTokenStored(context, serviceName, username, password);
        }
        
        [Theory]
        [InlineData("https", BITBUCKET_DOT_ORG_HOST,"jsquire", COMBINED_CHUNKED_ACCESS_TOKEN)]
        public async Task BitbucketHostProvider_StoreCredentialAsync_DoesNotChunkForSecretsSmallerThanMaximumCredentialSize(string protocol, string host,string username, string password) 
        {
            var request = MockInput(protocol, host, username, password);

            var context = new TestCommandContext();
            // Set the length to the exact size of the password we're trying to store. We can fully fit this secret and thus won't chunk
            context.CredentialStore.MaxCredentialSize = password.Length;

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, MockRestApiRegistry(request, bitbucketApi).Object);

            Assert.Equal(0, context.CredentialStore.Count);

            await provider.StoreCredentialAsync(request);
            Assert.Equal(1, context.CredentialStore.Count);
            var serviceName = GetAccessTokenServiceName(request);
            VerifyOAuthTokenStored(context, serviceName, username, password);
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

        private static GitRequest MockInput(string protocol, string host, string username, string password=null)
        {
            var payload = new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
                ["username"] = username,
            };
            if (password != null)
            {
                payload["password"] = password;
            }
                
            return new GitRequest(payload);
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

        private void MockRemoteRefreshTokenValid(GitRequest request, string refreshToken, string accessToken, string rotatedRefreshToken = null)
        {
            bitbucketAuthentication.Setup(m => m.RefreshOAuthCredentialsAsync(request, refreshToken)).ReturnsAsync(
                new OAuth2TokenResult(accessToken, "access_token") { RefreshToken = rotatedRefreshToken });
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

        private void MockRemoteAccessTokenValid(string username, string token)
        {
            var userInfo = new Mock<IUserInfo>(MockBehavior.Strict);
            userInfo.Setup(ui => ui.UserName).Returns(username);

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

        private static void MockStoredRefreshTokenForRequest(TestCommandContext context, GitRequest request, string token)
        {
            var remoteUri = request.GetRemoteUri();
            var refreshService = BitbucketHostProvider.GetRefreshTokenServiceName(remoteUri);
            MockStoredToken(context, refreshService, request.UserName, token);
        }
        private static void MockStoredToken(TestCommandContext context, String serviceName, String account, string token)
        {
            context.CredentialStore.Add(serviceName, new TestCredential(serviceName, account, token));
        }

        private string GetRefreshTokenServiceNameForRequest(GitRequest request)
        {
            var remoteUri = request.GetRemoteUri();
            return BitbucketHostProvider.GetRefreshTokenServiceName(remoteUri);
        }
        
        private string GetRefreshTokenChunkServiceName(GitRequest request)
        {
            var remoteUri = request.GetRemoteUri();
            var refreshService = BitbucketHostProvider.GetRefreshTokenServiceName(remoteUri);
            return BitbucketHostProvider.GetChunkServiceName(refreshService);
        }
        
        private string GetAccessTokenChunkServiceName(GitRequest request)
        {
            var service = GetAccessTokenServiceName(request);
            return BitbucketHostProvider.GetChunkServiceName(service);
        }
        private string GetAccessTokenServiceName(GitRequest request)
        {
            var remoteUri = request.GetRemoteUri();
            return BitbucketHostProvider.GetServiceName(remoteUri);
        }


        private void MockRemoteOAuthTokenCreate(GitRequest request, string accessToken, string refreshToken)
        {
            bitbucketAuthentication.Setup(x => x.CreateOAuthCredentialsAsync(request))
                .ReturnsAsync(new OAuth2TokenResult(accessToken, "access_token") { RefreshToken = refreshToken });
        }

        private void VerifyOAuthTokenStored(TestCommandContext context, string serviceName, string expectedAccount, string expectedToken)
        {
            bool result = context.CredentialStore.TryGet(serviceName, expectedAccount, out var credential);

            Assert.True(result);
            Assert.Equal(expectedAccount, credential.Account);
            Assert.Equal(expectedToken, credential.Password);
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
