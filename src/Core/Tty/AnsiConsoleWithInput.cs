using System;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace GitCredentialManager.Tty;

/// <summary>
/// Decorates a Spectre <see cref="IAnsiConsole"/> with a caller-supplied
/// <see cref="IAnsiConsoleInput"/>.
/// </summary>
/// <remarks>
/// <see cref="AnsiConsoleSettings"/> exposes the output sink but not the input,
/// so the only supported way to plug in a custom input implementation is to
/// decorate a Spectre-built facade. All other members forward to the inner
/// instance unchanged.
/// </remarks>
internal sealed class AnsiConsoleWithInput : IAnsiConsole
{
    private readonly IAnsiConsole _inner;

    public AnsiConsoleWithInput(IAnsiConsole inner, IAnsiConsoleInput input)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        Input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public Profile Profile => _inner.Profile;
    public IAnsiConsoleCursor Cursor => _inner.Cursor;
    public IAnsiConsoleInput Input { get; }
    public IExclusivityMode ExclusivityMode => _inner.ExclusivityMode;
    public RenderPipeline Pipeline => _inner.Pipeline;

    public void Clear(bool home) => _inner.Clear(home);
    public void Write(IRenderable renderable) => _inner.Write(renderable);
    public void WriteAnsi(Action<AnsiWriter> action) => _inner.WriteAnsi(action);
}
