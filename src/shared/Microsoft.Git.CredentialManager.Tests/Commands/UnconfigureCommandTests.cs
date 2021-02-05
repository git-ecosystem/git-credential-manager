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
        [Fact]
        public async Task UnconfigureCommand_ExecuteAsync_User_InvokesConfigurationServiceUnconfigureUser()
        {
            var configService = new Mock<IConfigurationService>();
            configService.Setup(x => x.UnconfigureAsync(It.IsAny<ConfigurationTarget>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var context = new TestCommandContext();
            var command = new UnconfigureCommand(context, configService.Object);

            await command.ExecuteAsync(false);

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
            var command = new UnconfigureCommand(context, configService.Object);

            await command.ExecuteAsync(true);

            configService.Verify(x => x.UnconfigureAsync(ConfigurationTarget.System), Times.Once);
        }
    }
}
