using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization;
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
{
    [EnumMember(Value = "version")]
    Version = 0,
    [EnumMember(Value = "start")]
    Start = 1,
    [EnumMember(Value = "exit")]
    Exit = 2
}

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
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void Start(TextWriter error,
        IFileSystem fileSystem,
        string appPath,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0);

    /// <summary>
    /// Shut down TRACE2 tracing by writing Exit event and disposing of writers.
    /// </summary>
    /// <param name="exitCode">The exit code of the GCM application.</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void Stop(int exitCode,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0);
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

    public void Start(TextWriter error,
        IFileSystem fileSystem,
        string appPath,
        string filePath,
        int lineNumber)
    {
        TryParseSettings(error, fileSystem);

        if (!AssemblyUtils.TryGetAssemblyVersion(out string version))
        {
            // A version is required for TRACE2, so if this call fails
            // manually set the version.
            version = "0.0.0";
        }
        WriteVersion(version, filePath, lineNumber);
        WriteStart(appPath, filePath, lineNumber);
    }

    public void Stop(int exitCode, string filePath, int lineNumber)
    {
        WriteExit(exitCode, filePath, lineNumber);
        ReleaseManagedResources();
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

    private void WriteVersion(
        string gcmVersion,
        string filePath,
        int lineNumber,
        string eventFormatVersion = "3")
    {
        EnsureArgument.NotNull(gcmVersion, nameof(gcmVersion));

        WriteMessage(new VersionMessage()
        {
            Event = Trace2Event.Version,
            Sid = _sid,
            Time = DateTimeOffset.UtcNow,
            File = Path.GetFileName(filePath).ToLower(),
            Line = lineNumber,
            Evt = eventFormatVersion,
            Exe = gcmVersion
        });
    }

    private void WriteStart(
        string appPath,
        string filePath,
        int lineNumber)
    {
        // Prepend GCM exe to arguments
        var argv = new List<string>()
        {
            Path.GetFileName(appPath),
        };
        argv.AddRange(_argv);

        WriteMessage(new StartMessage()
        {
            Event = Trace2Event.Start,
            Sid = _sid,
            Time = DateTimeOffset.UtcNow,
            File = Path.GetFileName(filePath).ToLower(),
            Line = lineNumber,
            Argv = argv,
            ElapsedTime = (DateTimeOffset.UtcNow - _applicationStartTime).TotalSeconds
        });
    }

    private void WriteExit(int code, string filePath = "", int lineNumber = 0)
    {
        EnsureArgument.NotNull(code, nameof(code));

        WriteMessage(new ExitMessage()
        {
            Event = Trace2Event.Exit,
            Sid = _sid,
            Time = DateTimeOffset.Now,
            File = Path.GetFileName(filePath).ToLower(),
            Line = lineNumber,
            Code = code,
            ElapsedTime = (DateTimeOffset.UtcNow - _applicationStartTime).TotalSeconds
        });
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
    private const int SourceColumnMaxWidth = 23;

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

    protected string BuildNormalString(string message)
    {
        // The normal format uses local time rather than UTC time.
        string time = Time.ToLocalTime().ToString("HH:mm:ss.ffffff");

        // Source column format is file:line
        string source = $"{File.ToLower()}:{Line}";
        if (source.Length > SourceColumnMaxWidth)
        {
            source = TraceUtils.FormatSource(source, SourceColumnMaxWidth);
        }

        // Git's TRACE2 normal format is:
        // [<time> SP <filename>:<line> SP+] <event-name> [[SP] <event-message>] LF
        return $"{time} {source,-33} {Event.ToString().ToLower()} {message}";
    }
}

public class VersionMessage : Trace2Message
{
    [JsonProperty("evt", Order = 7)]
    public string Evt { get; set; }

    [JsonProperty("exe", Order = 8)]
    public string Exe { get; set; }

    public override string ToJson()
    {
        return JsonConvert.SerializeObject(this,
                new StringEnumConverter(),
            new IsoDateTimeConverter()
            {
                DateTimeFormat = TimeFormat
            });
    }

    public override string ToNormalString()
    {
        return BuildNormalString(Exe.ToLower());
    }
}

public class StartMessage : Trace2Message
{
    [JsonProperty("t_abs", Order = 7)]
    public double ElapsedTime { get; set; }

    [JsonProperty("argv", Order = 8)]
    public List<string> Argv { get; set; }

    public override string ToJson()
    {
        return JsonConvert.SerializeObject(this,
            new StringEnumConverter(),
            new IsoDateTimeConverter()
            {
                DateTimeFormat = TimeFormat
            });
    }

    public override string ToNormalString()
    {
        return BuildNormalString(string.Join(" ", Argv));
    }
}

public class ExitMessage : Trace2Message
{
    [JsonProperty("t_abs", Order = 7)]
    public double ElapsedTime { get; set; }

    [JsonProperty("code", Order = 8)]
    public int Code { get; set; }

    public override string ToJson()
    {
        return JsonConvert.SerializeObject(this,
            new StringEnumConverter(),
            new IsoDateTimeConverter()
            {
                DateTimeFormat = TimeFormat
            });
    }

    public override string ToNormalString()
    {
        return BuildNormalString($"elapsed:{ElapsedTime} code:{Code}");
    }
}
