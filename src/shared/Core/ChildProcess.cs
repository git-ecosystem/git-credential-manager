using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GitCredentialManager;

public class ChildProcess : DisposableObject
{
    private readonly ITrace2 _trace2;

    private DateTimeOffset _startTime;
    private DateTimeOffset _exitTime => Process.ExitTime;

    private int _id => Process.Id;

    public ProcessStartInfo StartInfo => Process.StartInfo;
    public Process Process { get; }
    public StreamWriter StandardInput => Process.StandardInput;
    public StreamReader StandardOutput => Process.StandardOutput;
    public StreamReader StandardError => Process.StandardError;
    public int Id => Process.Id;
    public int ExitCode => Process.ExitCode;

    public static ChildProcess Start(ITrace2 trace2, ProcessStartInfo startInfo)
    {
        var childProc = new ChildProcess(trace2, startInfo);
        childProc.Start();
        return childProc;
    }

    public ChildProcess(ITrace2 trace2, ProcessStartInfo startInfo)
    {
        _trace2 = trace2;
        Process = new Process() { StartInfo = startInfo };
    }

    public void Start()
    {
        ThrowIfDisposed();
        Process.Start();
    }

    public void WaitForExit() => Process.WaitForExit();

    public void Kill() => Process.Kill();

    protected override void ReleaseManagedResources()
    {
        Process.Dispose();
        base.ReleaseUnmanagedResources();
    }
}
