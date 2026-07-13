using System;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.Entra;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra
{
    public class EntraAuthenticationTests
    {
        [Fact]
        public async Task EntraAuthentication_GetTokenForUserAsync_NoInteraction_ThrowsException()
        {
            const string authority = "https://login.microsoftonline.com/common";
            const string clientId = "C9E8FDA6-1D46-484C-917C-3DBD518F27C3";
            Uri redirectUri = new Uri("https://localhost");
            string[] scopes = {"user.read"};
            const IEntraAccount account = null; // No account to ensure we do not use an existing token

            var context = new TestCommandContext
            {
                Settings = {IsInteractionAllowed = false},
            };

            var msAuth = new EntraAuthentication(context);

            await Assert.ThrowsAsync<Trace2InvalidOperationException>(
                () => msAuth.GetTokenForUserAsync(authority, clientId, redirectUri, scopes, account, false));
        }

        [Fact]
        public async Task EntraAuthentication_GetUserAccountsAsync_NoPublicClientConfig_ThrowsException()
        {
            var entraAuth = new EntraAuthentication(new TestCommandContext());

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => entraAuth.GetUserAccountsAsync());
        }

        [Fact]
        public void EntraAuthentication_GetFlowType_UnknownValue_WarnsWithConfiguredValue()
        {
            const string configuredValue = "unknown-flow";
            var context = new TestCommandContext();
            context.Environment.Variables[Constants.EnvironmentVariables.MsAuthFlow] = configuredValue;
            var authentication = new EntraAuthentication(context);

            MicrosoftAuthenticationFlowType result = authentication.GetFlowType();

            Assert.Equal(MicrosoftAuthenticationFlowType.Auto, result);
            Assert.Contains(context.Console.WrittenMessages, x => x.Contains(configuredValue));
        }
    }
}
