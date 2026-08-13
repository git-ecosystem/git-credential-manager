using System;
using System.Text;

namespace GitCredentialManager;

/// <summary>
/// The different format targets supported in the TRACE2 tracing
/// system.
/// </summary>
public enum Trace2FormatTarget
{
    Event,
    Normal,
    Performance
}

public interface ITrace2Writer : IDisposable
{
    bool Failed { get; }

    void Write(Trace2Message message);
}

public class Trace2Writer : DisposableObject, ITrace2Writer
{
    private readonly Trace2FormatTarget _formatTarget;

    public bool Failed { get; protected set; }

    protected Trace2Writer(Trace2FormatTarget formatTarget)
    {
        _formatTarget = formatTarget;
    }

    protected string Format(Trace2Message message)
    {
        EnsureArgument.NotNull(message, nameof(message));
        var sb = new StringBuilder();

        switch (_formatTarget)
        {
            case Trace2FormatTarget.Event:
                sb.Append(message.ToJson());
                break;
            case Trace2FormatTarget.Normal:
                sb.Append(message.ToNormalString());
                break;
            case Trace2FormatTarget.Performance:
                sb.Append(message.ToPerformanceString());
                break;
            default:
                Console.WriteLine($"warning: unrecognized format target '{_formatTarget}', disabling TRACE2 tracing.");
                Failed = true;
                break;
        }

        sb.Append('\n');
        return sb.ToString();
    }

    public virtual void Write(Trace2Message message)
    { }
}
