using System.Threading.Tasks;
using GitCredentialManager.Commands;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests.Commands
{
    public class ConfigureCommandTests
    {
        [Fact]
        public async Task ConfigureCommand_ExecuteAsync_User_InvokesConfigurationServiceConfigureUser()
        {
            var configService = new Mock<IConfigurationService>();
            configService.Setup(x => x.ConfigureAsync(It.IsAny<ConfigurationTarget>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var context = new TestCommandContext();
            var command = new ConfigureCommand(context, configService.Object);

            await command.ExecuteAsync(false);

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
            var command = new ConfigureCommand(context, configService.Object);

            await command.ExecuteAsync(true);

            configService.Verify(x => x.ConfigureAsync(ConfigurationTarget.System), Times.Once);
        }
    }
}
