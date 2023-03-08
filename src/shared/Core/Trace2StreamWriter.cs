using System;
using System.IO;

namespace GitCredentialManager;

/// <summary>
/// The different format targets supported in the TRACE2 tracing
/// system.
/// </summary>
public enum Trace2FormatTarget
{
    Event,
    Normal
}

public class Trace2StreamWriter : Trace2Writer
{
    private readonly TextWriter _writer;

    public Trace2StreamWriter(Trace2FormatTarget formatTarget, TextWriter writer)
        : base(formatTarget)
    {
        _writer = writer;
    }

    public override void Write(Trace2Message message)
    {
        try
        {
            _writer.Write(Format(message));
            _writer.Flush();
        }
        catch
        {
            Failed = true;
        }
    }

    protected override void ReleaseManagedResources()
    {
        _writer.Dispose();
        base.ReleaseManagedResources();
    }
}
