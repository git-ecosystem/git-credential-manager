// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class HostProviderTests
    {
        #region GetCredentialAsync

        [Fact]
        public async Task HostProvider_GetCredentialAsync_CredentialExists_ReturnsExistingCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "git:test-cred-key";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["username"] = testUserName,
                ["password"] = testPassword,
            });

            var context = new TestCommandContext
            {
                CredentialStore = {[testCredentialKey] = new GitCredential(testUserName, testPassword)}
            };
            var provider = new TestHostProvider(context)
            {
                IsSupportedFunc = _ => true,
                CredentialKey = testCredentialKey,
                GenerateCredentialFunc = _ =>
                {
                    Assert.True(false, "Should never be called");
                    return null;
                },
            };

            ICredential actualCredential = await ((IHostProvider) provider).GetCredentialAsync(input);

            Assert.Equal(testUserName, actualCredential.UserName);
            Assert.Equal(testPassword, actualCredential.Password);
        }

        [Fact]
        public async Task HostProvider_GetCredentialAsync_CredentialDoesNotExist_ReturnsNewGeneratedCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "git:test-cred-key";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["username"] = testUserName,
                ["password"] = testPassword,
            });

            bool generateWasCalled = false;
            var context = new TestCommandContext();
            var provider = new TestHostProvider(context)
            {
                IsSupportedFunc = _ => true,
                CredentialKey = testCredentialKey,
                GenerateCredentialFunc = _ =>
                {
                    generateWasCalled = true;
                    return new GitCredential(testUserName, testPassword);
                },
            };

            ICredential actualCredential = await ((IHostProvider) provider).GetCredentialAsync(input);

            Assert.True(generateWasCalled);
            Assert.Equal(testUserName, actualCredential.UserName);
            Assert.Equal(testPassword, actualCredential.Password);
        }


            #endregion

        #region StoreCredentialAsync

        [Fact]
        public async Task HostProvider_StoreCredentialAsync_EmptyCredential_DoesNotStoreCredential()
        {
            const string emptyUserName = "";
            const string emptyPassword = "";
            const string testCredentialKey = "git:test-cred-key";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["username"] = emptyUserName,
                ["password"] = emptyPassword,
            });

            var context = new TestCommandContext();
            var provider = new TestHostProvider(context) {CredentialKey = testCredentialKey};

            await ((IHostProvider) provider).StoreCredentialAsync(input);

            Assert.Empty(context.CredentialStore);
        }

        [Fact]
        public async Task HostProvider_StoreCredentialAsync_NonEmptyCredential_StoresCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "git:test-cred-key";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["username"] = testUserName,
                ["password"] = testPassword,
            });

            var context = new TestCommandContext();
            var provider = new TestHostProvider(context) {CredentialKey = testCredentialKey};

            await ((IHostProvider) provider).StoreCredentialAsync(input);

            Assert.Single(context.CredentialStore);
            Assert.True(context.CredentialStore.TryGetValue(testCredentialKey, out ICredential storedCredential));
            Assert.Equal(testUserName, storedCredential.UserName);
            Assert.Equal(testPassword, storedCredential.Password);
        }

        [Fact]
        public async Task HostProvider_StoreCredentialAsync_NonEmptyCredential_ExistingCredential_UpdatesCredential()
        {
            const string testUserName = "john.doe";
            const string testPasswordOld = "letmein123-old";
            const string testPasswordNew = "letmein123-new";
            const string testCredentialKey = "git:test-cred-key";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["username"] = testUserName,
                ["password"] = testPasswordNew,
            });

            var context = new TestCommandContext
            {
                CredentialStore = {[testCredentialKey] = new GitCredential(testUserName, testPasswordOld)}
            };
            var provider = new TestHostProvider(context) {CredentialKey = testCredentialKey};

            await ((IHostProvider) provider).StoreCredentialAsync(input);

            Assert.Single(context.CredentialStore);
            Assert.True(context.CredentialStore.TryGetValue(testCredentialKey, out ICredential storedCredential));
            Assert.Equal(testUserName, storedCredential.UserName);
            Assert.Equal(testPasswordNew, storedCredential.Password);
        }

        #endregion

        #region EraseCredentialAsync

        [Fact]
        public async Task HostProvider_EraseCredentialAsync_NoInputUserPass_CredentialExists_ErasesCredential()
        {
            const string testCredentialKey = "git:test-cred-key";
            const string otherCredentialKey1 = "git:credential1";
            const string otherCredentialKey2 = "git:credential2";
            var input = new InputArguments(new Dictionary<string, string>());

            var context = new TestCommandContext
            {
                CredentialStore =
                {
                    [testCredentialKey] = new GitCredential("john.doe", "letmein123"),
                    [otherCredentialKey1] = new GitCredential("this.should-1", "not.be.erased-1"),
                    [otherCredentialKey2] = new GitCredential("this.should-2", "not.be.erased-2")
                }
            };
            var provider = new TestHostProvider(context) {CredentialKey = testCredentialKey};

            await ((IHostProvider) provider).EraseCredentialAsync(input);

            Assert.Equal(2, context.CredentialStore.Count);
            Assert.False(context.CredentialStore.ContainsKey(testCredentialKey));
            Assert.True(context.CredentialStore.ContainsKey(otherCredentialKey1));
            Assert.True(context.CredentialStore.ContainsKey(otherCredentialKey2));
        }

        [Fact]
        public async Task HostProvider_EraseCredentialAsync_InputUserPass_CredentialExists_UserNotMatch_DoesNothing()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "git:test-cred-key";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["username"] = testUserName,
                ["password"] = testPassword,
            });

            var context = new TestCommandContext
            {
                CredentialStore = {[testCredentialKey] = new GitCredential("different-username", testPassword)}
            };
            var provider = new TestHostProvider(context) {CredentialKey = testCredentialKey};

            await ((IHostProvider) provider).EraseCredentialAsync(input);

            Assert.Single(context.CredentialStore);
            Assert.True(context.CredentialStore.ContainsKey(testCredentialKey));
        }

        [Fact]
        public async Task HostProvider_EraseCredentialAsync_InputUserPass_CredentialExists_PassNotMatch_DoesNothing()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "git:test-cred-key";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["username"] = testUserName,
                ["password"] = testPassword,
            });

            var context = new TestCommandContext
            {
                CredentialStore =
                {
                    [testCredentialKey] = new GitCredential(testUserName, "different-password"),
                }
            };
            var provider = new TestHostProvider(context) {CredentialKey = testCredentialKey};

            await ((IHostProvider) provider).EraseCredentialAsync(input);

            Assert.Single(context.CredentialStore);
            Assert.True(context.CredentialStore.ContainsKey(testCredentialKey));
        }

        [Fact]
        public async Task HostProvider_EraseCredentialAsync_InputUserPass_CredentialExists_UserPassMatch_ErasesCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "git:test-cred-key";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["username"] = testUserName,
                ["password"] = testPassword,
            });

            var context = new TestCommandContext
            {
                CredentialStore = {[testCredentialKey] = new GitCredential(testUserName, testPassword)}
            };
            var provider = new TestHostProvider(context) {CredentialKey = testCredentialKey};

            await ((IHostProvider) provider).EraseCredentialAsync(input);

            Assert.Empty(context.CredentialStore);
            Assert.False(context.CredentialStore.ContainsKey(testCredentialKey));
        }

        [Fact]
        public async Task HostProvider_EraseCredentialAsync_NoCredential_DoesNothing()
        {
            const string testCredentialKey = "git:test-cred-key";
            const string otherCredentialKey1 = "git:credential1";
            const string otherCredentialKey2 = "git:credential2";
            var input = new InputArguments(new Dictionary<string, string>());

            var context = new TestCommandContext
            {
                CredentialStore =
                {
                    [otherCredentialKey1] = new GitCredential("this.should-1", "not.be.erased-1"),
                    [otherCredentialKey2] = new GitCredential("this.should-2", "not.be.erased-2")
                }
            };
            var provider = new TestHostProvider(context) {CredentialKey = testCredentialKey};

            await ((IHostProvider) provider).EraseCredentialAsync(input);

            Assert.Equal(2, context.CredentialStore.Count);
            Assert.True(context.CredentialStore.ContainsKey(otherCredentialKey1));
            Assert.True(context.CredentialStore.ContainsKey(otherCredentialKey2));
        }

        #endregion
    }
}
