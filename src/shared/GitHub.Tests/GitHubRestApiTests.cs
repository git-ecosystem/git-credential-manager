// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace GitHub.Tests
{
    public class GitHubRestApiTests
    {
        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_NullUri_ThrowsException()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testAuthCode = "1234";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var api = new GitHubRestApi(context);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => api.AcquireTokenAsync(null, testUserName, testPassword, testAuthCode, testScopes)
            );
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_NoNetwork_ThrowsException()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testAuthCode = "1234";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            var httpHandler = new TestHttpMessageHandler {SimulateNoNetwork = true};

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            await Assert.ThrowsAsync<HttpRequestException>(
                () => api.AcquireTokenAsync(uri, testUserName, testPassword, testAuthCode, testScopes)
            );
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_ValidRequestOK_ReturnsToken()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testAuthCode = "1234";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://github.com");

            var expectedRequestUri = new Uri("https://api.github.com/authorizations");

            const string expectedTokenValue = "GITHUB_TOKEN_VALUE";
            string tokenResponseJson = $"{{ \"token\": \"{expectedTokenValue}\" }}";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(tokenResponseJson)
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Post, expectedRequestUri, request =>
            {
                AssertBasicAuth(request, testUserName, testPassword);
                AssertAuthCode(request, testAuthCode);
                return httpResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            AuthenticationResult authResult = await api.AcquireTokenAsync(
                uri, testUserName, testPassword, testAuthCode, testScopes);

            Assert.Equal(GitHubAuthenticationResultType.Success, authResult.Type);
            Assert.NotNull(authResult.Token);
            Assert.Equal(expectedTokenValue, authResult.Token.Password);
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_ValidRequestOKBadJson_ReturnsFailure()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testAuthCode = "1234";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://github.com");

            var expectedRequestUri = new Uri("https://api.github.com/authorizations");

            const string tokenResponseJson = "ThisIsBadJSON";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(tokenResponseJson)
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Post, expectedRequestUri, request =>
            {
                AssertBasicAuth(request, testUserName, testPassword);
                AssertAuthCode(request, testAuthCode);
                return httpResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            AuthenticationResult authResult = await api.AcquireTokenAsync(
                uri, testUserName, testPassword, testAuthCode, testScopes);

            Assert.Equal(GitHubAuthenticationResultType.Failure, authResult.Type);
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_ValidRequestCreated_ReturnsToken()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testAuthCode = "1234";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://github.com");

            var expectedRequestUri = new Uri("https://api.github.com/authorizations");

            const string expectedTokenValue = "GITHUB_TOKEN_VALUE";
            string tokenResponseJson = $"{{ \"token\": \"{expectedTokenValue}\" }}";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(tokenResponseJson)
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Post, expectedRequestUri, request =>
            {
                AssertBasicAuth(request, testUserName, testPassword);
                AssertAuthCode(request, testAuthCode);
                return httpResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            AuthenticationResult authResult = await api.AcquireTokenAsync(
                uri, testUserName, testPassword, testAuthCode, testScopes);

            Assert.Equal(GitHubAuthenticationResultType.Success, authResult.Type);
            Assert.NotNull(authResult.Token);
            Assert.Equal(expectedTokenValue, authResult.Token.Password);
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_Valid1FANoAppAuthCode_ReturnsApp2FARequired()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://github.com");

            var expectedRequestUri = new Uri("https://api.github.com/authorizations");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.Add(GitHubConstants.GitHubOptHeader, "app");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Post, expectedRequestUri, request =>
            {
                AssertBasicAuth(request, testUserName, testPassword);
                AssertAuthCode(request, null);
                return httpResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            AuthenticationResult authResult = await api.AcquireTokenAsync(
                uri, testUserName, testPassword, null, testScopes);

            Assert.Equal(GitHubAuthenticationResultType.TwoFactorApp, authResult.Type);
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_Valid1FANoSmsAuthCode_ReturnsSms2FARequired()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://github.com");

            var expectedRequestUri = new Uri("https://api.github.com/authorizations");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.Add(GitHubConstants.GitHubOptHeader, "sms");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Post, expectedRequestUri, request =>
            {
                AssertBasicAuth(request, testUserName, testPassword);
                AssertAuthCode(request, null);
                return httpResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            AuthenticationResult authResult = await api.AcquireTokenAsync(
                uri, testUserName, testPassword, null, testScopes);

            Assert.Equal(GitHubAuthenticationResultType.TwoFactorSms, authResult.Type);
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_ValidOAuthToken_ReturnsOAuthToken()
        {
            const string testUserName = "john.doe";
            const string testOAuthToken = "TestOAuthToken";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://github.com");

            var expectedRequestUri = new Uri("https://api.github.com/authorizations");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("This API can only be accessed with username and password Basic Auth")
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Post, expectedRequestUri, request =>
            {
                AssertBasicAuth(request, testUserName, testOAuthToken);
                AssertAuthCode(request, null);
                return httpResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            AuthenticationResult authResult = await api.AcquireTokenAsync(
                uri, testUserName, testOAuthToken, null, testScopes);

            Assert.Equal(GitHubAuthenticationResultType.Success, authResult.Type);
            Assert.NotNull(authResult.Token);
            Assert.Equal(testOAuthToken, authResult.Token.Password);
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_Unauthorized_ReturnsFailure()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testAuthCode = "1234";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://github.com");

            var expectedRequestUri = new Uri("https://api.github.com/authorizations");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(string.Empty)
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Post, expectedRequestUri, request =>
            {
                AssertBasicAuth(request, testUserName, testPassword);
                AssertAuthCode(request, testAuthCode);
                return httpResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            AuthenticationResult authResult = await api.AcquireTokenAsync(
                uri, testUserName, testPassword, testAuthCode, testScopes);

            Assert.Equal(GitHubAuthenticationResultType.Failure, authResult.Type);
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_Forbidden_ReturnsFailure()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testAuthCode = "1234";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://github.com");

            var expectedRequestUri = new Uri("https://api.github.com/authorizations");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(string.Empty)
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Post, expectedRequestUri, request =>
            {
                AssertBasicAuth(request, testUserName, testPassword);
                AssertAuthCode(request, testAuthCode);
                return httpResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            AuthenticationResult authResult = await api.AcquireTokenAsync(
                uri, testUserName, testPassword, testAuthCode, testScopes);

            Assert.Equal(GitHubAuthenticationResultType.Failure, authResult.Type);
        }

        [Fact]
        public async Task GitHubRestApi_AcquireTokenAsync_UnknownResponse_ReturnsFailure()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testAuthCode = "1234";
            string[] testScopes = { "scope1", "scope2" };

            var context = new TestCommandContext();
            var uri = new Uri("https://github.com");

            var expectedRequestUri = new Uri("https://api.github.com/authorizations");

            // https://tools.ietf.org/html/rfc2324#section-2.3.2
            const HttpStatusCode httpIAmATeaPot = (HttpStatusCode) 418;
            var httpResponse = new HttpResponseMessage(httpIAmATeaPot)
            {
                Content = new StringContent("I am a tea pot (short and stout).")
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Post, expectedRequestUri, request =>
            {
                AssertBasicAuth(request, testUserName, testPassword);
                AssertAuthCode(request, testAuthCode);
                return httpResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new GitHubRestApi(context, httpFactory);

            AuthenticationResult authResult = await api.AcquireTokenAsync(
                uri, testUserName, testPassword, testAuthCode, testScopes);

            Assert.Equal(GitHubAuthenticationResultType.Failure, authResult.Type);
        }

        #region Helpers

        private static void AssertBasicAuth(HttpRequestMessage request, string userName, string password)
        {
            string expectedBasicValue = new GitCredential(userName, password).ToBase64String();

            AuthenticationHeaderValue authHeader = request.Headers.Authorization;
            Assert.NotNull(authHeader);
            Assert.Equal("Basic", authHeader.Scheme);
            Assert.Equal(expectedBasicValue, authHeader.Parameter);
        }

        private void AssertAuthCode(HttpRequestMessage request, string authCode)
        {
            if (authCode is null)
            {
                Assert.False(request.Headers.Contains(GitHubConstants.GitHubOptHeader));
            }
            else
            {
                Assert.True(request.Headers.TryGetValues(GitHubConstants.GitHubOptHeader, out var values));

                string actualAuthCode = values.Single();
                Assert.Equal(authCode, actualAuthCode);
            }
        }

        #endregion
    }
}
