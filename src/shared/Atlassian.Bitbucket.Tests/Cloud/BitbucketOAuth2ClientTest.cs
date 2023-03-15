using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.Cloud;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Atlassian.Bitbucket.Tests.Cloud
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

            Bitbucket.Cloud.BitbucketOAuth2Client client = GetBitbucketOAuth2Client();

            MockGetAuthenticationCodeAsync(finalCallbackUri, null, client.Scopes);

            MockCodeGenerator();

            var result = await client.GetAuthorizationCodeAsync(browser.Object, ct);

            VerifyAuthorizationCodeResult(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("i234")]
        public async Task BitbucketOAuth2Client_GetAuthorizationCodeAsync_RespectsClientIdOverride_ReturnsCode(string clientId)
        {
            MockClientIdOverride(clientId != null, clientId);

            Uri finalCallbackUri = MockFinalCallbackUri();

            Bitbucket.Cloud.BitbucketOAuth2Client client = GetBitbucketOAuth2Client();

            MockGetAuthenticationCodeAsync(finalCallbackUri, clientId, client.Scopes);

            MockCodeGenerator();

            var result = await client.GetAuthorizationCodeAsync(browser.Object, ct);

            VerifyAuthorizationCodeResult(result);
        }

        [Fact]
        public async Task BitbucketOAuth2Client_GetDeviceCodeAsync()
        {
            var trace2 = new NullTrace2();
            var client = new Bitbucket.Cloud.BitbucketOAuth2Client(httpClient.Object, settings.Object, trace2);
            await Assert.ThrowsAsync<Trace2InvalidOperationException>(async () => await client.GetDeviceCodeAsync(scopes, ct));
        }

        [Theory]
        [InlineData("https", "example.com", "john", "https://example.com/refresh_token")]
        [InlineData("http", "example.com", "john", "http://example.com/refresh_token")]
        [InlineData("https", "example.com", "dave", "https://example.com/refresh_token")]
        [InlineData("https", "example.com/", "john", "https://example.com/refresh_token")]
        public void BitbucketOAuth2Client_GetRefreshTokenServiceName(string protocol, string host, string username, string expectedResult)
        {
            var trace2 = new NullTrace2();
            var client = new Bitbucket.Cloud.BitbucketOAuth2Client(httpClient.Object, settings.Object, trace2);
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
                ["username"] = username
            });
            Assert.Equal(expectedResult, client.GetRefreshTokenServiceName(input));
        }


        private void VerifyAuthorizationCodeResult(OAuth2AuthorizationCodeResult result)
        {
            Assert.NotNull(result);
            Assert.Equal(authorization_code, result.Code);
            Assert.Equal(rootCallbackUri, result.RedirectUri);
            Assert.Equal(pkceCodeVerifier, result.CodeVerifier);
        }

        private Bitbucket.Cloud.BitbucketOAuth2Client GetBitbucketOAuth2Client()
        {
            var trace2 = new NullTrace2();
            var client = new Bitbucket.Cloud.BitbucketOAuth2Client(httpClient.Object, settings.Object, trace2);
            client.CodeGenerator = codeGenerator.Object;
            return client;
        }

        private void MockCodeGenerator()
        {
            codeGenerator.Setup(c => c.CreateNonce()).Returns(nonce);
            codeGenerator.Setup(c => c.CreatePkceCodeVerifier()).Returns(pkceCodeVerifier);
            codeGenerator.Setup(c => c.CreatePkceCodeChallenge(OAuth2PkceChallengeMethod.Sha256, pkceCodeVerifier)).Returns(pkceCodeChallenge);
        }

        private void MockGetAuthenticationCodeAsync(Uri finalCallbackUri, string overrideClientId, IEnumerable<string> scopes)
        {
            var authorizationUri = new UriBuilder(CloudConstants.OAuth2AuthorizationEndpoint)
            {
                Query = "?response_type=code"
             + "&client_id=" + (overrideClientId ?? CloudConstants.OAuth2ClientId)
             + "&state=12345"
             + "&code_challenge_method=" + OAuth2Constants.AuthorizationEndpoint.PkceChallengeMethodS256
             + "&code_challenge=" + WebUtility.UrlEncode(pkceCodeChallenge).ToLower()
             + "&redirect_uri=" + WebUtility.UrlEncode(rootCallbackUri.AbsoluteUri).ToLower()
             + "&scope=" + WebUtility.UrlEncode(string.Join(" ", scopes)).ToLower()
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
                CloudConstants.EnvironmentVariables.OAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, CloudConstants.GitConfiguration.Credential.OAuthClientId,
                out value)).Returns(set);
            return value;
        }
    }
}