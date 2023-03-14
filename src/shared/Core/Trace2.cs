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
    Version = 0,
    Start = 1,
    Exit = 2,
    ChildStart = 3,
    ChildExit = 4
}

/// <summary>
/// Classifications of processes invoked by GCM.
/// </summary>
public enum Trace2ProcessClass
{
    None = 0,
    UIHelper = 1,
    Git = 2,
    Other = 3
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
    /// Initialize TRACE2 tracing by initializing multi-use fields and setting up any configured target formats.
    /// </summary>
    /// <param name="startTime">Approximate time calling application began executing.</param>
    void Initialize(DateTimeOffset startTime);

    /// <summary>
    /// Write Version and Start events.
    /// </summary>
    /// <param name="appPath">The path to the application.</param>
    /// <param name="args">Args passed to the application (if applicable).</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void Start(string appPath,
        string[] args,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0);

    /// <summary>
    /// Write Exit event and dispose of writers.
    /// </summary>
    /// <param name="exitCode">The exit code of the GCM application.</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void Stop(int exitCode,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0);

    /// <summary>
    /// Writes information related to startup of child process to trace writer.
    /// </summary>
    /// <param name="startTime">Time at which child process began executing.</param>
    /// <param name="processClass">Process classification.</param>
    /// <param name="useShell">Specifies whether or not OS shell was used to start the process.</param>
    /// <param name="appName">Name of application running in child process.</param>
    /// <param name="argv">Arguments specific to the child process.</param>
    /// <param name="sid">The child process's session id.</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void WriteChildStart(DateTimeOffset startTime,
        Trace2ProcessClass processClass,
        bool useShell,
        string appName,
        string argv,
        [System.Runtime.CompilerServices.CallerFilePath]
        string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber]
        int lineNumber = 0);

    /// <summary>
    /// Writes information related to exit of child process to trace writer.
    /// </summary>
    /// <param name="elapsedTime">Runtime of child process.</param>
    /// <param name="pid">Id of exiting process.</param>
    /// <param name="code">Process exit code.</param>
    /// <param name="sid">The child process's session id.</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void WriteChildExit(
        double elapsedTime,
        int pid,
        int code,
        [System.Runtime.CompilerServices.CallerFilePath]
        string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber]
        int lineNumber = 0);
}

public class Trace2 : DisposableObject, ITrace2
{
    private readonly ICommandContext _commandContext;
    private readonly object _writersLock = new object();
    private readonly Encoding _utf8NoBomEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly List<ITrace2Writer> _writers = new List<ITrace2Writer>();

    private const string GitSidVariable = "GIT_TRACE2_PARENT_SID";

    private DateTimeOffset _applicationStartTime;
    private Trace2Settings _settings;
    private string _sid;

    private bool _initialized;
    // Increment with each new child process that is tracked
    private int _childProcCounter = 0;

    public Trace2(ICommandContext commandContext)
    {
        _commandContext = commandContext;
    }

    public void Initialize(DateTimeOffset startTime)
    {
        if (_initialized)
        {
            return;
        }

        _applicationStartTime = startTime;
        _settings = _commandContext.Settings.GetTrace2Settings();
        _sid = SidManager.Sid;

        InitializeWriters();

        _initialized = true;
    }

    public void Start(string appPath,
        string[] args,
        string filePath,
        int lineNumber)
    {
        if (!AssemblyUtils.TryGetAssemblyVersion(out string version))
        {
            // A version is required for TRACE2, so if this call fails
            // manually set the version.
            version = "0.0.0";
        }
        WriteVersion(version, filePath, lineNumber);
        WriteStart(appPath, args, filePath, lineNumber);
    }

    public void Stop(int exitCode, string filePath, int lineNumber)
    {
        WriteExit(exitCode, filePath, lineNumber);
    }

    public void WriteChildStart(DateTimeOffset startTime,
        Trace2ProcessClass processClass,
        bool useShell,
        string appName,
        string argv,
        string filePath = "",
        int lineNumber = 0)
    {
        // Some child processes are started before TRACE2 can be initialized.
        // Since certain dependencies are not available until initialization,
        // we must immediately return if this method is invoked prior to
        // initialization.
        if (!_initialized)
        {
            return;
        }

        // Always add name of the application the process is executing
        var procArgs = new List<string>()
        {
            Path.GetFileName(appName)
        };

        // If the process has arguments, append them.
        if (!string.IsNullOrEmpty(argv))
        {
            procArgs.AddRange(argv.Split(' '));
        }

        WriteMessage(new ChildStartMessage()
        {
            Event = Trace2Event.ChildStart,
            Sid = _sid,
            Time = startTime,
            File = Path.GetFileName(filePath).ToLower(),
            Line = lineNumber,
            Id = ++_childProcCounter,
            Classification = processClass,
            UseShell = useShell,
            Argv = procArgs
        });
    }

