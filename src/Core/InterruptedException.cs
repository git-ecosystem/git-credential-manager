using System;

namespace GitCredentialManager;

/// <summary>
/// Signals that the user interrupted an interactive operation (for example by
/// pressing Ctrl+C during a terminal prompt).
/// </summary>
/// <remarks>
/// Thrown from the interactive input adapters when Ctrl+C is read, after the
/// terminal has been restored.
/// </remarks>
public class InterruptedException : Exception
{
    public InterruptedException()
        : base("The operation was interrupted.") { }

    public InterruptedException(string message)
        : base(message) { }

    public InterruptedException(string message, Exception innerException)
        : base(message, innerException) { }
}
