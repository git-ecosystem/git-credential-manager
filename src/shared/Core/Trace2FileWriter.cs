using System.IO;

namespace GitCredentialManager;

public class Trace2FileWriter : Trace2Writer
{
    private readonly string _path;

    public Trace2FileWriter(Trace2FormatTarget formatTarget, string path) : base(formatTarget)
    {
        _path = path;
    }

    public override void Write(Trace2Message message)
    {
        File.AppendAllText(_path, Format(message));
    }
}
