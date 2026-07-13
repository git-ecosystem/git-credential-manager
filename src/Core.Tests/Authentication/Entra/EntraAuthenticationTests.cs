using System;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.Entra;
using GitCredentialManager.Tests.Objects;
using Microsoft.Identity.Client.AppConfig;
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
            const string userName = null; // No user to ensure we do not use an existing token

            var context = new TestCommandContext
            {
                Settings = {IsInteractionAllowed = false},
            };

            var msAuth = new EntraAuthentication(context);

            await Assert.ThrowsAsync<Trace2InvalidOperationException>(
                () => msAuth.GetTokenForUserAsync(authority, clientId, redirectUri, scopes, userName, false));
        }

        [Fact]
        public async Task EntraAuthentication_GetUserAccountsAsync_NoPublicClientConfig_ThrowsException()
        {
            var entraAuth = new EntraAuthentication(new TestCommandContext());

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => entraAuth.GetUserAccountsAsync());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("system")]
        [InlineData("SYSTEM")]
        [InlineData("sYsTeM")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("id://00000000-0000-0000-0000-000000000000")]
        [InlineData("ID://00000000-0000-0000-0000-000000000000")]
        [InlineData("Id://00000000-0000-0000-0000-000000000000")]
        public void EntraAuthentication_GetManagedIdentity_ValidSystemId_ReturnsSystemId(string str)
        {
            ManagedIdentityId actual = EntraAuthentication.GetManagedIdentity(str);
            Assert.Equal(ManagedIdentityId.SystemAssigned, actual);
        }

        [Theory]
        [InlineData("8B49DCA0-1298-4A0D-AD6D-934E40230839")]
        [InlineData("id://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
        [InlineData("ID://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
        [InlineData("Id://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
        [InlineData("resource://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
        [InlineData("RESOURCE://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
        [InlineData("rEsOuRcE://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
        [InlineData("resource://00000000-0000-0000-0000-000000000000")]
        public void EntraAuthentication_GetManagedIdentity_ValidUserIdByClientId_ReturnsUserId(string str)
        {
            ManagedIdentityId actual = EntraAuthentication.GetManagedIdentity(str);
            Assert.NotNull(actual);
            Assert.NotEqual(ManagedIdentityId.SystemAssigned, actual);
        }

        [Theory]
        [InlineData("unknown://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
        [InlineData("this is a string")]
        public void EntraAuthentication_GetManagedIdentity_Invalid_ThrowsArgumentException(string str)
        {
            Assert.Throws<ArgumentException>(() => EntraAuthentication.GetManagedIdentity(str));
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
