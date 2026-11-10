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
