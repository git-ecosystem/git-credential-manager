using System;
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
    private const string SidEnvar = "GIT_TRACE2_PARENT_SID";

    protected readonly ITrace2 Trace2;

    public static string Sid { get; internal set; }

    public static int Depth { get; internal set; }

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

    /// <summary>
    /// Create a TRACE2 "session id" (sid) for this process.
    /// </summary>
    public static void CreateSid()
    {
        Sid = Environment.GetEnvironmentVariable(SidEnvar);

        if (!string.IsNullOrEmpty(Sid))
        {
            // Use trim to ensure no accidental leading or trailing slashes
            Sid = $"{Sid.Trim('/')}/{Guid.NewGuid():D}";
            // Only check for process depth if there is a parent.
            // If there is not a parent, depth defaults to 0.
            Depth = GetProcessDepth();
        }
        else
        {
            // We are the root process; create our own 'root' SID
            Sid = Guid.NewGuid().ToString("D");
        }

        Environment.SetEnvironmentVariable(SidEnvar, Sid);
    }

    /// <summary>
    /// Get "depth" of current process relative to top-level GCM process.
    /// </summary>
    /// <returns>Depth of current process.</returns>
    internal static int GetProcessDepth()
    {
        char processSeparator = '/';

        int count = 0;
        // Use AsSpan() for slight performance bump over traditional foreach loop.
        foreach (var c in Sid.AsSpan())
        {
            if (c == processSeparator)
                count++;
        }

        return count;
    }
}
