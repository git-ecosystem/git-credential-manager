using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.Entra;
using GitCredentialManager.Tests.Objects;
using Microsoft.Identity.Client;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra;

public class EntraAuthenticationTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "true", true)]
    [InlineData(null, "false", false)]
    [InlineData(null, "FALSE", false)]
    [InlineData(null, "0", false)]
    [InlineData(null, "no", false)]
    [InlineData(null, "off", false)]
    [InlineData(null, "invalid", true)]
    [InlineData(null, "", true)]
    [InlineData(null, " ", true)]
    [InlineData("true", null, true)]
    [InlineData("false", null, false)]
    [InlineData("FALSE", null, false)]
    [InlineData("0", null, false)]
    [InlineData("no", null, false)]
    [InlineData("off", null, false)]
    [InlineData("invalid", null, true)]
    [InlineData("", null, true)]
    [InlineData(" ", null, true)]
    [InlineData("false", "true", false)]
    [InlineData("true", "false", true)]
    [InlineData("invalid", "false", true)]
    [InlineData("", "false", true)]
    public void IsBrokerEnabled(string environmentValue, string gitConfigValue, bool expected)
    {
        var context = new TestCommandContext();
        if (gitConfigValue is not null)
        {
            string key =
                $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.MsAuthUseBroker}";
            context.Git.Configuration.Global[key] = [gitConfigValue];
        }
        if (environmentValue is not null)
        {
            context.Environment.Variables[Constants.EnvironmentVariables.MsAuthUseBroker] = environmentValue;
        }
        var entraAuth = new EntraAuthentication(context);

        Assert.Equal(expected, entraAuth.IsBrokerEnabled());
    }

    [Fact]
    public async Task GetTokenForUserAsync_NoInteraction_ThrowsException()
    {
        const string authority = "https://login.microsoftonline.com/common";
        const string clientId = "C9E8FDA6-1D46-484C-917C-3DBD518F27C3";
        string[] scopes = ["user.read"];

        var context = new TestCommandContext
        {
            Settings = { IsInteractionAllowed = false },
        };
        var config = new PublicClientConfig
        {
            ClientId = clientId,
        };
        var entraAuth = new EntraAuthentication(context, config);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => entraAuth.GetTokenForUserAsync(scopes, authority));
    }

    [Fact]
    public async Task GetUserAccountsAsync_NoPublicClientConfig_ThrowsException()
    {
        var entraAuth = new EntraAuthentication(new TestCommandContext());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => entraAuth.GetUserAccountsAsync());
    }

    [WindowsFact]
    public async Task GetTokenUsingWorkloadFederationAsync_WithCachedToken_ReturnsTokenWithoutAccount()
    {
        const string tenantId = "11111111-1111-1111-1111-111111111111";
        const string token = "test-access-token";
        string[] scopes = ["https://resource.invalid/.default"];
        var options = new WorkloadFederationOptions
        {
            Scenario = WorkloadFederationScenario.Generic,
            TenantId = tenantId,
            ClientId = "22222222-2222-2222-2222-222222222222",
            GenericClientAssertion = "test-client-assertion",
        };
        var tokenEndpoint = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token");
        using var handler = new TestHttpMessageHandler { ThrowOnUnexpectedRequest = true };
        handler.Setup(HttpMethod.Get, new Uri("https://login.microsoftonline.com/common/discovery/instance"),
            HttpStatusCode.OK, $$"""
            {
              "tenant_discovery_endpoint": "https://login.microsoftonline.com/{{tenantId}}/v2.0/.well-known/openid-configuration",
              "metadata": [{
                "preferred_network": "login.microsoftonline.com",
                "preferred_cache": "login.windows.net",
                "aliases": ["login.microsoftonline.com", "login.windows.net"]
              }]
            }
            """);
        handler.Setup(HttpMethod.Post, tokenEndpoint, HttpStatusCode.OK, $$"""
            { "token_type": "Bearer", "expires_in": 3600, "access_token": "{{token}}" }
            """);

        DirectoryInfo cacheDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var context = new TestCommandContext
            {
                FileSystem = { UserDataDirectoryPath = cacheDirectory.FullName },
                HttpClientFactory = { MessageHandler = handler },
            };
            var cold = await new EntraAuthentication(context).GetTokenUsingWorkloadFederationAsync(scopes, options);
            Assert.Equal(token, cold.AccessToken);
            Assert.Null(cold.Account);
            handler.AssertRequest(HttpMethod.Post, tokenEndpoint, 1);

            handler.Setup(HttpMethod.Post, tokenEndpoint, HttpStatusCode.Unauthorized, """
                { "error": "invalid_client", "error_description": "Test exchange rejected" }
                """);
            var warm = await new EntraAuthentication(context).GetTokenUsingWorkloadFederationAsync(scopes, options);
            Assert.Equal(token, warm.AccessToken);
            Assert.Null(warm.Account);
            handler.AssertRequest(HttpMethod.Post, tokenEndpoint, 1);

            var denied = await Assert.ThrowsAsync<MsalServiceException>(() =>
                new EntraAuthentication(context).GetTokenUsingWorkloadFederationAsync(
                    ["https://other-resource.invalid/.default"], options));
            Assert.Equal("invalid_client", denied.ErrorCode);
            handler.AssertRequest(HttpMethod.Post, tokenEndpoint, 2);
        }
        finally
        {
            cacheDirectory.Delete(recursive: true);
        }
    }
}
