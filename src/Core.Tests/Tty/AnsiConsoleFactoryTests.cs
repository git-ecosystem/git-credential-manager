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
    public void Create_HeadlessFallback_ReadKey_ReturnsNull()
    {
        IAnsiConsole console = AnsiConsoleFactory.Create();

        ConsoleKeyInfo? key = console.Input.ReadKey(intercept: true);

        Assert.Null(key);
    }

    [Fact]
    public void Create_HeadlessFallback_IsKeyAvailable_ReturnsFalse()
    {
        IAnsiConsole console = AnsiConsoleFactory.Create();

        Assert.False(console.Input.IsKeyAvailable());
    }

    [Fact]
    public async System.Threading.Tasks.Task Create_HeadlessFallback_ReadKeyAsync_ReturnsNull()
    {
        IAnsiConsole console = AnsiConsoleFactory.Create();

        ConsoleKeyInfo? key = await console.Input.ReadKeyAsync(intercept: true, CancellationToken.None);

        Assert.Null(key);
    }

    [Fact]
    public void TestCommandContext_ExposesConsole()
    {
        ICommandContext ctx = new TestCommandContext();

        Assert.NotNull(ctx.Console);
    }
}
