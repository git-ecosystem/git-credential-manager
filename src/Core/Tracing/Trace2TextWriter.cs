using System.IO;

namespace GitCredentialManager;

public class Trace2TextWriter(Trace2FormatTarget formatTarget, TextWriter writer) : Trace2Writer(formatTarget)
{
    public override void Write(Trace2Message message)
    {
        try
        {
            writer.Write(Format(message));
            writer.Flush();
        }
        catch
        {
            Failed = true;
        }
    }

    protected override void ReleaseManagedResources()
    {
        writer.Dispose();
        base.ReleaseManagedResources();
    }
}
