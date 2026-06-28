using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Atlassian.Bitbucket.DataCenter;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Atlassian.Bitbucket.Tests.DataCenter
{
    public class BitbucketRestApiTest
    {
        [Fact]
        public async Task BitbucketRestApi_GetUserInformationAsync_ReturnsUserInfo_ForSuccessfulRequest_DoesNothing()
        {
            var context = new TestCommandContext();

            var expectedRequestUri = new Uri("http://example.com/rest/api/1.0/users");
            var httpHandler = new TestHttpMessageHandler();
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            httpHandler.Setup(HttpMethod.Get, expectedRequestUri, request =>
            {
                return httpResponse;
            });
            context.HttpClientFactory.MessageHandler = httpHandler;

            context.Settings.RemoteUri = new Uri("http://example.com");

            var api = new BitbucketRestApi(context);
            var result = await api.GetUserInformationAsync("never used", "never used", false);

            Assert.NotNull(result);
            Assert.Equal(DataCenterConstants.OAuthUserName, result.Response.UserName);

            httpHandler.AssertRequest(HttpMethod.Get, expectedRequestUri, 1);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized, true)]
        [InlineData(HttpStatusCode.NotFound, false)]
        public async Task BitbucketRestApi_IsOAuthInstalledAsync_ReflectsBitbucketAuthenticationResponse(HttpStatusCode responseCode, bool impliedSupport)
        {
            var context = new TestCommandContext();
            var httpHandler = new TestHttpMessageHandler();

            var expectedRequestUri = new Uri("http://example.com/rest/oauth2/1.0/client");

            var httpResponse = new HttpResponseMessage(responseCode);
            httpHandler.Setup(HttpMethod.Get, expectedRequestUri, request =>
            {
                return httpResponse;
            });

            context.HttpClientFactory.MessageHandler = httpHandler;
            context.Settings.RemoteUri = new Uri("http://example.com");

            var api = new BitbucketRestApi(context);

            var isInstalled = await api.IsOAuthInstalledAsync();

            httpHandler.AssertRequest(HttpMethod.Get, expectedRequestUri, 1);

            Assert.Equal(impliedSupport, isInstalled);
        }

        [Theory]
        [MemberData(nameof(GetAuthenticationMethodsAsyncData))]
        public async Task BitbucketRestApi_GetAuthenticationMethodsAsync_ReflectRestApiResponse(string loginOptionResponseJson, List<AuthenticationMethod> impliedSupportedMethods, List<AuthenticationMethod> impliedUnsupportedMethods)
        {
            var context = new TestCommandContext();
            var httpHandler = new TestHttpMessageHandler();

            var expectedRequestUri = new Uri("http://example.com/rest/authconfig/1.0/login-options");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(loginOptionResponseJson)
            };

            httpHandler.Setup(HttpMethod.Get, expectedRequestUri, request =>
            {
                return httpResponse;
            });

            context.HttpClientFactory.MessageHandler = httpHandler;
            context.Settings.RemoteUri = new Uri("http://example.com");

            var api = new BitbucketRestApi(context);

            var authMethods = await api.GetAuthenticationMethodsAsync();

            httpHandler.AssertRequest(HttpMethod.Get, expectedRequestUri, 1);

            Assert.NotNull(authMethods);
            Assert.Equal(authMethods.Count, impliedSupportedMethods.Count);
            Assert.Contains(authMethods, m => impliedSupportedMethods.Contains(m));
            Assert.DoesNotContain(authMethods, m => impliedUnsupportedMethods.Contains(m));
        }

        public static IEnumerable<object[]> GetAuthenticationMethodsAsyncData =>
        new List<object[]>
        {
            new object[] { $"{{ \"results\":[ {{ \"type\":\"LOGIN_FORM\"}}]}}",
                            new List<AuthenticationMethod>{AuthenticationMethod.BasicAuth},
                            new List<AuthenticationMethod>{AuthenticationMethod.Sso}},
            new object[] { $"{{ \"results\":[{{\"type\":\"IDP\"}}]}}",
                            new List<AuthenticationMethod>{AuthenticationMethod.Sso},
                            new List<AuthenticationMethod>{AuthenticationMethod.BasicAuth}},
            new object[] { $"{{ \"results\":[{{\"type\":\"IDP\"}}, {{ \"type\":\"LOGIN_FORM\"}},  {{ \"type\":\"UNDEFINED\"}}]}}",
                            new List<AuthenticationMethod>{AuthenticationMethod.Sso, AuthenticationMethod.BasicAuth},
                            new List<AuthenticationMethod>()},
        };
    }
}