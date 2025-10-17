using System.Collections.Generic;
using System.Threading.Tasks;
using GitCredentialManager.Commands;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests.Commands
{
    public class StoreCommandTests
    {[Fact]
        public async Task StoreCommand_ExecuteAsync_CallsHostProvider()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string testRefreshToken = "xyzzy";
            const long testExpiry = 1919539847;
            var stdin = $"protocol=http\nhost=example.com\nusername={testUserName}\npassword={testPassword}\noauth_refresh_token={testRefreshToken}\npassword_expiry_utc={testExpiry}\n\n";
            var expectedInput = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"]     = "example.com",
                ["username"] = testUserName,
                ["password"] = testPassword,
                ["oauth_refresh_token"] = testRefreshToken,
                ["password_expiry_utc"] = testExpiry.ToString(),
            });

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.StoreCredentialAsync(It.IsAny<InputArguments>()))
                        .Returns(Task.CompletedTask);
            var providerRegistry = new TestHostProviderRegistry {Provider = providerMock.Object};
            var context = new TestCommandContext
            {
                Streams = {In = stdin}
            };

            var command = new StoreCommand(context, providerRegistry);

            await command.ExecuteAsync();

            providerMock.Verify(
                x => x.StoreCredentialAsync(It.Is<InputArguments>(y => AreInputArgumentsEquivalent(expectedInput, y))),
                Times.Once);
        }

        bool AreInputArgumentsEquivalent(InputArguments a, InputArguments b)
        {
            return a.Protocol == b.Protocol &&
                   a.Host     == b.Host &&
                   a.Path     == b.Path &&
                   a.UserName == b.UserName &&
                   a.Password == b.Password &&
                   a.OAuthRefreshToken == b.OAuthRefreshToken &&
                   a.PasswordExpiry == b.PasswordExpiry;
        }
    }
}
