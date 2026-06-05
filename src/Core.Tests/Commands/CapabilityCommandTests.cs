using System.Threading.Tasks;
using GitCredentialManager.Commands;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Commands;

public class CapabilityCommandTests
{
    [Fact]
    public void CapabilityCommand_Execute_WritesVersionAndAdvertisedCapabilities()
    {
        var context = new TestCommandContext();

        var command = new CapabilityCommand(context);
        command.Execute();

        string actualOutput = context.Streams.Out.ToString().Replace("\r\n", "\n");

        // First line MUST be `version <n>` per git-credential(1) CAPABILITY format;
        // older Gits and helpers treat anything else as "no capabilities supported".
        Assert.StartsWith("version 0\n", actualOutput);

        // GCM advertises no capabilities yet; only the version line should be emitted.
        Assert.Equal("version 0\n", actualOutput);
    }

    [Fact]
    public async Task CapabilityCommand_ExecuteAsync_DoesNotReadStandardInput()
    {
        // The capability action MUST NOT read stdin (it is not in the get/store/erase
        // key=value protocol). If stdin contains anything the command should still work.
        var context = new TestCommandContext
        {
            Streams = { In = "protocol=https\nhost=example.com\n\n" },
        };

        var command = new CapabilityCommand(context);
        await Task.Run(command.Execute);

        string actualOutput = context.Streams.Out.ToString().Replace("\r\n", "\n");

        Assert.StartsWith("version 0\n", actualOutput);
    }
}
