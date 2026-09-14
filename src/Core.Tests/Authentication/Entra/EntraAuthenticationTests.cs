using System;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.Entra;
using GitCredentialManager.Tests.Objects;
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
}
