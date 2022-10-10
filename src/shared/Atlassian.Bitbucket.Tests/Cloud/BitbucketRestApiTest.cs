using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Atlassian.Bitbucket.Cloud;
using GitCredentialManager.Tests;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Atlassian.Bitbucket.Tests.Cloud
{
    public class BitbucketRestApiTest
    {
        [Theory]
        [InlineData("jsquire", "token", true)]
        [InlineData("jsquire", "password", false)]
        public async Task BitbucketRestApi_GetUserInformationAsync_ReturnsUserInfo_ForSuccessfulRequest(string username, string password, bool isBearerToken) 
        {
            var twoFactorAuthenticationEnabled = false;
            var uuid = Guid.NewGuid();
            var accountId = "1234";

            var context = new TestCommandContext();

            var expectedRequestUri = new Uri("https://api.bitbucket.org/2.0/user");

            var userinfoResponseJson = $"{{ \"username\": \"{username}\" , \"has_2fa_enabled\": \"{twoFactorAuthenticationEnabled}\", \"account_id\": \"{accountId}\", \"uuid\": \"{uuid}\"}}";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(userinfoResponseJson)
            };

            var httpHandler = new TestHttpMessageHandler();
            httpHandler.Setup(HttpMethod.Get, expectedRequestUri, request =>
            {
                if (isBearerToken)
                {
                    RestTestUtilities.AssertBearerAuth(request, password);
                }
                else
                {
                    RestTestUtilities.AssertBasicAuth(request, username, password);
                }

                return httpResponse;
            });
            context.HttpClientFactory.MessageHandler = httpHandler;

            var api = new BitbucketRestApi(context);
            var result = await api.GetUserInformationAsync(username, password, isBearerToken);

            Assert.NotNull(result);
            Assert.Equal(username, result.Response.UserName);
            Assert.Equal(twoFactorAuthenticationEnabled, result.Response.IsTwoFactorAuthenticationEnabled);
            Assert.Equal(accountId, ((UserInfo)result.Response).AccountId);
            Assert.Equal(uuid, ((UserInfo)result.Response).Uuid);

            httpHandler.AssertRequest(HttpMethod.Get, expectedRequestUri, 1);
        }
    }
}