    public void WriteChildExit(
        double elapsedTime,
        int pid,
        int code,
        string filePath = "",
        int lineNumber = 0)
    {
        // Some child processes are started before TRACE2 can be initialized.
        // Since certain dependencies are not available until initialization,
        // we must immediately return if this method is invoked prior to
        // initialization.
        if (!_initialized)
        {
            return;
        }

        WriteMessage(new ChildExitMessage()
        {
            Event = Trace2Event.ChildExit,
            Sid = _sid,
            Time = DateTimeOffset.UtcNow,
            File = Path.GetFileName(filePath).ToLower(),
            Line = lineNumber,
            Id = _childProcCounter,
            Pid = pid,
            Code = code,
            ElapsedTime = elapsedTime
        });
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

    internal static bool TryGetPipeName(string eventTarget, out string name)
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

    private void InitializeWriters()
    {
        // Set up the correct writer for every enabled format target.
        foreach (var formatTarget in _settings.FormatTargetsAndValues)
        {
            if (TryGetPipeName(formatTarget.Value, out string name)) // Write to named pipe/socket
            {
                AddWriter(new Trace2CollectorWriter(formatTarget.Key, (
                        () => new NamedPipeClientStream(".", name,
                            PipeDirection.Out,
                            PipeOptions.Asynchronous)
                    )
                ));
            }
            else if (formatTarget.Value.IsTruthy()) // Write to stderr
            {
                AddWriter(new Trace2StreamWriter(formatTarget.Key, _commandContext.Streams.Error));
            }
            else if (Path.IsPathRooted(formatTarget.Value)) // Write to file
            {
                try
                {
                    AddWriter(new Trace2FileWriter(formatTarget.Key, formatTarget.Value));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"warning: unable to trace to file '{formatTarget.Value}': {ex.Message}");
                }
            }
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
        string[] args,
        string filePath,
        int lineNumber)
    {
        // Prepend GCM exe to arguments
        var argv = new List<string>()
        {
            Path.GetFileName(appPath),
        };

        if (args.Length > 0)
        {
            argv.AddRange(args);
        }

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

        if (!_initialized)
        {
            return;
        }

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

    // TODO: Remove this default value when TRACE2 regions are introduced.
    [JsonProperty("thread", Order = 3)]
    public string Thread { get; set; } = "main";

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
        return $"{time} {source,-33} {Event.ToString().ToSnakeCase()} {message}";
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
                new StringEnumConverter(typeof(SnakeCaseNamingStrategy)),
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
            new StringEnumConverter(typeof(SnakeCaseNamingStrategy)),
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
            new StringEnumConverter(typeof(SnakeCaseNamingStrategy)),
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

public class ChildStartMessage : Trace2Message
{
    [JsonProperty("child_id", Order = 7)]
    public long Id { get; set; }

    [JsonProperty("child_class", Order = 8)]
    public Trace2ProcessClass Classification { get; set; }

    [JsonProperty("use_shell", Order = 9)]
    public bool UseShell { get; set; }

    [JsonProperty("argv", Order = 10)]
    public IList<string> Argv { get; set; }

    public override string ToJson()
    {
        return JsonConvert.SerializeObject(this,
            new StringEnumConverter(typeof(SnakeCaseNamingStrategy)),
            new IsoDateTimeConverter()
            {
                DateTimeFormat = TimeFormat
            });
    }

    public override string ToNormalString()
    {
        return BuildNormalString($"[{Id}] {string.Join(" ", Argv)}");
    }
}

public class ChildExitMessage : Trace2Message
{
    [JsonProperty("child_id", Order = 7)]
    public long Id { get; set; }

    [JsonProperty("pid", Order = 8)]
    public int Pid { get; set; }

    [JsonProperty("code", Order = 9)]
    public int Code { get; set; }

    [JsonProperty("t_rel", Order = 10)]
    public double ElapsedTime { get; set; }

    public override string ToJson()
    {
        return JsonConvert.SerializeObject(this,
            new StringEnumConverter(typeof(SnakeCaseNamingStrategy)),
            new IsoDateTimeConverter()
            {
                DateTimeFormat = TimeFormat
            });
    }

    public override string ToNormalString()
    {
        return BuildNormalString($"[{Id}] pid:{Pid} code:{Code} elapsed:{ElapsedTime}");
    }
}
