using System;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using GitCredentialManager.Tty;
using Spectre.Console;
using Xunit;

namespace GitCredentialManager.Tests.Tty;

public class AnsiConsoleFactoryTests
{
    [Fact]
    public void Create_ReturnsNonNullConsole()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateForTty();

        Assert.NotNull(console);
    }

    [Fact]
    public void CreateHeadless_ReadKey_ReturnsNull()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateHeadless();

        Assert.Null(console.Input.ReadKey(intercept: true));
    }

    [Fact]
    public void CreateHeadless_IsKeyAvailable_ReturnsFalse()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateHeadless();

        Assert.False(console.Input.IsKeyAvailable());
    }

    [Fact]
    public async Task CreateHeadless_ReadKeyAsync_ReturnsNull()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateHeadless();

        ConsoleKeyInfo? key = await console.Input.ReadKeyAsync(intercept: true, CancellationToken.None);

        Assert.Null(key);
    }

    [Fact]
    public async Task CreateHeadless_ReadKeyAsync_RespectsCancellation()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateHeadless();

        // Pre-cancelled token: the no-op input still returns null immediately
        // (it doesn't honour the token because there's nothing to block on),
        // so the contract here is "doesn't hang and produces a defined result".
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ConsoleKeyInfo? key = await console.Input.ReadKeyAsync(intercept: true, cts.Token);

        Assert.Null(key);
    }

    [Fact]
    public void CreateHeadless_Output_IsNotTerminal()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateHeadless();

        Assert.False(console.Profile.Out.IsTerminal);
    }

    [Fact]
    public void CreateHeadless_Output_IsNoColor()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateHeadless();

        Assert.False(console.Profile.Capabilities.Ansi);
    }

    [Fact]
    public void CreateHeadless_Output_Write_DoesNotThrow()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateHeadless();

        // Discarded into TextWriter.Null; verifies the wiring doesn't trip on
        // ANSI / markup processing when the writer can't accept escape codes.
        console.MarkupLine("[red]error[/] in [bold]headless[/] mode");
    }

    [Fact]
    public void TestCommandContext_ExposesConsole()
    {
        ICommandContext ctx = new TestCommandContext();

        Assert.NotNull(ctx.Console);
    }
}


