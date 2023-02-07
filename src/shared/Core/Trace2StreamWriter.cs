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

public class Trace2StreamWriter : DisposableObject, ITrace2Writer
{
    private readonly TextWriter _writer;
    private readonly Trace2FormatTarget _formatTarget;

    public bool Failed { get; private set; }

    public Trace2StreamWriter(TextWriter writer, Trace2FormatTarget formatTarget)
    {
        _writer = writer;
        _formatTarget = formatTarget;
    }

    public void Write(Trace2Message message)
    {
        try
        {
            _writer.Write(Format(message));
            _writer.Write('\n');
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

    private string Format(Trace2Message message)
    {
        EnsureArgument.NotNull(message, nameof(message));

        switch (_formatTarget)
        {
            case Trace2FormatTarget.Event:
                return message.ToJson();
            case Trace2FormatTarget.Normal:
                return message.ToNormalString();
            default:
                Console.WriteLine($"warning: unrecognized format target '{_formatTarget}', disabling TRACE2 tracing.");
                Failed = true;
                return "";
        }
    }
}
