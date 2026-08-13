using System.Diagnostics;

namespace GitCredentialManager.Tests;

public class TestProcessManager : IProcessManager
{
    public ChildProcess CreateProcess(string path, string args, bool useShellExecute, string workingDirectory)
    {
        var psi = new ProcessStartInfo(path, args)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true, // Ok to redirect stderr for testing
            UseShellExecute = useShellExecute,
            WorkingDirectory = workingDirectory ?? string.Empty
        };

        return CreateProcess(psi);
    }

    public ChildProcess CreateProcess(ProcessStartInfo psi)
    {
        return new ChildProcess(psi);
    }
}
