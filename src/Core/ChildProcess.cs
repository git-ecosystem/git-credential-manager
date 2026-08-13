using System;
using System.Diagnostics;
using System.IO;

namespace GitCredentialManager;

public class ChildProcess : DisposableObject
{
    private DateTimeOffset _startTime;
    private DateTimeOffset _exitTime => Process.ExitTime;
    private ProcessStartInfo _startInfo => Process.StartInfo;

    private int _id => Process.Id;

    public ProcessStartInfo StartInfo => Process.StartInfo;
    public Process Process { get; }
    public StreamWriter StandardInput => Process.StandardInput;
    public StreamReader StandardOutput => Process.StandardOutput;
    public StreamReader StandardError => Process.StandardError;
    public int ExitCode => Process.ExitCode;

    public static ChildProcess Start(ProcessStartInfo startInfo, Trace2ProcessClass @class = Trace2ProcessClass.None)
    {
        var childProc = new ChildProcess(startInfo);
        childProc.Start(@class);
        return childProc;
    }

    public ChildProcess(ProcessStartInfo startInfo)
    {
        Process = new Process() { StartInfo = startInfo };
        Process.Exited += ProcessOnExited;
    }

    public bool Start(Trace2ProcessClass @class = Trace2ProcessClass.None)
    {
        ThrowIfDisposed();
        _startTime = DateTimeOffset.UtcNow;
        Trace2.WriteChildStart(
            _startTime,
            @class,
            _startInfo.UseShellExecute,
            _startInfo.FileName,
            _startInfo.Arguments);
        return Process.Start();
    }

    public void WaitForExit() => Process.WaitForExit();

    public void Kill() => Process.Kill();

    protected override void ReleaseManagedResources()
    {
        Process.Exited -= ProcessOnExited;
        Process.Dispose();
        base.ReleaseUnmanagedResources();
    }

    private void ProcessOnExited(object sender, EventArgs e)
    {
        if (sender is Process)
        {
            double elapsedTime = (_exitTime - _startTime).TotalSeconds;
            Trace2.WriteChildExit(
                elapsedTime,
                _id,
                Process.ExitCode);
        }
    }
}
