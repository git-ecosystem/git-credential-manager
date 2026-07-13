using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Testing;

namespace GitCredentialManager.Tests.Objects;

public class TestConsoleService : IConsoleService
{
    public IList<string> WrittenMessages { get; } = new List<string>();

    public TestConsole TtyConsole { get; } = new TestConsole();
    public TestConsole StdErrConsole { get; } = new TestConsole();

    public TestConsoleService()
    {
        // Selection prompts (driven via PushSelection) require an interactive console.
        TtyConsole.Profile.Capabilities.Interactive = true;
    }

    public void WriteInfo(string message) => WrittenMessages.Add(message);

    public void WriteWarning(string message) => WrittenMessages.Add(message);

    public void WriteError(string message) => WrittenMessages.Add(message);

    public void WriteFatal(string message) => WrittenMessages.Add(message);

    public void WriteLine(string message) => WrittenMessages.Add(message);

    public T ShowPrompt<T>(IPrompt<T> prompt) => prompt.Show(TtyConsole);

    public Task<T> ShowPromptAsync<T>(IPrompt<T> prompt, CancellationToken ct = default) =>
        prompt.ShowAsync(TtyConsole, ct);

    /// <summary>
    /// Queue the answer for the next text (or secret) prompt shown on <see cref="TtyConsole"/>.
    /// </summary>
    public void PushText(string text) => TtyConsole.Input.PushTextWithEnter(text);

    /// <summary>
    /// Queue the keystrokes needed to select the choice at the given zero-based
    /// <paramref name="index"/> in the next Spectre selection prompt shown on
    /// <see cref="TtyConsole"/>.
    /// </summary>
    public void PushSelection(int index)
    {
        for (int i = 0; i < index; i++)
        {
            TtyConsole.Input.PushKey(ConsoleKey.DownArrow);
        }

        TtyConsole.Input.PushKey(ConsoleKey.Enter);
    }
}
