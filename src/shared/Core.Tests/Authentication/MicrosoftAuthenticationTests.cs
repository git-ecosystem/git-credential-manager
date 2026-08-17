using System;
using System.IO;
using System.Threading.Tasks;
using GitCredentialManager.Authentication;
using GitCredentialManager.Tests.Objects;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensions.Msal;
using Xunit;

namespace GitCredentialManager.Tests.Authentication
{
    public class MicrosoftAuthenticationTests
    {
        [Fact]
        public async Task MicrosoftAuthentication_GetTokenForUserAsync_NoInteraction_ThrowsException()
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

            var msAuth = new MicrosoftAuthentication(context);

            await Assert.ThrowsAsync<Trace2InvalidOperationException>(
                () => msAuth.GetTokenForUserAsync(authority, clientId, redirectUri, scopes, userName, false));
        }

        [MacOSFact]
        public void MicrosoftAuthentication_CreateUserTokenCacheProps_OnMacOS_UsesGcmKeychain()
        {
            var context = new TestCommandContext();
            var msAuth = new MicrosoftAuthentication(context);

            StorageCreationProperties actual = msAuth.CreateUserTokenCacheProps(useLinuxFallback: false);

            Assert.Equal("user.cache", actual.CacheFileName);
            Assert.Equal(Path.Combine(context.FileSystem.UserDataDirectoryPath, "msal"), actual.CacheDirectory);
            Assert.Equal("GitCredentialManager.MSAL", actual.MacKeyChainServiceName);
            Assert.Equal("UserCache", actual.MacKeyChainAccountName);
        }

        [WindowsFact]
        public void MicrosoftAuthentication_CreateUserTokenCacheProps_OnWindows_UsesSharedCache()
        {
            var context = new TestCommandContext();
            var msAuth = new MicrosoftAuthentication(context);

            StorageCreationProperties actual = msAuth.CreateUserTokenCacheProps(useLinuxFallback: false);

            Assert.Equal("msal.cache", actual.CacheFileName);
            Assert.Equal(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".IdentityService"),
                actual.CacheDirectory);
        }

        [LinuxFact]
        public void MicrosoftAuthentication_CreateUserTokenCacheProps_OnLinux_UsesSharedCache()
        {
            var context = new TestCommandContext();
            var msAuth = new MicrosoftAuthentication(context);

            StorageCreationProperties actual = msAuth.CreateUserTokenCacheProps(useLinuxFallback: false);

            Assert.Equal("msal.cache", actual.CacheFileName);
            Assert.Equal(
                Path.Combine(context.FileSystem.UserHomePath, ".local", ".IdentityService"),
                actual.CacheDirectory);
            Assert.Equal("msal.cache", actual.KeyringSchemaName);
            Assert.Equal("default", actual.KeyringCollection);
            Assert.Equal("MSALCache", actual.KeyringSecretLabel);
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
        public void MicrosoftAuthentication_GetManagedIdentity_ValidSystemId_ReturnsSystemId(string str)
        {
            ManagedIdentityId actual = MicrosoftAuthentication.GetManagedIdentity(str);
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
        public void MicrosoftAuthentication_GetManagedIdentity_ValidUserIdByClientId_ReturnsUserId(string str)
        {
            ManagedIdentityId actual = MicrosoftAuthentication.GetManagedIdentity(str);
            Assert.NotNull(actual);
            Assert.NotEqual(ManagedIdentityId.SystemAssigned, actual);
        }

        [Theory]
        [InlineData("unknown://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
        [InlineData("this is a string")]
        public void MicrosoftAuthentication_GetManagedIdentity_Invalid_ThrowsArgumentException(string str)
        {
            Assert.Throws<ArgumentException>(() => MicrosoftAuthentication.GetManagedIdentity(str));
        }
    }
}
