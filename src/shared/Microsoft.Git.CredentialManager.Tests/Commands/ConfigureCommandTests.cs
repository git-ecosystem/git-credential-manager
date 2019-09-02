// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Commands
{
    public class ConfigureCommandTests
    {
        [Theory]
        [InlineData("configure", true)]
        [InlineData("CONFIGURE", true)]
        [InlineData("cOnFiGuRe", true)]
        [InlineData("get", false)]
        [InlineData("store", false)]
        [InlineData("unconfigure", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void ConfigureCommand_CanExecuteAsync(string argString, bool expected)
        {
            var command = new ConfigureCommand(Mock.Of<IConfigurationService>());

            bool result = command.CanExecute(argString?.Split(null));

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task ConfigureCommand_ExecuteAsync_User_InvokesConfigurationServiceConfigureUser()
        {
            var configService = new Mock<IConfigurationService>();
            configService.Setup(x => x.ConfigureAsync(It.IsAny<ConfigurationTarget>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var context = new TestCommandContext();

            string[] cmdArgs = {"configure"};
            var command = new ConfigureCommand(configService.Object);

            await command.ExecuteAsync(context, cmdArgs);

            configService.Verify(x => x.ConfigureAsync(ConfigurationTarget.User), Times.Once);
        }

        [Fact]
        public async Task ConfigureCommand_ExecuteAsync_System_InvokesConfigurationServiceConfigureSystem()
        {
            var configService = new Mock<IConfigurationService>();
            configService.Setup(x => x.ConfigureAsync(It.IsAny<ConfigurationTarget>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var context = new TestCommandContext();

            string[] cmdArgs = {"configure", "--system"};
            var command = new ConfigureCommand(configService.Object);

            await command.ExecuteAsync(context, cmdArgs);

            configService.Verify(x => x.ConfigureAsync(ConfigurationTarget.System), Times.Once);
        }
    }
}
