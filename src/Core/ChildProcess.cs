using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GitCredentialManager;

public class ChildProcess : DisposableObject
{
    // Increment with each new child process that is tracked
    private static int _nextTrace2Id;

    // The child process ID for Trace2 for this instance
    private readonly int _trace2Id;
    private readonly Trace2ProcessClass _processClass;
    private DateTimeOffset _startTime;

    public ProcessStartInfo StartInfo => Process.StartInfo;
    public Process Process { get; }
    public int Id => Process.Id;
    public StreamWriter StandardInput => Process.StandardInput;
    public StreamReader StandardOutput => Process.StandardOutput;
    public StreamReader StandardError => Process.StandardError;
    public int ExitCode => Process.ExitCode;

    public static ChildProcess Start(ProcessStartInfo startInfo, Trace2ProcessClass @class = Trace2ProcessClass.None)
    {
        var childProc = new ChildProcess(startInfo, @class);
        childProc.Start();
        return childProc;
    }

    public ChildProcess(ProcessStartInfo startInfo, Trace2ProcessClass @class = Trace2ProcessClass.None)
    {
        _trace2Id = Interlocked.Increment(ref _nextTrace2Id);
        _processClass = @class;
        Process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };
        Process.Exited += ProcessOnExited;
    }

    public bool Start()
    {
        ThrowIfDisposed();
        _startTime = Trace2.WriteChildStart(
            _trace2Id,
            _processClass,
            Process.StartInfo.UseShellExecute,
            Process.StartInfo.FileName,
            Process.StartInfo.Arguments);
        return Process.Start();
    }

    public void WaitForExit() => Process.WaitForExit();

    public void Kill() => Process.Kill();

    protected override void ReleaseManagedResources()
    {
        Process.Exited -= ProcessOnExited;
        Process.Dispose();
        base.ReleaseManagedResources();
    }

    private void ProcessOnExited(object sender, EventArgs e)
    {
        if (sender is Process p)
        {
            // This event may have been triggered a while after the process
            // actually exited, so we should read the exit time from the
            // process object, and not compute the current timestamp inproc.
            // Note that we continue to use the start time computed and stored
            // inproc and *not* the start time recorded by the process object.
            // This is because if the process has already exited and cleaned up
            // by the operating system by the time we try and read the start time
            // we get an error!
            var exitTime = p.ExitTime.ToUniversalTime();
            var relativeTime = exitTime - _startTime;
            Trace2.WriteChildExit(_trace2Id, relativeTime, p.Id, p.ExitCode);
        }
    }
}
