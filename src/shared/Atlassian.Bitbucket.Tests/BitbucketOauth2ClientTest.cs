using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using Moq;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketOAuth2ClientTest
    {
        private Mock<HttpClient> httpClient = new Mock<HttpClient>(MockBehavior.Strict);
        private Mock<ISettings> settings = new Mock<ISettings>(MockBehavior.Loose);
        private Mock<IOAuth2WebBrowser> browser = new Mock<IOAuth2WebBrowser>(MockBehavior.Strict);
        private Mock<IOAuth2CodeGenerator> codeGenerator = new Mock<IOAuth2CodeGenerator>(MockBehavior.Strict);
        private IEnumerable<string> scopes = new List<string>();
        private CancellationToken ct = new CancellationToken();
        private Uri rootCallbackUri = new Uri("http://localhost:34106/");
        private string nonce = "12345";
        private string pkceCodeVerifier = "abcde";
        private string pkceCodeChallenge = "xyz987";
        private string authorization_code = "authorization_token";

        [Fact]
        public async Task BitbucketOAuth2Client_GetAuthorizationCodeAsync_ReturnsCode()
        {
            MockClientIdOverride(false, "never used");

            Uri finalCallbackUri = MockFinalCallbackUri();

            MockGetAuthenticationCodeAsync(finalCallbackUri, null);

            MockCodeGenerator();

            BitbucketOAuth2Client client = GetBitbucketOAuth2Client();

            var result = await client.GetAuthorizationCodeAsync(scopes, browser.Object, ct);

            VerifyAuthorizationCodeResult(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("i234")]
        public async Task BitbucketOAuth2Client_GetAuthorizationCodeAsync_RespectsClientIdOverride_ReturnsCode(string clientId)
        {
            MockClientIdOverride(clientId != null, clientId);

            Uri finalCallbackUri = MockFinalCallbackUri();

            MockGetAuthenticationCodeAsync(finalCallbackUri, clientId);

            MockCodeGenerator();

            BitbucketOAuth2Client client = GetBitbucketOAuth2Client();

            var result = await client.GetAuthorizationCodeAsync(scopes, browser.Object, ct);

            VerifyAuthorizationCodeResult(result);
        }

        [Fact]
        public async Task BitbucketOAuth2Client_GetDeviceCodeAsync()
        {
            var client = new BitbucketOAuth2Client(httpClient.Object, settings.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetDeviceCodeAsync(scopes, ct));
        }

        private void VerifyOAuth2TokenResult(OAuth2TokenResult result)
        {
            Assert.NotNull(result);
            IEnumerable<char> access_token = null;
            Assert.Equal(access_token, result.AccessToken);
            IEnumerable<char> refresh_token = null;
            Assert.Equal(refresh_token, result.RefreshToken);
            IEnumerable<char> tokenType = null;
            Assert.Equal(tokenType, result.TokenType);
            Assert.Equal(null, result.Scopes);
        }

        private void VerifyAuthorizationCodeResult(OAuth2AuthorizationCodeResult result)
        {
            Assert.NotNull(result);
            Assert.Equal(authorization_code, result.Code);
            Assert.Equal(rootCallbackUri, result.RedirectUri);
            Assert.Equal(pkceCodeVerifier, result.CodeVerifier);
        }

        private BitbucketOAuth2Client GetBitbucketOAuth2Client()
        {
            var client = new BitbucketOAuth2Client(httpClient.Object, settings.Object);
            client.CodeGenerator = codeGenerator.Object;
            return client;
        }

        private void MockCodeGenerator()
        {
            codeGenerator.Setup(c => c.CreateNonce()).Returns(nonce);
            codeGenerator.Setup(c => c.CreatePkceCodeVerifier()).Returns(pkceCodeVerifier);
            codeGenerator.Setup(c => c.CreatePkceCodeChallenge(OAuth2PkceChallengeMethod.Sha256, pkceCodeVerifier)).Returns(pkceCodeChallenge);
        }

        private void MockGetAuthenticationCodeAsync(Uri finalCallbackUri, string overrideClientId)
        {
            var authorizationUri = new UriBuilder(BitbucketConstants.OAuth2AuthorizationEndpoint)
            {
                Query = "?response_type=code"
             + "&client_id=" + (overrideClientId ?? BitbucketConstants.OAuth2ClientId)
             + "&state=12345"
             + "&code_challenge_method=" + OAuth2Constants.AuthorizationEndpoint.PkceChallengeMethodS256
             + "&code_challenge=" + WebUtility.UrlEncode(pkceCodeChallenge).ToLower()
             + "&redirect_uri=" + WebUtility.UrlEncode(rootCallbackUri.AbsoluteUri).ToLower()
            }.Uri;

            browser.Setup(b => b.GetAuthenticationCodeAsync(authorizationUri, rootCallbackUri, ct)).Returns(Task.FromResult(finalCallbackUri));
        }

        private Uri MockFinalCallbackUri()
        {
            var finalUri = new Uri(rootCallbackUri, "?state=" + nonce + "&code=" + authorization_code);
            browser.Setup(b => b.UpdateRedirectUri(rootCallbackUri)).Returns(rootCallbackUri);
            return finalUri;
        }

        private string MockeClientIdOverride(bool set)
        {
            return MockClientIdOverride(set, null);
        }
        private string MockClientIdOverride(bool set, string value)
        {
            settings.Setup(s => s.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.DevOAuthClientId,
                out value)).Returns(set);
            return value;
        }
    }
}