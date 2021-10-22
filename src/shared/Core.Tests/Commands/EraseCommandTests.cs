using System.Collections.Generic;
using System.Threading.Tasks;
using GitCredentialManager.Commands;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests.Commands
{
    public class EraseCommandTests
    {
        [Fact]
        public async Task EraseCommand_ExecuteAsync_CallsHostProvider()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            var stdin = $"protocol=http\nhost=example.com\nusername={testUserName}\npassword={testPassword}\n\n";
            var expectedInput = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"]     = "example.com",
                ["username"] = testUserName,
                ["password"] = testPassword // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            });

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.EraseCredentialAsync(It.IsAny<InputArguments>()))
                        .Returns(Task.CompletedTask);
            var providerRegistry = new TestHostProviderRegistry {Provider = providerMock.Object};
            var context = new TestCommandContext
            {
                Streams = {In = stdin}
            };

            var command = new EraseCommand(context, providerRegistry);

            await command.ExecuteAsync();

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
