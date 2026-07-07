using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.DataCenter;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Atlassian.Bitbucket.Tests.DataCenter
{
    public class BitbucketOAuth2ClientTest
    {
        private Mock<HttpClient> httpClient = new Mock<HttpClient>(MockBehavior.Strict);
        private Mock<ISettings> settings = new Mock<ISettings>(MockBehavior.Loose);
        private Mock<IOAuth2WebBrowser> browser = new Mock<IOAuth2WebBrowser>(MockBehavior.Strict);
        private Mock<IOAuth2CodeGenerator> codeGenerator = new Mock<IOAuth2CodeGenerator>(MockBehavior.Strict);
        private CancellationToken ct = new CancellationToken();
        private Uri rootCallbackUri = new Uri("http://localhost:34106/");
        private string nonce = "12345";
        private string pkceCodeVerifier = "abcde";
        private string pkceCodeChallenge = "xyz987";
        private string authorization_code = "authorization_token";

        [Fact]
        public async Task BitbucketOAuth2Client_GetAuthorizationCodeAsync_ReturnsCode()
        {
            var remoteUrl = MockRemoteUri("http://example.com");
            var clientId = MockClientIdOverride("dc-client-id");
            MockClientSecretOverride("dc-client-seccret");

            Uri finalCallbackUri = MockFinalCallbackUri(rootCallbackUri);

            var client = GetBitbucketOAuth2Client();

            MockGetAuthenticationCodeAsync(remoteUrl, rootCallbackUri, finalCallbackUri, clientId, client.Scopes);

            MockCodeGenerator();

            var result = await client.GetAuthorizationCodeAsync(browser.Object, ct);

            VerifyAuthorizationCodeResult(result, rootCallbackUri);
        }

        [Fact]
        public async Task BitbucketOAuth2Client_GetAuthorizationCodeAsync_ReturnsCode_WhileRespectingRedirectUriOverride()
        {
            var rootCallbackUrl = MockRootCallbackUriOverride("http://localhost:12345/");
            var remoteUrl = MockRemoteUri("http://example.com");
            var clientId = MockClientIdOverride("dc-client-id");
            MockClientSecretOverride("dc-client-seccret");

            Uri finalCallbackUri = MockFinalCallbackUri(new Uri(rootCallbackUrl));

            var client = GetBitbucketOAuth2Client();

            MockGetAuthenticationCodeAsync(remoteUrl, new Uri(rootCallbackUrl), finalCallbackUri, clientId, client.Scopes);

            MockCodeGenerator();

            var result = await client.GetAuthorizationCodeAsync(browser.Object, ct);

            VerifyAuthorizationCodeResult(result, new Uri(rootCallbackUrl));
        }

        private void VerifyAuthorizationCodeResult(OAuth2AuthorizationCodeResult result, Uri redirectUri)
        {
            Assert.NotNull(result);
            Assert.Equal(authorization_code, result.Code);
            Assert.Equal(redirectUri, result.RedirectUri);
            Assert.Equal(pkceCodeVerifier, result.CodeVerifier);
        }

        private Bitbucket.DataCenter.BitbucketOAuth2Client GetBitbucketOAuth2Client()
        {
            var trace2 = new NullTrace2();
            var client = new Bitbucket.DataCenter.BitbucketOAuth2Client(httpClient.Object, settings.Object, trace2);
            client.CodeGenerator = codeGenerator.Object;
            return client;
        }

        private void MockCodeGenerator()
        {
            codeGenerator.Setup(c => c.CreateNonce()).Returns(nonce);
            codeGenerator.Setup(c => c.CreatePkceCodeVerifier()).Returns(pkceCodeVerifier);
            codeGenerator.Setup(c => c.CreatePkceCodeChallenge(OAuth2PkceChallengeMethod.Sha256, pkceCodeVerifier)).Returns(pkceCodeChallenge);
        }

        private void MockGetAuthenticationCodeAsync(string url, Uri redirectUri, Uri finalCallbackUri, string overrideClientId, IEnumerable<string> scopes)
        {
            var authorizationUri = new UriBuilder(url + "/rest/oauth2/latest/authorize")
            {
                Query = "?response_type=code"
             + "&client_id=" + (overrideClientId ?? "clientId")
             + "&state=12345"
             + "&code_challenge_method=" + OAuth2Constants.AuthorizationEndpoint.PkceChallengeMethodS256
             + "&code_challenge=" + WebUtility.UrlEncode(pkceCodeChallenge).ToLower()
             + "&redirect_uri=" + WebUtility.UrlEncode(redirectUri.AbsoluteUri).ToLower()
             + "&scope=" + WebUtility.UrlEncode(string.Join(" ", scopes)).ToUpper()
            }.Uri;

            browser.Setup(b => b.GetAuthenticationCodeAsync(authorizationUri, redirectUri, ct)).Returns(Task.FromResult(finalCallbackUri));
        }

        private Uri MockFinalCallbackUri(Uri redirectUri)
        {
            var finalUri = new Uri(rootCallbackUri, "?state=" + nonce + "&code=" + authorization_code);
            // This is a simplification but consistent
            browser.Setup(b => b.UpdateRedirectUri(redirectUri)).Returns(redirectUri);
            return finalUri;
        }

        private string MockRemoteUri(string value)
        {
            settings.Setup(s => s.RemoteUri).Returns(new Uri(value));
            return value;
        }

        private string MockClientIdOverride(string value)
        {
            settings.Setup(s => s.TryGetSetting(
                DataCenterConstants.EnvironmentVariables.OAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, DataCenterConstants.GitConfiguration.Credential.OAuthClientId,
                out value)).Returns(true);
            return value;
        }

        private string MockClientSecretOverride(string value)
        {
            settings.Setup(s => s.TryGetSetting(
                DataCenterConstants.EnvironmentVariables.OAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, DataCenterConstants.GitConfiguration.Credential.OAuthClientSecret,
                out value)).Returns(true);
            return value;
        }

        private string MockRootCallbackUriOverride(string value)
        {
            settings.Setup(s => s.TryGetSetting(
                DataCenterConstants.EnvironmentVariables.OAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, DataCenterConstants.GitConfiguration.Credential.OAuthRedirectUri,
                out value)).Returns(true);
            return value;
        }
    }
}