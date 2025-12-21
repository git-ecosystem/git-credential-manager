using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

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
    ChildExit = 4,
    Error = 5,
    RegionEnter = 6,
    RegionLeave = 7,
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

/// <summary>
/// Stores various TRACE2 format targets user has enabled.
/// Check <see cref="Trace2FormatTarget"/> for supported formats.
/// </summary>
public class Trace2Settings
{
    public IDictionary<Trace2FormatTarget, string> FormatTargetsAndValues { get; set; } =
        new Dictionary<Trace2FormatTarget, string>();
}

/// <summary>
/// Specifies a "text span" (i.e. space between two pipes) for the performance format target.
/// </summary>
public class PerformanceFormatSpan
{
    public int Size { get; set; }

    public int BeginPadding { get; set; }

    public int EndPadding { get; set; }
}

/// <summary>
/// Class that manages regions.
/// </summary>
public class Region : DisposableObject
{
    private readonly ITrace2 _trace2;
    private readonly string _category;
    private readonly string _label;
    private readonly string _filePath;
    private readonly int _lineNumber;
    private readonly string _message;
    private readonly DateTimeOffset _startTime;

    public Region(ITrace2 trace2, string category, string label, string filePath, int lineNumber, string message = "")
    {
        _trace2 = trace2;
        _category = category;
        _label = label;
        _filePath = filePath;
        _lineNumber = lineNumber;
        _message = message;

        _startTime = DateTimeOffset.UtcNow;

        _trace2.WriteRegionEnter(_category, _label, _message, _filePath, _lineNumber);
    }

