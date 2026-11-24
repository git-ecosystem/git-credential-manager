using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GitCredentialManager.Tests;

public class TestProcessManager : IProcessManager
{
    public Dictionary<int, DateTime> StartTimes { get; } = new();

    public ChildProcess CreateProcess(string path, string args, bool useShellExecute, string workingDirectory,
        Trace2ProcessClass @class)
    {
        var psi = new ProcessStartInfo(path, args)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true, // Ok to redirect stderr for testing
            UseShellExecute = useShellExecute,
            WorkingDirectory = workingDirectory ?? string.Empty
        };

        return CreateProcess(psi, @class);
    }

    public ChildProcess CreateProcess(ProcessStartInfo psi, Trace2ProcessClass @class)
    {
        return new ChildProcess(psi, @class);
    }

    public DateTime GetStartTimeUtc(int pid)
    {
        if (StartTimes.TryGetValue(pid, out DateTime startTime))
        {
            return startTime;
        }

        throw new ArgumentException($"No start time found for PID {pid}.", nameof(pid));
    }
}
