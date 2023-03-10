namespace GitCredentialManager.Interop.Windows;

public class WindowsProcessManager : ProcessManager
{
    public WindowsProcessManager(ITrace2 trace2) : base(trace2)
    {
        PlatformUtils.EnsureWindows();
    }

    public override ChildProcess CreateProcess(string path, string args, bool useShellExecute, string workingDirectory)
    {
        // If we're asked to start a WSL executable we must launch via the wsl.exe command tool
        if (!useShellExecute && WslUtils.IsWslPath(path))
        {
            string wslPath = WslUtils.ConvertToDistroPath(path, out string distro);
            return WslUtils.CreateWslProcess(distro, $"{wslPath} {args}", Trace2, workingDirectory);
        }

        return base.CreateProcess(path, args, useShellExecute, workingDirectory);
    }
}
