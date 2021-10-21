using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitCredentialManager;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class AzureReposBindingManagerTests
    {
        #region Bind

        [Fact]
        public void AzureReposBindingManager_Bind_NullOrganization_ThrowException()
        {
            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            Assert.Throws<ArgumentNullException>(() => manager.Bind(null, "user", false));
        }

        [Fact]
        public void AzureReposBindingManager_Bind_NoUser_SetsOrgKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";

            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            manager.Bind(orgName, expectedUser, false);

            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var users));
            Assert.Single(users);
            string actualUser = users[0];
            Assert.Equal(expectedUser, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_BindLocal_NoUser_SetsOrgKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";

            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            manager.Bind(orgName, expectedUser, true);

            Assert.True(git.Configuration.Local.TryGetValue(CreateKey(orgName), out var users));
            Assert.Single(users);
            string actualUser = users[0];
            Assert.Equal(expectedUser, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_Bind_ExistingUser_SetsOrgKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";

            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new[] {"org-user"};

            manager.Bind(orgName, expectedUser, false);

            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var users));
            Assert.Single(users);
            string actualUser = users[0];
            Assert.Equal(expectedUser, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_BindLocal_ExistingUser_SetsOrgKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";

            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Local[CreateKey(orgName)] = new[] {"org-user"};

            manager.Bind(orgName, expectedUser, true);

            Assert.True(git.Configuration.Local.TryGetValue(CreateKey(orgName), out var users));
            Assert.Single(users);
            string actualUser = users[0];
            Assert.Equal(expectedUser, actualUser);
        }

        #endregion

        #region Unbind

        [Fact]
        public void AzureReposBindingManager_Unbind_NullOrganization_ThrowException()
        {
            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            Assert.Throws<ArgumentNullException>(() => manager.Unbind(null, false));
        }

        [Fact]
        public void AzureReposBindingManager_Unbind_NoUser_DoesNothing()
        {
            const string orgName = "org";

            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            manager.Unbind(orgName, false);

            Assert.Empty(git.Configuration.Global);
        }

        [Fact]
        public void AzureReposBindingManager_UnbindLocal_NoUser_DoesNothing()
        {
            const string orgName = "org";

            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            manager.Unbind(orgName, true);

            Assert.Empty(git.Configuration.Local);
        }

        [Fact]
        public void AzureReposBindingManager_Unbind_ExistingUser_RemovesKey()
        {
            const string orgName = "org";

            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new[] {"org-user"};

            manager.Unbind(orgName, false);

            Assert.Empty(git.Configuration.Global);
        }

        [Fact]
        public void AzureReposBindingManager_UnbindLocal_ExistingUser_RemovesKey()
        {
            const string orgName = "org";

            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Local[CreateKey(orgName)] = new[] {"org-user"};

            manager.Unbind(orgName, true);

            Assert.Empty(git.Configuration.Local);
        }

        #endregion

        #region GetBinding

        [Fact]
        public void AzureReposBindingManager_GetBinding_Null_ThrowException()
        {
            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            Assert.Throws<ArgumentNullException>(() => manager.GetBinding(null));
        }

        [Fact]
        public void AzureReposBindingManager_GetBinding_NoUser_ReturnsNull()
        {
            const string orgName = "org";

            var trace = new NullTrace();
            var git = new TestGit();
            var manager = new AzureReposBindingManager(trace, git);

            AzureReposBinding binding = manager.GetBinding(orgName);

            Assert.Null(binding);
        }

        [Fact]
        public void AzureReposBindingManager_GetBinding_GlobalUser_ReturnsBinding()
        {
            const string orgUser = "john.doe";
            const string orgName = "org";
            string orgKey = CreateKey(orgName);

            var git = new TestGit
            {
                Configuration =
                {
                    Global =
                    {
                        [orgKey] = new[] {orgUser}
                    }
                }
            };

            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            AzureReposBinding binding = manager.GetBinding(orgName);

            Assert.Equal(orgName, binding.Organization);
            Assert.Equal(orgUser, binding.GlobalUserName);
            Assert.Null(binding.LocalUserName);
        }

        [Fact]
        public void AzureReposBindingManager_GetBinding_LocalUser_ReturnsBinding()
        {
            const string orgUser = "john.doe";
            const string orgName = "org";
            string orgKey = CreateKey(orgName);

            var git = new TestGit
            {
                Configuration =
                {
                    Local =
                    {
                        [orgKey] = new[] {orgUser}
                    }
                }
            };

            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            AzureReposBinding binding = manager.GetBinding(orgName);

            Assert.Equal(orgName, binding.Organization);
            Assert.Null(binding.GlobalUserName);
            Assert.Equal(orgUser, binding.LocalUserName);
        }

        [Fact]
        public void AzureReposBindingManager_GetBinding_LocalAndGlobalUsers_ReturnsBinding()
        {
            const string orgUserLocal = "john.doe";
            const string orgUserGlobal = "jane.doe";
            const string orgName = "org";
            string orgKey = CreateKey(orgName);

            var git = new TestGit
            {
                Configuration =
                {
                    Global = { [orgKey] = new[] {orgUserGlobal} },
                    Local  = { [orgKey] = new[] {orgUserLocal} }
                }
            };

            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            AzureReposBinding binding = manager.GetBinding(orgName);

            Assert.Equal(orgName, binding.Organization);
            Assert.Equal(orgUserGlobal, binding.GlobalUserName);
            Assert.Equal(orgUserLocal, binding.LocalUserName);
        }

        #endregion

        #region GetBindings

        [Fact]
        public void AzureReposBindingManager_GetBindings_NoUsers_ReturnsEmpty()
        {
            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            IList<AzureReposBinding> actual = manager.GetBindings().ToList();

            Assert.Empty(actual);
        }

        [Fact]
        public void AzureReposBindingManager_GetBindings_Users_ReturnsUsers()
        {
            const string org1 = "org1";
            const string org2 = "org2";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(org1)] = new[] {"user1"};
            git.Configuration.Global[CreateKey(org2)] = new[] {"user2"};

            AzureReposBinding[] bindings = manager.GetBindings().ToArray();

            static void AssertBinding(
                string expectedOrg, string expectedGlobalUser, string expectedLocalUser, AzureReposBinding binding)
            {
                Assert.Equal(expectedOrg, binding.Organization);
                Assert.Equal(expectedGlobalUser, binding.GlobalUserName);
                Assert.Equal(expectedLocalUser, binding.LocalUserName);
            }

            Assert.Equal(2, bindings.Length);
            AssertBinding(org1, "user1", null, bindings[0]);
            AssertBinding(org2, "user2", null, bindings[1]);
        }

        #endregion

        #region GetUser

        [Fact]
        public void AzureReposBindingManager_GetUser_NullOrg_ThrowsException()
        {
            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            Assert.Throws<ArgumentNullException>(() => manager.GetUser(null));
        }

        [Fact]
        public void AzureReposBindingManager_GetUser_NoUser_ReturnsNull()
        {
            const string orgName = "org";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            string actualUser = manager.GetUser(orgName);

            Assert.Null(actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_GetUser_GlobalUser_ReturnsGlobalUser()
        {
            const string orgName = "org";
            const string user1 = "user1";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new[] {user1};

            string actualUser = manager.GetUser(orgName);

            Assert.Equal(user1, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_GetUser_LocalUser_ReturnsLocalUser()
        {
            const string orgName = "org";
            const string user1 = "user1";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Local[CreateKey(orgName)] = new[] {user1};

            string actualUser = manager.GetUser(orgName);

            Assert.Equal(user1, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_GetUser_GlobalAndLocalUsers_ReturnsLocalUser()
        {
            const string orgName = "org";
            const string user1 = "user1";
            const string user2 = "user2";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new[] {user1};
            git.Configuration.Local[CreateKey(orgName)] = new[] {user2};

            string actualUser = manager.GetUser(orgName);

            Assert.Equal(user2, actualUser);
        }

        #endregion

        #region SignIn

        [Fact]
        public void AzureReposBindingManager_SignIn_NullOrg_ThrowsException()
        {
            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            Assert.Throws<ArgumentNullException>(() => manager.SignIn(null, "user1"));
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_NullUser_ThrowsException()
        {
            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            Assert.Throws<ArgumentNullException>(() => manager.SignIn("org", null));
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_NoGlobalNoLocal_BindsGlobal()
        {
            const string orgName = "org";
            const string user1 = "user1";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            manager.SignIn(orgName, user1);

            Assert.Empty(git.Configuration.Local);
            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            string actualUser = globalUsers[0];
            Assert.Equal(user1, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_NoGlobalSameLocal_BindsGlobalUnbindLocal()
        {
            const string orgName = "org";
            const string user1 = "user1";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Local[CreateKey(orgName)] = new []{user1};

            manager.SignIn(orgName, user1);

            Assert.Empty(git.Configuration.Local);
            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            string actualUser = globalUsers[0];
            Assert.Equal(user1, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_NoGlobalOtherLocal_BindsGlobalUnbindLocal()
        {
            const string orgName = "org";
            const string user1 = "user1";
            const string user2 = "user2";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Local[CreateKey(orgName)] = new []{user2};

            manager.SignIn(orgName, user1);

            Assert.Empty(git.Configuration.Local);
            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            string actualUser = globalUsers[0];
            Assert.Equal(user1, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_SameGlobalNoLocal_DoesNothing()
        {
            const string orgName = "org";
            const string user1 = "user1";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new []{user1};

            manager.SignIn(orgName, user1);

            Assert.Empty(git.Configuration.Local);
            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            string actualUser = globalUsers[0];
            Assert.Equal(user1, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_SameGlobalSameLocal_UnbindLocal()
        {
            const string orgName = "org";
            const string user1 = "user1";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new []{user1};
            git.Configuration.Local[CreateKey(orgName)] = new []{user1};

            manager.SignIn(orgName, user1);

            Assert.Empty(git.Configuration.Local);
            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            string actualUser = globalUsers[0];
            Assert.Equal(user1, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_SameGlobalOtherLocal_UnbindLocal()
        {
            const string orgName = "org";
            const string user1 = "user1";
            const string user2 = "user2";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new []{user1};
            git.Configuration.Local[CreateKey(orgName)] = new []{user2};

            manager.SignIn(orgName, user1);

            Assert.Empty(git.Configuration.Local);
            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            string actualUser = globalUsers[0];
            Assert.Equal(user1, actualUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_OtherGlobalNoLocal_BindsLocal()
        {
            const string orgName = "org";
            const string user1 = "user1";
            const string user2 = "user2";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new []{user2};

            manager.SignIn(orgName, user1);

            Assert.True(git.Configuration.Local.TryGetValue(CreateKey(orgName), out var localUsers));
            string actualLocalUser = localUsers[0];
            Assert.Equal(user1, actualLocalUser);

            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            string actualGlobalUser = globalUsers[0];
            Assert.Equal(user2, actualGlobalUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_OtherGlobalSameLocal_DoesNothing()
        {
            const string orgName = "org";
            const string user1 = "user1";
            const string user2 = "user2";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new []{user2};
            git.Configuration.Local[CreateKey(orgName)] = new []{user1};

            manager.SignIn(orgName, user1);

            Assert.True(git.Configuration.Local.TryGetValue(CreateKey(orgName), out var localUsers));
            string actualLocalUser = localUsers[0];
            Assert.Equal(user1, actualLocalUser);

            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            string actualGlobalUser = globalUsers[0];
            Assert.Equal(user2, actualGlobalUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignIn_OtherGlobalOtherLocal_BindsLocal()
        {
            const string orgName = "org";
            const string user1 = "user1";
            const string user2 = "user2";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new []{user2};
            git.Configuration.Local[CreateKey(orgName)] = new []{user2};

            manager.SignIn(orgName, user1);

            Assert.True(git.Configuration.Local.TryGetValue(CreateKey(orgName), out var localUsers));
            string actualLocalUser = localUsers[0];
            Assert.Equal(user1, actualLocalUser);

            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            string actualGlobalUser = globalUsers[0];
            Assert.Equal(user2, actualGlobalUser);
        }

        #endregion

        #region SignOut

        [Fact]
        public void AzureReposBindingManager_SignOut_NoGlobalNoLocal_DoesNothing()
        {
            const string orgName = "org";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            manager.SignOut(orgName);

            Assert.Empty(git.Configuration.Local);
            Assert.Empty(git.Configuration.Global);
        }

        [Fact]
        public void AzureReposBindingManager_SignOut_NoGlobalUserLocal_UnbindsLocal()
        {
            const string orgName = "org";
            const string user1 = "user1";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Local[CreateKey(orgName)] = new[] { user1 };

            manager.SignOut(orgName);

            Assert.Empty(git.Configuration.Local);
            Assert.Empty(git.Configuration.Global);
        }

        [Fact]
        public void AzureReposBindingManager_SignOut_NoGlobalNoInheritLocal_UnbindsLocal()
        {
            const string orgName = "org";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Local[CreateKey(orgName)] = new[] { AzureReposBinding.NoInherit };

            manager.SignOut(orgName);

            Assert.Empty(git.Configuration.Global);
            Assert.Empty(git.Configuration.Local);
        }

        [Fact]
        public void AzureReposBindingManager_SignOut_UserGlobalNoLocal_BindLocalNoInherit()
        {
            const string orgName = "org";
            const string user1 = "user1";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new[] { user1 };

            manager.SignOut(orgName);

            Assert.True(git.Configuration.Local.TryGetValue(CreateKey(orgName), out var localUsers));
            Assert.Single(localUsers);
            string actualLocalUser = localUsers[0];
            Assert.Equal(AzureReposBinding.NoInherit, actualLocalUser);

            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            Assert.Single(globalUsers);
            string actualGlobalUser = globalUsers[0];
            Assert.Equal(user1, actualGlobalUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignOut_UserGlobalNoInheritLocal_DoesNothing()
        {
            const string orgName = "org";
            const string user1 = "user1";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new[] { user1 };
            git.Configuration.Local[CreateKey(orgName)] = new[] { AzureReposBinding.NoInherit };

            manager.SignOut(orgName);

            Assert.True(git.Configuration.Local.TryGetValue(CreateKey(orgName), out var localUsers));
            Assert.Single(localUsers);
            string actualLocalUser = localUsers[0];
            Assert.Equal(AzureReposBinding.NoInherit, actualLocalUser);

            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            Assert.Single(globalUsers);
            string actualGlobalUser = globalUsers[0];
            Assert.Equal(user1, actualGlobalUser);
        }

        [Fact]
        public void AzureReposBindingManager_SignOut_UserGlobalUserLocal_BindLocalNoInherit()
        {
            const string orgName = "org";
            const string user1 = "user1";
            const string user2 = "user2";

            var git = new TestGit();
            var trace = new NullTrace();
            var manager = new AzureReposBindingManager(trace, git);

            git.Configuration.Global[CreateKey(orgName)] = new[] { user1 };
            git.Configuration.Local[CreateKey(orgName)] = new[] { user2 };

            manager.SignOut(orgName);

            Assert.True(git.Configuration.Local.TryGetValue(CreateKey(orgName), out var localUsers));
            Assert.Single(localUsers);
            string actualLocalUser = localUsers[0];
            Assert.Equal(AzureReposBinding.NoInherit, actualLocalUser);

            Assert.True(git.Configuration.Global.TryGetValue(CreateKey(orgName), out var globalUsers));
            Assert.Single(globalUsers);
            string actualGlobalUser = globalUsers[0];
            Assert.Equal(user1, actualGlobalUser);
        }


        #endregion

        #region Helpers

        private static string CreateKey(string orgName)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}:{2}/{3}.{4}",
                Constants.GitConfiguration.Credential.SectionName,
                AzureDevOpsConstants.UrnScheme, AzureDevOpsConstants.UrnOrgPrefix, orgName,
                Constants.GitConfiguration.Credential.UserName);
        }

        #endregion
    }
}
