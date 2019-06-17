// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
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

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task EraseCommand_ExecuteAsync_CallsHostProvider()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            var stdin = $"username={testUserName}\npassword={testPassword}\n\n";
            var expectedInput = new InputArguments(new Dictionary<string, string>
            {
                ["username"] = testUserName,
                ["password"] = testPassword
            });

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.EraseCredentialAsync(It.IsAny<InputArguments>()))
                        .Returns(Task.CompletedTask);
            var providerRegistry = new TestHostProviderRegistry {Provider = providerMock.Object};
            var context = new TestCommandContext {StdIn = stdin};

            string[] cmdArgs = {"erase"};
            var command = new EraseCommand(providerRegistry);

            await command.ExecuteAsync(context, cmdArgs);

            providerMock.Verify(
                x => x.EraseCredentialAsync(It.Is<InputArguments>(y => AreInputArgumentsEquivalent(expectedInput, y))),
                Times.Once);
        }

        private static bool AreInputArgumentsEquivalent(InputArguments a, InputArguments b)
        {
            return a.Protocol == b.Protocol &&
                   a.Host     == b.Host &&
                   a.Path     == b.Path &&
                   a.UserName == b.UserName &&
                   a.Password == b.Password;
        }
    }
}
