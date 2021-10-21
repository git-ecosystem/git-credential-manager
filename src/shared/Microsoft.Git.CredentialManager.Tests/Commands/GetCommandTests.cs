using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitCredentialManager.Commands;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests.Commands
{
    public class GetCommandTests
    {
        [Fact]
        public async Task GetCommand_ExecuteAsync_CallsHostProviderAndWritesCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            ICredential testCredential = new GitCredential(testUserName, testPassword);
            var stdin = $"protocol=http\nhost=example.com\n\n";
            var expectedStdOutDict = new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"]     = "example.com",
                ["username"] = testUserName,
                ["password"] = testPassword
            };

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<InputArguments>()))
                        .ReturnsAsync(testCredential);
            var providerRegistry = new TestHostProviderRegistry {Provider = providerMock.Object};
            var context = new TestCommandContext
            {
                Streams = {In = stdin}
            };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            IDictionary<string, string> actualStdOutDict = ParseDictionary(context.Streams.Out);

            providerMock.Verify(x => x.GetCredentialAsync(It.IsAny<InputArguments>()), Times.Once);
            Assert.Equal(expectedStdOutDict, actualStdOutDict);
        }

        #region Helpers

        private static IDictionary<string, string> ParseDictionary(StringBuilder sb) => ParseDictionary(sb.ToString());

        private static IDictionary<string, string> ParseDictionary(string str) => new StringReader(str).ReadDictionary();

        #endregion
    }
}
