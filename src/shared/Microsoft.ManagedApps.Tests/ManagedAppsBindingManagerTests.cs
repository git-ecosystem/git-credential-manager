using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.ManagedApps.Tests
{
    public class ManagedAppsBindingManagerTests
    {
        private const string Host = "https://4899945d6e51f1f0326cda880ec7a7.09.environment.api.powerplatform.com";

        [Fact]
        public void ManagedAppsBindingManager_GetAccount_NoBinding_ReturnsNull()
        {
            var manager = new ManagedAppsBindingManager(new NullTrace(), new TestGit());

            string account = manager.GetAccount(Host);

            Assert.Null(account);
        }

        [Fact]
        public void ManagedAppsBindingManager_SignIn_ThenGetAccount_ReturnsBoundAccount()
        {
            var git = new TestGit();
            var manager = new ManagedAppsBindingManager(new NullTrace(), git);

            manager.SignIn(Host, "user@example.com");

            Assert.Equal("user@example.com", manager.GetAccount(Host));
        }

        [Fact]
        public void ManagedAppsBindingManager_SignIn_NullAccount_DoesNotThrowAndDoesNotBind()
        {
            var manager = new ManagedAppsBindingManager(new NullTrace(), new TestGit());

            manager.SignIn(Host, null);

            Assert.Null(manager.GetAccount(Host));
        }

        [Fact]
        public void ManagedAppsBindingManager_SignOut_RemovesBinding()
        {
            var git = new TestGit();
            var manager = new ManagedAppsBindingManager(new NullTrace(), git);
            manager.SignIn(Host, "user@example.com");

            manager.SignOut(Host);

            Assert.Null(manager.GetAccount(Host));
        }

        [Fact]
        public void ManagedAppsBindingManager_SignOut_NoExistingBinding_DoesNotThrow()
        {
            var manager = new ManagedAppsBindingManager(new NullTrace(), new TestGit());

            manager.SignOut(Host);

            Assert.Null(manager.GetAccount(Host));
        }

        [Fact]
        public void ManagedAppsBindingManager_Bindings_AreIndependentPerHost()
        {
            const string otherHost = "https://c0a12cc2ff79f7306d6ef9fc69c2062.1.environment.api.preprod.powerplatform.com";
            var git = new TestGit();
            var manager = new ManagedAppsBindingManager(new NullTrace(), git);

            manager.SignIn(Host, "user1@example.com");
            manager.SignIn(otherHost, "user2@example.com");

            Assert.Equal("user1@example.com", manager.GetAccount(Host));
            Assert.Equal("user2@example.com", manager.GetAccount(otherHost));
        }
    }
}
