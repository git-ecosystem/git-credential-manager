using System;

namespace GitCredentialManager.Tty;

/// <summary>
/// Implemented by TTY input adapters that can hold the terminal in raw mode for
/// the duration of an interactive prompt, rather than toggling it around each
/// individual keystroke read.
/// </summary>
/// <remarks>
/// Toggling raw mode per keystroke leaves a brief window between reads in which
/// the terminal is back in cooked mode with echo on. A user typing faster than
/// the prompt consumes keys can have characters echoed by the terminal driver
/// in that window — which for a masked/secret prompt leaks them in clear text.
/// Holding raw mode across the whole prompt closes that window.
/// </remarks>
internal interface IRawModeSessionInput
{
    /// <summary>
    /// Enter raw mode (if not already held) and keep it held until the returned
    /// scope is disposed. Reentrant with the per-keystroke reads issued inside
    /// the session: the terminal is returned to its cooked mode only once the
    /// session scope and all in-flight reads have completed.
    /// </summary>
    IDisposable BeginRawModeSession();
}
