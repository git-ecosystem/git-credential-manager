using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitHub.Tests
{
    public class GitHubOAuth2ClientTests
    {
        [Fact]
        public async Task GitHubOAuth2Client_RefreshToken_ParsesRotatingTokenPair()
        {
            using var handler = new TestHttpMessageHandler();
            using var httpClient = new HttpClient(handler);
            var client = new GitHubOAuth2Client(httpClient, new TestSettings(), new Uri("https://github.com"));
            handler.Setup(HttpMethod.Post, new Uri("https://github.com/login/oauth/access_token"), async request =>
            {
                string body = await request.Content.ReadAsStringAsync();
                Assert.Contains("grant_type=refresh_token", body);
                Assert.Contains("refresh_token=old-refresh", body);
                Assert.Contains($"client_id={GitHubConstants.OAuthClientId}", body);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {"access_token":"new-access","token_type":"bearer","refresh_token":"new-refresh",
                         "expires_in":28800,"refresh_token_expires_in":15897600}
                        """)
                };
            });

            OAuth2TokenResult result = await client.GetTokenByRefreshTokenAsync("old-refresh", CancellationToken.None);
            Assert.Equal("new-access", result.AccessToken);
            Assert.Equal("new-refresh", result.RefreshToken);
            Assert.Equal(TimeSpan.FromHours(8), result.ExpiresIn);
            Assert.Equal(TimeSpan.FromSeconds(15897600), result.RefreshTokenExpiresIn);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.BadRequest)]
        public async Task GitHubOAuth2Client_RefreshToken_ExposesTerminalErrorCode(HttpStatusCode status)
        {
            using var handler = new TestHttpMessageHandler();
            using var httpClient = new HttpClient(handler);
            var client = new GitHubOAuth2Client(httpClient, new TestSettings(), new Uri("https://github.com"));
            handler.Setup(HttpMethod.Post, new Uri("https://github.com/login/oauth/access_token"), status,
                """{"error":"bad_refresh_token","error_description":"Refresh token expired"}""");
            OAuth2Exception exception = await Assert.ThrowsAsync<OAuth2Exception>(
                () => client.GetTokenByRefreshTokenAsync("old-refresh", CancellationToken.None));
            Assert.Equal("bad_refresh_token", exception.ErrorCode);
        }

        [Fact]
        public async Task GitHubOAuth2Client_CommaSeparatedScopes()
        {
            const string authCode = "test-auth-code";
            const string responseJson = """
                {
                  "access_token": "test-token",
                  "token_type": "bearer",
                  "scope": "repo,gist,workflow"
                }
                """;

            string[] expectedScopes = ["repo", "gist", "workflow"];

            using var httpHandler = new TestHttpMessageHandler();
            var httpClient = new HttpClient(httpHandler);
            var settings = new TestSettings();
            Uri baseUri = new("https://github.com/");
            var client = new GitHubOAuth2Client(httpClient, settings, baseUri);

            // Mock the HTTP response for the access token request
            httpHandler.Setup(HttpMethod.Post, new Uri(baseUri, "login/oauth/access_token"),
                HttpStatusCode.OK, responseJson);

            var authCodeResult = new OAuth2AuthorizationCodeResult(authCode);
            OAuth2TokenResult result = await client.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);

            Assert.Equal(expectedScopes, result.Scopes);
        }
    }
}
