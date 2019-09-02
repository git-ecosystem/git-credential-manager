// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Commands
{
    public class UnconfigureCommandTests
    {
        [Theory]
        [InlineData("unconfigure", true)]
        [InlineData("UNCONFIGURE", true)]
        [InlineData("uNcOnFiGuRe", true)]
        [InlineData("get", false)]
        [InlineData("store", false)]
        [InlineData("configure", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void UnconfigureCommand_CanExecuteAsync(string argString, bool expected)
        {
            var command = new UnconfigureCommand(Mock.Of<IConfigurationService>());

            bool result = command.CanExecute(argString?.Split(null));

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task UnconfigureCommand_ExecuteAsync_User_InvokesConfigurationServiceUnconfigureUser()
        {
            var configService = new Mock<IConfigurationService>();
            configService.Setup(x => x.UnconfigureAsync(It.IsAny<ConfigurationTarget>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var context = new TestCommandContext();

            string[] cmdArgs = {"unconfigure"};
            var command = new UnconfigureCommand(configService.Object);

            await command.ExecuteAsync(context, cmdArgs);

            configService.Verify(x => x.UnconfigureAsync(ConfigurationTarget.User), Times.Once);
        }

        [Fact]
        public async Task UnconfigureCommand_ExecuteAsync_System_InvokesConfigurationServiceUnconfigureSystem()
        {
            var configService = new Mock<IConfigurationService>();
            configService.Setup(x => x.UnconfigureAsync(It.IsAny<ConfigurationTarget>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var context = new TestCommandContext();

            string[] cmdArgs = {"unconfigure", "--system"};
            var command = new UnconfigureCommand(configService.Object);

            await command.ExecuteAsync(context, cmdArgs);

            configService.Verify(x => x.UnconfigureAsync(ConfigurationTarget.System), Times.Once);
        }
    }
}
