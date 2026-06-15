using System;
using System.Threading;
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
        IAnsiConsole console = AnsiConsoleFactory.Create();

        Assert.NotNull(console);
    }

    [Fact]
    public void Create_NoInputAdapterYet_ReadKeyReturnsNull()
    {
        // Until commits 3/4 wire up real input adapters, Create() always returns a
        // console whose Input rejects every read. Headless and platform output paths
        // share this property.
        IAnsiConsole console = AnsiConsoleFactory.Create();

        Assert.Null(console.Input.ReadKey(intercept: true));
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
    public async System.Threading.Tasks.Task CreateHeadless_ReadKeyAsync_ReturnsNull()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateHeadless();

        ConsoleKeyInfo? key = await console.Input.ReadKeyAsync(intercept: true, CancellationToken.None);

        Assert.Null(key);
    }

    [Fact]
    public void CreateHeadless_Output_IsNotTerminal()
    {
        IAnsiConsole console = AnsiConsoleFactory.CreateHeadless();

        Assert.False(console.Profile.Out.IsTerminal);
    }

    [Fact]
    public void TestCommandContext_ExposesConsole()
    {
        ICommandContext ctx = new TestCommandContext();

        Assert.NotNull(ctx.Console);
    }
}

