using System;
using System.Threading.Tasks;
using Spectre.Console;

namespace GitCredentialManager.Tty;

/// <summary>
/// Wraps Spectre's <see cref="IExclusivityMode"/> so the TTY input is held in
/// raw mode for the whole duration of an interactive prompt — everything
/// Spectre runs through <c>RunExclusive</c> (e.g. <c>TextPrompt</c>, including
/// its masked <c>.Secret()</c> form) — rather than only around each keystroke
/// read.
/// </summary>
/// <remarks>
/// See <see cref="IRawModeSessionInput"/> for why per-keystroke toggling leaks
/// characters from masked prompts when the user types quickly. The raw-mode
/// session is entered <em>inside</em> the inner exclusivity scope so we do not
/// hold the terminal raw while merely waiting for exclusivity.
/// </remarks>
internal sealed class RawModeExclusivityMode : IExclusivityMode
{
    private readonly IExclusivityMode _inner;
    private readonly IRawModeSessionInput _input;

    public RawModeExclusivityMode(IExclusivityMode inner, IRawModeSessionInput input)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public T Run<T>(Func<T> func) =>
        _inner.Run(() =>
        {
            using (_input.BeginRawModeSession())
            {
                return func();
            }
        });

    public Task<T> RunAsync<T>(Func<Task<T>> func) =>
        _inner.RunAsync(async () =>
        {
            using (_input.BeginRawModeSession())
            {
                return await func().ConfigureAwait(false);
            }
        });
}
