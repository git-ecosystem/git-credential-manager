using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Tty;
using Spectre.Console;

namespace GitCredentialManager;

/// <summary>
/// The user-facing console. Routes output-only messages (diagnostics, QR codes) to
/// standard error, and interactive prompts — which need to read input — to the
/// controlling terminal.
/// </summary>
/// <remarks>
/// Git's stdin/stdout are reserved for the credential protocol, so neither sink may use
/// them. Standard error is always available and capturable, so messages survive even
/// when no terminal is attached; only prompts require a TTY, since only they read input.
/// </remarks>
public interface IConsoleService
{
    void WriteInfo(string message);
    void WriteWarning(string message);
    void WriteError(string message);
    void WriteFatal(string message);
    void WriteLine(string message);

    /// <summary>
    /// Prompt the user for a selection on the controlling terminal.
    /// </summary>
    T ShowPrompt<T>(IPrompt<T> prompt);

    /// <summary>
    /// Prompt the user for a selection on the controlling terminal.
    /// </summary>
    Task<T> ShowPromptAsync<T>(IPrompt<T> prompt, CancellationToken ct = default);
}

public class ConsoleService : IConsoleService
{
    private readonly IAnsiConsole _ttyConsole;
    private readonly IAnsiConsole _stderrConsole;

    public ConsoleService(IStandardStreams streams)
        : this(AnsiConsoleFactory.Create(), AnsiConsoleFactory.CreateForWriter(streams.Error, streams.IsErrorRedirected))
    { }

    public ConsoleService(IAnsiConsole ttyConsole, IAnsiConsole stderrConsole)
    {
        _ttyConsole = ttyConsole;
        _stderrConsole = stderrConsole;
    }

    public void WriteInfo(string message) => _stderrConsole.MarkupLine($"[blue]info:[/] {Markup.Escape(message)}");

    public void WriteWarning(string message) => _stderrConsole.MarkupLine($"[yellow]warning:[/] {Markup.Escape(message)}");

    public void WriteError(string message) => _stderrConsole.MarkupLine($"[red]error:[/] {Markup.Escape(message)}");

    public void WriteFatal(string message) => _stderrConsole.MarkupLine($"[red]fatal:[/] {Markup.Escape(message)}");

    public void WriteLine(string message) => _stderrConsole.WriteLine(message);

    public T ShowPrompt<T>(IPrompt<T> prompt) =>
        prompt.Show(_ttyConsole);

    public Task<T> ShowPromptAsync<T>(IPrompt<T> prompt, CancellationToken ct = default) =>
        prompt.ShowAsync(_ttyConsole, ct);
}
