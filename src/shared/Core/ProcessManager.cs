using System.Diagnostics;

namespace GitCredentialManager;

public interface IProcessManager
{
    /// <summary>
    /// Create a process ready to start.
    /// </summary>
    /// <param name="path">Absolute file path of executable or command to start.</param>
    /// <param name="args">Command line arguments to pass to executable.</param>
    /// <param name="useShellExecute">
    ///     True to resolve <paramref name="path"/> using the OS shell, false to use as an absolute file path.
    /// </param>
    /// <param name="workingDirectory">Working directory for the new process.</param>
    /// <returns><see cref="Process"/> object ready to start.</returns>
    ChildProcess CreateProcess(string path, string args, bool useShellExecute, string workingDirectory);

    /// <summary>
    /// Create a process ready to start.
    /// </summary>
    /// <param name="psi">Process start info.</param>
    /// <returns><see cref="Process"/> object ready to start.</returns>
    ChildProcess CreateProcess(ProcessStartInfo psi);
}

public class ProcessManager : IProcessManager
{
    protected readonly ITrace2 Trace2;

    public ProcessManager(ITrace2 trace2)
    {
        EnsureArgument.NotNull(trace2, nameof(trace2));

        Trace2 = trace2;
    }

    public virtual ChildProcess CreateProcess(string path, string args, bool useShellExecute, string workingDirectory)
    {
        var psi = new ProcessStartInfo(path, args)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
            UseShellExecute = useShellExecute,
            WorkingDirectory = workingDirectory ?? string.Empty
        };

        return CreateProcess(psi);
    }


    public virtual ChildProcess CreateProcess(ProcessStartInfo psi)
    {
        return new ChildProcess(Trace2, psi);
    }
}
