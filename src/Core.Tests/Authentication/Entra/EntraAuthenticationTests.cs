using System;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.Entra;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra;

public class EntraAuthenticationTests
{
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

        await Assert.ThrowsAsync<Trace2InvalidOperationException>(
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
