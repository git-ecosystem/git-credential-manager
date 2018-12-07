// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
using Microsoft.Git.CredentialManager.SecureStorage;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Commands
{
    public class StoreCommandTests
    {
        [Theory]
        [InlineData("store", true)]
        [InlineData("STORE", true)]
        [InlineData("sToRe", true)]
        [InlineData("get", false)]
        [InlineData("erase", false)]
        [InlineData("foobar", false)]
        [InlineData("", false)]
        public void StoreCommand_CanExecuteAsync(string argString, bool expected)
        {
            var command = new StoreCommand(Mock.Of<IHostProviderRegistry>());

            bool result = command.CanExecute(argString.Split(null));

            if (expected)
            {
                Assert.True(result);
            }
            else
            {
                Assert.False(result);
            }
        }

        [Fact]
        public async Task StoreCommand_ExecuteAsync_ProviderStoresOnCreate_DoesNotStoreCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "test-cred-key";
            string stdIn = $"username={testUserName}\npassword={testPassword}\n\n";

            var provider = new TestHostProvider
            {
                IsCredentialStoredOnCreation = true,
                CredentialKey = testCredentialKey
            };
            var providerRegistry = new TestHostProviderRegistry {Provider = provider};
            var context = new TestCommandContext {StdIn = stdIn};

            string[] cmdArgs = {"store"};
            var command = new StoreCommand(providerRegistry);

            await command.ExecuteAsync(context, cmdArgs);

            Assert.Empty(context.CredentialStore);
        }

        [Fact]
        public async Task StoreCommand_ExecuteAsync_ProviderDoesNotStoreOnCreate_StoresCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "test-cred-key";
            string stdIn = $"username={testUserName}\npassword={testPassword}\n\n";

            var provider = new TestHostProvider
            {
                IsCredentialStoredOnCreation = false,
                CredentialKey = testCredentialKey
            };
            var providerRegistry = new TestHostProviderRegistry {Provider = provider};
            var context = new TestCommandContext {StdIn = stdIn};

            string[] cmdArgs = {"store"};
            var command = new StoreCommand(providerRegistry);

            await command.ExecuteAsync(context, cmdArgs);

            Assert.Single(context.CredentialStore);
            Assert.True(context.CredentialStore.TryGetValue($"git:{testCredentialKey}", out ICredential storedCredential));
            Assert.Equal(testUserName, storedCredential.UserName);
            Assert.Equal(testPassword, storedCredential.Password);
        }

        [Fact]
        public async Task StoreCommand_ExecuteAsync_ProviderDoesNotStoreOnCreate_ExistingCredential_UpdatesCredential()
        {
            const string testUserName = "john.doe";
            const string testPasswordOld = "letmein123-old";
            const string testPasswordNew = "letmein123-new";
            const string testCredentialKey = "test-cred-key";
            string stdIn = $"username={testUserName}\npassword={testPasswordNew}\n\n";

            var provider = new TestHostProvider
            {
                IsCredentialStoredOnCreation = false,
                CredentialKey = testCredentialKey
            };
            var providerRegistry = new TestHostProviderRegistry {Provider = provider};
            var context = new TestCommandContext
            {
                StdIn = stdIn,
                CredentialStore = {[$"git:{testCredentialKey}"] = new GitCredential(testUserName, testPasswordOld)}
            };

            string[] cmdArgs = {"store"};
            var command = new StoreCommand(providerRegistry);

            await command.ExecuteAsync(context, cmdArgs);

            Assert.Single(context.CredentialStore);
            Assert.True(context.CredentialStore.TryGetValue($"git:{testCredentialKey}", out ICredential storedCredential));
            Assert.Equal(testUserName, storedCredential.UserName);
            Assert.Equal(testPasswordNew, storedCredential.Password);
        }
    }
}
