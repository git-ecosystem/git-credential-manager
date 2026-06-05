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
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(new GitResponse(testCredential));
            var providerRegistry = new TestHostProviderRegistry {Provider = providerMock.Object};
            var context = new TestCommandContext
            {
                Streams = {In = stdin}
            };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            IDictionary<string, string> actualStdOutDict = ParseDictionary(context.Streams.Out);

            providerMock.Verify(x => x.GetCredentialAsync(It.IsAny<GitRequest>()), Times.Once);
            Assert.Equal(expectedStdOutDict, actualStdOutDict);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_EmptyCredential_PreservesEmptyUsernameAndPassword()
        {
            // Regression: the generic provider returns empty username + password to
            // signal Windows Integrated Authentication. Those empty values MUST be
            // emitted (as `username=` / `password=`) for Git to use WIA.
            ICredential emptyCredential = new GitCredential(string.Empty, string.Empty);
            var stdin = "protocol=https\nhost=example.com\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(new GitResponse(emptyCredential));
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString().Replace("\r\n", "\n");

            Assert.Contains("username=\n", actualOutput);
            Assert.Contains("password=\n", actualOutput);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_AdditionalProperties_AreEmitted()
        {
            // Regression: the generic provider emits `ntlm=allow` via
            // GitResponse.AdditionalProperties. Those entries must round-trip
            // to standard out so Git continues to honour them.
            ICredential testCredential = new GitCredential(string.Empty, string.Empty);
            var response = new GitResponse(testCredential);
            response.AdditionalProperties["ntlm"] = "allow";

            var stdin = "protocol=https\nhost=example.com\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(response);
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            IDictionary<string, string> actualStdOutDict = ParseDictionary(context.Streams.Out);

            Assert.True(actualStdOutDict.TryGetValue("ntlm", out string ntlmValue));
            Assert.Equal("allow", ntlmValue);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_NoNegotiatedCapabilities_EmitsNoCapabilityLines()
        {
            // GCM advertises no capabilities yet; even when Git declares some,
            // the intersection is empty and no capability[] lines should be emitted.
            ICredential testCredential = new GitCredential("alice", "hunter2");
            var stdin = "protocol=https\nhost=example.com\ncapability[]=authtype\ncapability[]=state\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(new GitResponse(testCredential));
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString();

            Assert.DoesNotContain("capability", actualOutput);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_CancelledResponse_EmitsQuitAndNoCredential()
        {
            // A provider that declines to produce a credential (e.g. the user closed
            // an auth prompt) returns GitResponse.Cancel(); the command MUST emit
            // `quit=1` so Git aborts the credential acquisition pipeline rather than
            // falling back to an interactive prompt that re-asks the user. No
            // credential fields must be emitted.
            var stdin = "protocol=https\nhost=example.com\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(GitResponse.Cancel());
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString().Replace("\r\n", "\n");

            Assert.Equal("quit=1\n\n", actualOutput);
        }

        #region Helpers

        private static IDictionary<string, string> ParseDictionary(StringBuilder sb) => ParseDictionary(sb.ToString());

        private static IDictionary<string, string> ParseDictionary(string str) => new StringReader(str).ReadDictionary();

        #endregion
    }
}
