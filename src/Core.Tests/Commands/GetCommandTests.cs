using System;
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
        public async Task GetCommand_ExecuteAsync_NegotiatedCapability_EchoesIntersection()
        {
            // Git advertises authtype + state; GCM advertises state. The intersection
            // (state) MUST be echoed back via `capability[]=state` per the protocol's
            // capability negotiation rules. The unsupported authtype MUST NOT be
            // echoed.
            ICredential testCredential = new GitCredential("alice", "hunter2");
            var stdin = "protocol=https\nhost=example.com\ncapability[]=authtype\ncapability[]=state\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(new GitResponse(testCredential));
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString().Replace("\r\n", "\n");

            Assert.Contains("capability[]=state\n", actualOutput);
            Assert.DoesNotContain("capability[]=authtype", actualOutput);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_NoCapabilityFromGit_EmitsNoCapabilityLines()
        {
            // Git declares no capabilities; even though GCM advertises state, the
            // intersection is empty and no capability[] lines should be emitted.
            ICredential testCredential = new GitCredential("alice", "hunter2");
            var stdin = "protocol=https\nhost=example.com\n\n";

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

        [Fact]
        public async Task GetCommand_ExecuteAsync_YieldedResponse_EmitsEmptyResponse()
        {
            // A provider that has nothing to contribute but does not want to stop
            // the pipeline returns GitResponse.Yield(); the command MUST emit just
            // the terminating blank line (no credential fields, no quit signal) so
            // Git proceeds to the next helper or its interactive prompt.
            var stdin = "protocol=https\nhost=example.com\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(GitResponse.Yield());
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString().Replace("\r\n", "\n");

            Assert.Equal("\n", actualOutput);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_StateNegotiated_EmitsStateLinesWithGcmPrefix()
        {
            ICredential testCredential = new GitCredential("alice", "hunter2");
            var response = GitResponse.Ok(testCredential);
            response.SetState("github.account", "alice");
            response.SetState("azure.tenant", "contoso");

            var stdin = "protocol=https\nhost=example.com\ncapability[]=state\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(response);
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString().Replace("\r\n", "\n");

            Assert.Contains("state[]=gcm.github.account=alice\n", actualOutput);
            Assert.Contains("state[]=gcm.azure.tenant=contoso\n", actualOutput);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_StateNotNegotiated_DropsStateLines()
        {
            ICredential testCredential = new GitCredential("alice", "hunter2");
            var response = GitResponse.Ok(testCredential);
            response.SetState("github.account", "alice");

            // Git did NOT advertise the state capability.
            var stdin = "protocol=https\nhost=example.com\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(response);
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString();

            Assert.DoesNotContain("state[]", actualOutput);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_ContinueNegotiated_EmitsContinue1AlongsideCredential()
        {
            ICredential testCredential = new GitCredential("alice", "hunter2");
            var stdin = "protocol=https\nhost=example.com\ncapability[]=state\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(GitResponse.Continue(testCredential));
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString().Replace("\r\n", "\n");

            Assert.Contains("username=alice\n", actualOutput);
            Assert.Contains("password=hunter2\n", actualOutput);
            Assert.Contains("continue=1\n", actualOutput);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_ContinueNotNegotiated_DropsContinueLine()
        {
            ICredential testCredential = new GitCredential("alice", "hunter2");
            var stdin = "protocol=https\nhost=example.com\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(GitResponse.Continue(testCredential));
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString();

            // Credential still emitted; continue silently dropped.
            Assert.Contains("username=alice", actualOutput);
            Assert.DoesNotContain("continue=", actualOutput);
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_OutputOrdering_CapabilitiesFirstThenScalarsThenContinueThenState()
        {
            ICredential testCredential = new GitCredential("alice", "hunter2");
            var response = GitResponse.Continue(testCredential);
            response.SetState("k", "v");

            var stdin = "protocol=https\nhost=example.com\ncapability[]=state\n\n";

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<GitRequest>()))
                        .ReturnsAsync(response);
            var providerRegistry = new TestHostProviderRegistry { Provider = providerMock.Object };
            var context = new TestCommandContext { Streams = { In = stdin } };

            var command = new GetCommand(context, providerRegistry);

            await command.ExecuteAsync();

            string actualOutput = context.Streams.Out.ToString().Replace("\r\n", "\n");

            int posCapability = actualOutput.IndexOf("capability[]=state", StringComparison.Ordinal);
            int posUsername   = actualOutput.IndexOf("username=", StringComparison.Ordinal);
            int posContinue   = actualOutput.IndexOf("continue=1", StringComparison.Ordinal);
            int posState      = actualOutput.IndexOf("state[]=", StringComparison.Ordinal);

            Assert.True(posCapability >= 0 && posUsername > posCapability,
                "capability[] must precede scalar fields");
            Assert.True(posContinue > posUsername,
                "continue=1 must follow scalar fields");
            Assert.True(posState > posContinue,
                "state[] must follow continue=1");
        }

        #region Helpers

        private static IDictionary<string, string> ParseDictionary(StringBuilder sb) => ParseDictionary(sb.ToString());

        private static IDictionary<string, string> ParseDictionary(string str) => new StringReader(str).ReadDictionary();

        #endregion
    }
}
