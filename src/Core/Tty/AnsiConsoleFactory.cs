using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Windows;
using Spectre.Console;

namespace GitCredentialManager.Tty;

/// <summary>
/// Constructs the <see cref="IAnsiConsole"/> used for rich interactive prompts.
/// </summary>
/// <remarks>
/// The credential helper's stdin and stdout are reserved for the Git credential
/// protocol; we cannot let Spectre.Console talk to them. Real platform implementations
/// route Spectre over the TTY bypass (<c>/dev/tty</c> on POSIX, <c>CONIN$</c>/<c>CONOUT$</c>
/// on Windows). When no TTY is reachable, the factory returns a no-op console:
/// prompts immediately return null and rendered output is discarded. Callers must
/// treat a null prompt result as "no user available" and respond accordingly
/// (typically by returning <c>Credential.NotFound</c>).
/// </remarks>
public static class AnsiConsoleFactory
{
    /// <summary>
    /// Construct an <see cref="IAnsiConsole"/> for the current process.
    /// </summary>
    /// <remarks>
    /// Attempts to open the platform TTY bypass for output; falls back to a
    /// headless no-op console when the device is unavailable. Input adapters
    /// land in follow-on commits — until then the returned console's
    /// <see cref="IAnsiConsoleInput"/> always reports no key available.
    /// </remarks>
    public static IAnsiConsole Create()
    {
        IAnsiConsoleOutput output = TryCreatePlatformOutput();
        if (output is null)
        {
            return CreateHeadless();
        }

        IAnsiConsole inner = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = output,
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.Detect,
            Interactive = InteractionSupport.Yes,
        });

        return new AnsiConsoleWithInput(inner, new NullAnsiConsoleInput());
    }

    /// <summary>
    /// Construct an output-only <see cref="IAnsiConsole"/> over a text writer stream,
    /// for diagnostics and other messages.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Create"/> this never opens the controlling TTY: standard error
    /// is always available (and capturable via <c>2&gt;</c>), so messages are never
    /// silently dropped even when no terminal is attached. The console carries no input.
    /// Styling follows whether standard error is redirected: coloured when connected to a
    /// terminal, plain text otherwise.
    /// </remarks>
    public static IAnsiConsole CreateForWriter(TextWriter writer, bool isRedirected)
    {
        return AnsiConsole.Create(
            new AnsiConsoleSettings
            {
                Out = new TextWriterAnsiConsoleOutput(writer, isRedirected),
                Ansi = isRedirected ? AnsiSupport.No : AnsiSupport.Yes,
                ColorSystem = isRedirected ? ColorSystemSupport.NoColors : ColorSystemSupport.Detect,
                Interactive = InteractionSupport.No,
            }
        );
    }

    /// <summary>
    /// Construct an <see cref="IAnsiConsole"/> that discards output and reports no
    /// available input. Useful when no controlling terminal is reachable.
    /// </summary>
    internal static IAnsiConsole CreateHeadless()
    {
        IAnsiConsole inner = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new NullAnsiConsoleOutput(),
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Interactive = InteractionSupport.No,
        });

        return new AnsiConsoleWithInput(inner, new NullAnsiConsoleInput());
    }

    private static IAnsiConsoleOutput TryCreatePlatformOutput()
    {
        try
        {
            if (PlatformUtils.IsWindows())
            {
                return new WindowsAnsiConsoleOutput();
            }
            if (PlatformUtils.IsPosix())
            {
                return new PosixAnsiConsoleOutput();
            }
        }
        catch (IOException)
        {
            // No controlling TTY — fall back to headless.
        }
        catch (PlatformNotSupportedException)
        {
            // Unknown platform — fall back to headless.
        }
        return null;
    }

    private sealed class NullAnsiConsoleOutput : IAnsiConsoleOutput
    {
        public TextWriter Writer { get; } = TextWriter.Null;
        public bool IsTerminal => false;
        public int Width => 80;
        public int Height => 24;
        public void SetEncoding(Encoding encoding) { }
    }

    private sealed class TextWriterAnsiConsoleOutput : IAnsiConsoleOutput
    {
        public TextWriterAnsiConsoleOutput(TextWriter writer, bool isRedirected)
        {
            Writer = writer;
            IsRedirected = isRedirected;
        }

        public TextWriter Writer { get; }
        public bool IsRedirected { get; }
        public bool IsTerminal => !IsRedirected;
        public int Width => IsTerminal ? TryGet(() => Console.WindowWidth, 80) : 80;
        public int Height => IsTerminal ? TryGet(() => Console.WindowHeight, 24) : 24;
        public void SetEncoding(Encoding encoding) { }

        private static int TryGet(Func<int> get, int fallback)
        {
            try { return get(); }
            catch { return fallback; }
        }
    }

    private sealed class NullAnsiConsoleInput : IAnsiConsoleInput
    {
        public bool IsKeyAvailable() => false;
        public ConsoleKeyInfo? ReadKey(bool intercept) => null;
        public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
        {
            return Task.FromResult<ConsoleKeyInfo?>(null);
        }
    }
}

