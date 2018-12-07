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
    public class EraseCommandTests
    {
        [Theory]
        [InlineData("erase", true)]
        [InlineData("ERASE", true)]
        [InlineData("eRaSe", true)]
        [InlineData("get", false)]
        [InlineData("store", false)]
        [InlineData("foobar", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void EraseCommand_CanExecuteAsync(string argString, bool expected)
        {
            var command = new EraseCommand(Mock.Of<IHostProviderRegistry>());

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
        public async Task EraseCommand_ExecuteAsync_ErasesCredential()
        {
            const string testCredentialKey = "test-cred-key";

            var provider = new TestHostProvider {CredentialKey = testCredentialKey};
            var providerRegistry = new TestHostProviderRegistry {Provider = provider};
            var context = new TestCommandContext
            {
                CredentialStore =
                {
                    [$"git:{testCredentialKey}"] = new GitCredential("john.doe", "letmein123"),
                    ["git:credential1"] = new GitCredential("this.should-1", "not.be.erased-1"),
                    ["git:credential2"] = new GitCredential("this.should-2", "not.be.erased-2")
                }
            };

            string[] cmdArgs = {"erase"};
            var command = new EraseCommand(providerRegistry);

            await command.ExecuteAsync(context, cmdArgs);

            Assert.Equal(2, context.CredentialStore.Count);
            Assert.False(context.CredentialStore.ContainsKey($"git:{testCredentialKey}"));
            Assert.True(context.CredentialStore.ContainsKey("git:credential1"));
            Assert.True(context.CredentialStore.ContainsKey("git:credential2"));
        }
    }
}
