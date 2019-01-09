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
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class AzureDevOpsApiTests
    {
        private const string ExpectedLocationServicePath = "_apis/ServiceDefinitions/LocationService2/951917AC-A960-4999-8464-E3F0AA25B381?api-version=1.0";
        private const string ExpectedIdentityServicePath = "_apis/token/sessiontokens?api-version=1.0&tokentype=compact";

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_NullUri_ThrowsException()
        {
            var api = new AzureDevOpsRestApi(new TestCommandContext());

            await Assert.ThrowsAsync<ArgumentNullException>(() => api.GetAuthorityAsync(null));
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_NoNetwork_ThrowsException()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            var httpHandler = new TestHttpMessageHandler {SimulateNoNetwork = true};

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            await Assert.ThrowsAsync<HttpRequestException>(() => api.GetAuthorityAsync(uri));
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_NoHeaders_ReturnsCommonAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            const string expectedAuthority = "https://login.microsoftonline.com/common";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_WwwAuthenticateBearer_ReturnsAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            const string expectedAuthority = "https://login.microsoftonline.com/test-authority";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.WwwAuthenticate.ParseAdd($"Bearer authorization_uri={expectedAuthority}");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_WwwAuthenticateMultiple_ReturnsBearerAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            const string expectedAuthority = "https://login.microsoftonline.com/test-authority";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.WwwAuthenticate.ParseAdd("Bearer");
            httpResponse.Headers.WwwAuthenticate.ParseAdd($"Bearer authorization_uri={expectedAuthority}");
            httpResponse.Headers.WwwAuthenticate.ParseAdd("NTLM [test-challenge-string]");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_VssResourceTenantAad_ReturnsAadAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");
            var aadTenantId = Guid.NewGuid();

            string expectedAuthority = $"https://login.microsoftonline.com/{aadTenantId:D}";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers = {{AzureDevOpsConstants.VssResourceTenantHeader, aadTenantId.ToString("D")}}
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_VssResourceTenantMultiple_ReturnsFirstAadAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");
            var aadTenantId1 = Guid.NewGuid();
            var msaTenantId  = Guid.Empty;
            var aadTenantId2 = Guid.NewGuid();

            string expectedAuthority = $"https://login.microsoftonline.com/{aadTenantId1:D}";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers =
                {
                    {AzureDevOpsConstants.VssResourceTenantHeader, aadTenantId1.ToString("D")},
                    {AzureDevOpsConstants.VssResourceTenantHeader, msaTenantId.ToString("D")},
                    {AzureDevOpsConstants.VssResourceTenantHeader, aadTenantId2.ToString("D")},
                }
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_VssResourceTenantMsa_ReturnsMsaAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");
            var msaTenantId = Guid.Empty;

            const string expectedAuthority = "https://login.microsoftonline.com/live.com";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers = {{AzureDevOpsConstants.VssResourceTenantHeader, msaTenantId.ToString("D")}}
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_BothWwwAuthAndVssResourceHeaders_ReturnsWwwAuthAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");
            var aadTenantIdWwwAuth = Guid.NewGuid();
            var aadTenantIdVssRes = Guid.NewGuid();

            string expectedAuthority = $"https://login.microsoftonline.com/{aadTenantIdWwwAuth:D}";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.Add(AzureDevOpsConstants.VssResourceTenantHeader, aadTenantIdVssRes.ToString("D"));
            httpResponse.Headers.WwwAuthenticate.ParseAdd($"Bearer authorization_uri={expectedAuthority}");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_CreatePersonalAccessTokenAsync_ReturnsPAT()
        {
            var context = new TestCommandContext();
            var orgUri = new Uri("https://dev.azure.com/org/");

            const string expectedPat = "PERSONAL-ACCESS-TOKEN";
            const string accessToken = "ACCESS-TOKEN";
            IEnumerable<string> scopes = new[] {AzureDevOpsConstants.PersonalAccessTokenScopes.ReposWrite};

            var identityServiceUri = new Uri("https://identity.example.com/");

            var locSvcRequestUri = new Uri(orgUri, ExpectedLocationServicePath);
            var locSvcResponse = CreateLocationServiceResponse(identityServiceUri);

            var identSvcRequestUri = new Uri(identityServiceUri, ExpectedIdentityServicePath);
            var identSvcResponse = CreateIdentityServiceResponse(expectedPat);

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Get, locSvcRequestUri, x =>
            {
                AssertAcceptJson(x);
                AssertBearerToken(x, accessToken);
                return locSvcResponse;
            });
            httpHandler.Setup(HttpMethod.Post, identSvcRequestUri, x =>
            {
                AssertAcceptJson(x);
                AssertBearerToken(x, accessToken);
                return identSvcResponse;
            });

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            string actualPat = await api.CreatePersonalAccessTokenAsync(orgUri, accessToken, scopes);

            Assert.Equal(expectedPat, actualPat);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_CreatePersonalAccessTokenAsync_LocSvcReturnsHttp500_ThrowsException()
        {
            var context = new TestCommandContext();
            var orgUri = new Uri("https://dev.azure.com/org/");

            const string accessToken = "ACCESS-TOKEN";
            IEnumerable<string> scopes = new[] {AzureDevOpsConstants.PersonalAccessTokenScopes.ReposWrite};

            var locSvcRequestUri = new Uri(orgUri, ExpectedLocationServicePath);

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Get, locSvcRequestUri, HttpStatusCode.InternalServerError);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            await Assert.ThrowsAsync<Exception>(() => api.CreatePersonalAccessTokenAsync(orgUri, accessToken, scopes));
        }

        [Fact]
        public async Task AzureDevOpsRestApi_CreatePersonalAccessTokenAsync_IdentSvcReturnsHttp500_ThrowsException()
        {
            var context = new TestCommandContext();
            var orgUri = new Uri("https://dev.azure.com/org/");

            const string accessToken = "ACCESS-TOKEN";
            IEnumerable<string> scopes = new[] {AzureDevOpsConstants.PersonalAccessTokenScopes.ReposWrite};

            var identityServiceUri = new Uri("https://identity.example.com/");

            var locSvcRequestUri = new Uri(orgUri, ExpectedLocationServicePath);
            var locSvcResponse = CreateLocationServiceResponse(identityServiceUri);

            var identSvcRequestUri = new Uri(identityServiceUri, ExpectedIdentityServicePath);

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Get,  locSvcRequestUri,   x =>
            {
                AssertAcceptJson(x);
                AssertBearerToken(x, accessToken);
                return locSvcResponse;
            });
            httpHandler.Setup(HttpMethod.Post, identSvcRequestUri, HttpStatusCode.InternalServerError);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            await Assert.ThrowsAsync<Exception>(() => api.CreatePersonalAccessTokenAsync(orgUri, accessToken, scopes));
        }

        [Fact]
        public async Task AzureDevOpsRestApi_CreatePersonalAccessTokenAsync_IdentSvcReturnsHttp500WithError_ThrowsExceptionWithErrorMessage()
        {
            const string serverErrorMessage = "ERROR123: This is a test error.";

            var context = new TestCommandContext();
            var orgUri = new Uri("https://dev.azure.com/org/");

            const string accessToken = "ACCESS-TOKEN";
            IEnumerable<string> scopes = new[] {AzureDevOpsConstants.PersonalAccessTokenScopes.ReposWrite};

            var identityServiceUri = new Uri("https://identity.example.com/");

            var locSvcRequestUri = new Uri(orgUri, ExpectedLocationServicePath);
            var locSvcResponse = CreateLocationServiceResponse(identityServiceUri);

            var identSvcRequestUri = new Uri(identityServiceUri, ExpectedIdentityServicePath);
            var identSvcError = CreateIdentityServiceErrorResponse(serverErrorMessage);

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Get,  locSvcRequestUri,   x =>
            {
                AssertAcceptJson(x);
                AssertBearerToken(x, accessToken);
                return locSvcResponse;
            });
            httpHandler.Setup(HttpMethod.Post, identSvcRequestUri, _ => identSvcError);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var api = new AzureDevOpsRestApi(context, httpFactory);

            Exception exception = await Assert.ThrowsAsync<Exception>(
                () => api.CreatePersonalAccessTokenAsync(orgUri, accessToken, scopes));

            Assert.Contains(serverErrorMessage, exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(null, false, null)]
        [InlineData("NotBearer", false, null)]
        [InlineData("Bearer", false, null)]
        [InlineData("Bearer foobar", false, null)]
        [InlineData("Bearer authorization_uri=https://example.com", true, "https://example.com")]
        public void AzureDevOpsRestApi_TryGetAuthorityFromHeader(string headerValue, bool expectedResult, string expectedAuthority)
        {
            var header = headerValue is null ? null : AuthenticationHeaderValue.Parse(headerValue);
            bool actualResult = AzureDevOpsRestApi.TryGetAuthorityFromHeader(header, out string actualAuthority);

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Theory]
        [InlineData(null, "target", false, null)]
        [InlineData("{}", "target", false, null)]
        [InlineData("{\"foo\": \"123\"}",
                    "target", false, null)]
        [InlineData("{\"target\": \"42\"}",
                    "target", true, "42")]
        [InlineData("{\"TARGET\": \"42\"}",
                    "target", true, "42")]
        [InlineData("{\"target\": \"42\", \"other\": { \"target\": \"123\" } }",
                    "target", true, "42")]
        [InlineData("{\"foo\": { \"bar\": {\"target\":\"42\"} } }",
                    "target", true, "42")]
        public void AzureDevOpsRestApi_TryGetFirstJsonStringField(
            string json, string fieldName, bool expectedResult, string expectedValue)
        {
            bool actualResult = AzureDevOpsRestApi.TryGetFirstJsonStringField(json, fieldName, out string actualValue);

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedValue, actualValue);
        }

        #region Helpers

        private static void AssertHeader(HttpRequestMessage request, KeyValuePair<string, IEnumerable<string>> header)
        {
            AssertHeader(request, header.Key, header.Value);
        }

        private static void AssertHeader(HttpRequestMessage request, string headerName, IEnumerable<string> headerValues)
        {
            Assert.True(request.Headers.Contains(headerName));
            Assert.Equal(headerValues, request.Headers.GetValues(headerName));
        }

        private static void AssertAcceptJson(HttpRequestMessage request)
        {
            IEnumerable<string> acceptMimeTypes = request.Headers.Accept.Select(x => x.MediaType);
            Assert.Contains(Constants.Http.MimeTypeJson, acceptMimeTypes);
        }

        private static void AssertBearerToken(HttpRequestMessage request, string bearerToken)
        {
            AuthenticationHeaderValue authHeader = request.Headers.Authorization;
            Assert.NotNull(authHeader);
            Assert.Equal("Bearer", authHeader.Scheme);
            Assert.Equal(bearerToken, authHeader.Parameter);
        }

        private static HttpResponseMessage CreateLocationServiceResponse(Uri identityServiceUri)
        {
            var json = JsonConvert.SerializeObject(
                new Dictionary<string, object>{["location"] = identityServiceUri.AbsoluteUri}
            );

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
        }

        private static HttpResponseMessage CreateIdentityServiceResponse(string pat)
        {
            var json = JsonConvert.SerializeObject(
                new Dictionary<string, object> {["token"] = pat}
            );

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
        }

        private static HttpResponseMessage CreateIdentityServiceErrorResponse(string errorMessage)
        {
            var json = JsonConvert.SerializeObject(
                new Dictionary<string, object> {["message"] = errorMessage}
            );

            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(json)
            };
        }

        #endregion
    }
}