    protected override void ReleaseManagedResources()
    {
        double relativeTime = (DateTimeOffset.UtcNow - _startTime).TotalSeconds;
        _trace2.WriteRegionLeave(relativeTime, _category, _label, _message, _filePath, _lineNumber);
    }
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
    /// <param name="relativeTime">Runtime of child process.</param>
    /// <param name="pid">Id of exiting process.</param>
    /// <param name="code">Process exit code.</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void WriteChildExit(
        double relativeTime,
        int pid,
        int code,
        [System.Runtime.CompilerServices.CallerFilePath]
        string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber]
        int lineNumber = 0);

    /// <summary>
    /// Writes an error as a message to the trace writer.
    /// </summary>
    /// <param name="errorMessage">The error message to write.</param>
    /// <param name="parameterizedMessage">The error format string.</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void WriteError(
        string errorMessage,
        string parameterizedMessage = null,
        [System.Runtime.CompilerServices.CallerFilePath]
        string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber]
        int lineNumber = 0);

    /// <summary>
    /// Creates a region and manages entry/leaving.
    /// </summary>
    /// <param name="category">Category of region.</param>
    /// <param name="label">Description of region.</param>
    /// <param name="message">Message associated with entering region.</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    Region CreateRegion(
        string category,
        string label,
        string message = "",
        [System.Runtime.CompilerServices.CallerFilePath]
        string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber]
        int lineNumber = 0);

    /// <summary>
    /// Writes a region enter message to the trace writer.
    /// </summary>
    /// <param name="category">Category of region.</param>
    /// <param name="label">Description of region.</param>
    /// <param name="message">Message associated with entering region.</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void WriteRegionEnter(
        string category,
        string label,
        string message = "",
        [System.Runtime.CompilerServices.CallerFilePath]
        string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber]
        int lineNumber = 0);

    /// <summary>
    /// Writes a region leave message to the trace writer.
    /// </summary>
    /// <param name="relativeTime">Time of region execution.</param>
    /// <param name="category">Category of region.</param>
    /// <param name="label">Description of region.</param>
    /// <param name="message">Message associated with entering region.</param>
    /// <param name="filePath">Path of the file this method is called from.</param>
    /// <param name="lineNumber">Line number of file this method is called from.</param>
    void WriteRegionLeave(
        double relativeTime,
        string category,
        string label,
        string message = "",
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
        _sid = ProcessManager.Sid;

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
            Thread = BuildThreadName(),
            File = Path.GetFileName(filePath),
            Line = lineNumber,
            Id = ++_childProcCounter,
            Classification = processClass,
            UseShell = useShell,
            Argv = procArgs,
            ElapsedTime = (DateTimeOffset.UtcNow - _applicationStartTime).TotalSeconds,
            Depth = ProcessManager.Depth,
        });
    }

    public void WriteChildExit(
        double relativeTime,
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
            Thread = BuildThreadName(),
            File = Path.GetFileName(filePath),
            Line = lineNumber,
            Id = _childProcCounter,
            Pid = pid,
            Code = code,
            ElapsedTime = (DateTimeOffset.UtcNow - _applicationStartTime).TotalSeconds,
            RelativeTime = relativeTime,
            Depth = ProcessManager.Depth
        });
    }

    public void WriteError(
        string errorMessage,
        string parameterizedMessage = null,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
    {
        // It is possible for an error to be thrown before TRACE2 can be initialized.
        // Since certain dependencies are not available until initialization,
        // we must immediately return if this method is invoked prior to
        // initialization.
        if (!_initialized)
        {
            return;
        }

        WriteMessage(new ErrorMessage()
        {
            Event = Trace2Event.Error,
            Sid = _sid,
            Time = DateTimeOffset.UtcNow,
            Thread = BuildThreadName(),
            File = Path.GetFileName(filePath),
            Line = lineNumber,
            Message = errorMessage,
            ParameterizedMessage = parameterizedMessage ?? errorMessage,
            Depth = ProcessManager.Depth
        });
    }

    public Region CreateRegion(
        string category,
        string label,
        string message,
        string filePath,
        int lineNumber)
    {
        return new Region(this, category, label, filePath, lineNumber, message);
    }

    public void WriteRegionEnter(
        string category,
        string label,
        string message = "",
        string filePath = "",
        int lineNumber = 0)
    {
        WriteMessage(new RegionEnterMessage()
        {
            Event = Trace2Event.RegionEnter,
            Sid = _sid,
            Time = DateTimeOffset.UtcNow,
            Category = category,
            Label = label,
            Message = message == "" ? label : message,
            Thread = BuildThreadName(),
            File = Path.GetFileName(filePath),
            Line = lineNumber,
            ElapsedTime = (DateTimeOffset.UtcNow - _applicationStartTime).TotalSeconds,
            Depth = ProcessManager.Depth
        });
    }

    public void WriteRegionLeave(
        double relativeTime,
        string category,
        string label,
        string message = "",
        string filePath = "",
        int lineNumber = 0)
    {
        WriteMessage(new RegionLeaveMessage()
        {
            Event = Trace2Event.RegionLeave,
            Sid = _sid,
            Time = DateTimeOffset.UtcNow,
            Category = category,
            Label = label,
            Message = message == "" ? label : message,
            Thread = BuildThreadName(),
            File = Path.GetFileName(filePath),
            Line = lineNumber,
            ElapsedTime = (DateTimeOffset.UtcNow - _applicationStartTime).TotalSeconds,
            RelativeTime = relativeTime,
            Depth = ProcessManager.Depth
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
        if (eventTarget.StartsWith("af_unix:", StringComparison.OrdinalIgnoreCase) ||
            eventTarget.StartsWith(@"\\.\pipe\", StringComparison.OrdinalIgnoreCase) ||
            eventTarget.StartsWith("//./pipe/", StringComparison.OrdinalIgnoreCase))
        {
            name = PlatformUtils.IsWindows()
                ? eventTarget.Replace('/', '\\')
                    .TrimUntilIndexOf(@"\\.\pipe\")
                : eventTarget.Replace("af_unix:dgram:", "")
                    .Replace("af_unix:stream:", "")
                    .Replace("af_unix:", "");
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
            Thread = BuildThreadName(),
            File = Path.GetFileName(filePath),
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
            Thread = BuildThreadName(),
            File = Path.GetFileName(filePath),
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
            Time = DateTimeOffset.UtcNow,
            Thread = BuildThreadName(),
            File = Path.GetFileName(filePath),
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

    private static string BuildThreadName()
    {
        // If this is the entry thread, call it "main", per Trace2 convention
        if (Thread.CurrentThread.ManagedThreadId == 0)
        {
            return "main";
        }

        // If this is a thread pool thread, name it as such
        if (Thread.CurrentThread.IsThreadPoolThread)
        {
            return $"thread_pool_{Environment.CurrentManagedThreadId}";
        }

        // Otherwise, if the thread is named, use it!
        if (!string.IsNullOrEmpty(Thread.CurrentThread.Name))
        {
            return Thread.CurrentThread.Name;
        }

        // We don't know what this thread is!
        return string.Empty;
    }
}
