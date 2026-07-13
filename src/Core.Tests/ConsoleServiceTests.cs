using System.IO;
using System.Text;
using GitCredentialManager.Tty;
using Spectre.Console;
using Xunit;

namespace GitCredentialManager.Tests;

public class ConsoleServiceTests
{
    [Fact]
    public void ConsoleService_WriteMethods_RouteToErrorConsoleWriter()
    {
        var err = new StringWriter();
        var console = new ConsoleService(
            AnsiConsoleFactory.CreateHeadless(),
            AnsiConsoleFactory.CreateForWriter(err, isRedirected: true));

        console.WriteInfo("info-[marker]");
        console.WriteWarning("warn-[marker]");
        console.WriteError("error-[marker]");
        console.WriteFatal("fatal-[marker]");
        console.WriteLine("line-[marker]");

        string output = err.ToString();
        Assert.Contains("info-[marker]", output);
        Assert.Contains("warn-[marker]", output);
        Assert.Contains("error-[marker]", output);
        Assert.Contains("fatal-[marker]", output);
        Assert.Contains("line-[marker]", output);
    }

    [Fact]
    public void ConsoleService_WriteFatal_RoutesToAutoFlushStreamWriter()
    {
        // Mirror StandardStreams.Error exactly: a UTF-8 StreamWriter with AutoFlush.
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };

        var console = new ConsoleService(
            AnsiConsoleFactory.CreateHeadless(),
            AnsiConsoleFactory.CreateForWriter(sw, isRedirected: true));

        console.WriteFatal("fatal-marker");
        sw.Flush();

        string output = new UTF8Encoding(false).GetString(ms.ToArray());
        Assert.Contains("fatal-marker", output);
    }
}
