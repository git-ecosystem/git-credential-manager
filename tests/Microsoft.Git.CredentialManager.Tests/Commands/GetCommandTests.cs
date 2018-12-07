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
    public class GetCommandTests
    {
        [Theory]
        [InlineData("get", true)]
        [InlineData("GET", true)]
        [InlineData("gEt", true)]
        [InlineData("erase", false)]
        [InlineData("store", false)]
        [InlineData("foobar", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void GetCommand_CanExecuteAsync(string argString, bool expected)
        {
            var command = new GetCommand(Mock.Of<IHostProviderRegistry>());

            bool result = command.CanExecute(argString?.Split(null));

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
        public async Task GetCommand_ExecuteAsync_CredentialExists_WritesExistingCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "test-cred-key";
            string expectedStdOut = $"username={testUserName}\npassword={testPassword}\n\n";

            var provider = new TestHostProvider {CredentialKey = testCredentialKey};
            var providerRegistry = new TestHostProviderRegistry {Provider = provider};
            var context = new TestCommandContext
            {
                CredentialStore = {[$"git:{testCredentialKey}"] = new GitCredential(testUserName, testPassword)}
            };

            string[] cmdArgs = {"get"};
            var command = new GetCommand(providerRegistry);

            await command.ExecuteAsync(context, cmdArgs);

            Assert.Equal(expectedStdOut, context.StdOut.ToString());
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_CredentialNotExists_CreatesAndWritesNewCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            const string testCredentialKey = "test-cred-key";
            string expectedStdOut = $"username={testUserName}\npassword={testPassword}\n\n";

            var provider = new TestHostProvider
            {
                CredentialKey = testCredentialKey,
                Credential = new GitCredential(testUserName, testPassword)
            };
            var providerRegistry = new TestHostProviderRegistry {Provider = provider};
            var context = new TestCommandContext();

            string[] cmdArgs = {"get"};
            var command = new GetCommand(providerRegistry);

            await command.ExecuteAsync(context, cmdArgs);

            Assert.Equal(expectedStdOut, context.StdOut.ToString());
        }
    }
}
