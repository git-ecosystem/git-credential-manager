using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace GitCredentialManager;

/// <summary>
/// The different event types tracked in the TRACE2 tracing
/// system.
/// </summary>
public enum Trace2Event
{ }

public class Trace2Settings
{
    public IDictionary<Trace2FormatTarget, string> FormatTargetsAndValues { get; set; } =
        new Dictionary<Trace2FormatTarget, string>();
}

/// <summary>
/// Represents the application's TRACE2 tracing system.
/// </summary>
public interface ITrace2 : IDisposable
{
    /// <summary>
    /// Initialize TRACE2 tracing by setting up any configured target formats and
    /// writing Version and Start events.
    /// </summary>
    /// <param name="error">The standard error text stream connected back to the calling process.</param>
    /// <param name="fileSystem">File system abstraction.</param>
    /// <param name="appPath">The path to the GCM application.</param>
    void Start(TextWriter error, IFileSystem fileSystem, string appPath);
}

public class Trace2 : DisposableObject, ITrace2
{
    private readonly object _writersLock = new object();
    private readonly Encoding _utf8NoBomEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private const string GitSidVariable = "GIT_TRACE2_PARENT_SID";

    private List<ITrace2Writer> _writers = new List<ITrace2Writer>();
    private IEnvironment _environment;
    private Trace2Settings _settings;
    private string[] _argv;
    private DateTimeOffset _applicationStartTime;
    private string _sid;

    public Trace2(IEnvironment environment, Trace2Settings settings, string[] argv, DateTimeOffset applicationStartTime)
    {
        _environment = environment;
        _settings = settings;
        _argv = argv;
        _applicationStartTime = applicationStartTime;

        _sid = SetSid();
    }

    public void Start(TextWriter error, IFileSystem fileSystem, string appPath)
    {
        TryParseSettings(error, fileSystem);
    }

    protected override void ReleaseManagedResources()
    {
        lock (_writersLock)
        {
            try
            {
                for (int i = 0; i < _writers.Count; i += 1)
                {
                    using (var writer = _writers[i])
                    {
                        _writers.Remove(writer);
                    }
                }
            }
            catch
            {
                /* squelch */
            }
        }

        base.ReleaseManagedResources();
    }

    internal string SetSid()
    {
        var sids = new List<string>();
        if (_environment.Variables.TryGetValue(GitSidVariable, out string parentSid))
        {
            sids.Add(parentSid);
        }

        // Add GCM "child" sid
        sids.Add(Guid.NewGuid().ToString("D"));
        var combinedSid = string.Join("/", sids);

        _environment.SetEnvironmentVariable(GitSidVariable, combinedSid);
        return combinedSid;
    }

    internal bool TryGetPipeName(string eventTarget, out string name)
    {
        // Use prefixes to determine whether target is a named pipe/socket
        if (eventTarget.Contains("af_unix:", StringComparison.OrdinalIgnoreCase) ||
            eventTarget.Contains("\\\\.\\pipe\\", StringComparison.OrdinalIgnoreCase) ||
            eventTarget.Contains("/./pipe/", StringComparison.OrdinalIgnoreCase))
        {
            name = PlatformUtils.IsWindows()
                ? eventTarget.TrimUntilLastIndexOf("\\")
                : eventTarget.TrimUntilLastIndexOf(":");
            return true;
        }

        name = "";
        return false;
    }

    private void TryParseSettings(TextWriter error, IFileSystem fileSystem)
    {
        // Set up the correct writer for every enabled format target.
        foreach (var formatTarget in _settings.FormatTargetsAndValues)
        {
            if (TryGetPipeName(formatTarget.Value, out string name)) // Write to named pipe/socket
            {
                AddWriter(new Trace2CollectorWriter((
                        () => new NamedPipeClientStream(".", name,
                            PipeDirection.Out,
                            PipeOptions.Asynchronous)
                    )
                ));
            }
            else if (formatTarget.Value.IsTruthy()) // Write to stderr
            {
                AddWriter(new Trace2StreamWriter(error, formatTarget.Key));
            }
            else if (Path.IsPathRooted(formatTarget.Value)) // Write to file
            {
                try
                {
                    Stream stream = fileSystem.OpenFileStream(formatTarget.Value, FileMode.Append,
                        FileAccess.Write, FileShare.ReadWrite);
                    AddWriter(new Trace2StreamWriter(new StreamWriter(stream, _utf8NoBomEncoding,
                        4096, leaveOpen: false), formatTarget.Key));
                }
                catch (Exception ex)
                {
                    error.WriteLine($"warning: unable to trace to file '{formatTarget.Value}': {ex.Message}");
                }
            }
        }

        if (_writers.Count == 0)
        {
            error.WriteLine("warning: unable to set up TRACE2 tracing. No traces will be written.");
        }
    }

    private void AddWriter(ITrace2Writer writer)
    {
        ThrowIfDisposed();

        lock (_writersLock)
        {
            // Try not to add the same writer more than once
            if (_writers.Contains(writer))
                return;

            _writers.Add(writer);
        }
    }

    private void WriteMessage(Trace2Message message)
    {
        ThrowIfDisposed();

        lock (_writersLock)
        {
            if (_writers.Count == 0)
            {
                return;
            }

            foreach (var writer in _writers)
            {
                if (!writer.Failed)
                {
                    writer.Write(message);
                }
            }
        }
    }
}

public abstract class Trace2Message
{
    protected const string TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffff'Z'";

    [JsonProperty("event", Order = 1)]
    public Trace2Event Event { get; set; }

    [JsonProperty("sid", Order = 2)]
    public string Sid { get; set; }

    [JsonProperty("thread", Order = 3)]
    public string Thread { get; set; }

    [JsonProperty("time", Order = 4)]
    public DateTimeOffset Time { get; set; }

    [JsonProperty("file", Order = 5)]

    public string File { get; set; }

    [JsonProperty("line", Order = 6)]
    public int Line { get; set; }

    public abstract string ToJson();

    public abstract string ToNormalString();
}
