using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace GitCredentialManager;

/// <summary>
/// Stores various TRACE2 format targets user has enabled.
/// Check <see cref="Trace2FormatTarget"/> for supported formats.
/// </summary>
internal class Trace2Settings
{
    public IDictionary<Trace2FormatTarget, string> Targets { get; } =
        new Dictionary<Trace2FormatTarget, string>();
}

internal class RegionScope : DisposableObject
{
    private readonly string _category;
    private readonly string _label;
    private readonly string _filePath;
    private readonly int _lineNumber;
    private readonly string _message;
    private readonly string _thread;
    private readonly int _nesting;
    private readonly DateTimeOffset _startTime;

    internal RegionScope(
        string category,
        string label,
        string filePath,
        int lineNumber,
        string message,
        string thread,
        int nesting)
    {
        _category = category;
        _label = label;
        _filePath = filePath;
        _lineNumber = lineNumber;
        _message = message;
        _thread = thread;
        _nesting = nesting;

        _startTime = DateTimeOffset.UtcNow;

        Trace2.WriteRegionEnter(_category, _label, _message, _thread, _nesting, _filePath, _lineNumber);
    }

    protected override void ReleaseManagedResources()
    {
        double relativeTime = (DateTimeOffset.UtcNow - _startTime).TotalSeconds;
        Trace2.WriteRegionLeave(
            relativeTime, _category, _label, _message, _thread, _nesting, _filePath, _lineNumber);
        Trace2.CompleteRegion(_nesting);
    }
}

/// <summary>
/// The application's process-wide TRACE2 tracing system.
/// </summary>
public static class Trace2
{
    internal const string SidEnvar = "GIT_TRACE2_PARENT_SID";
    private static readonly object WritersLock = new object();
    private static readonly List<ITrace2Writer> Writers = new List<ITrace2Writer>();
    private static readonly AsyncLocal<int> RegionNesting = new AsyncLocal<int>();

    private static DateTimeOffset _applicationStartTime;
    private static Trace2Settings _settings;
    private static string _sid;
    private static int _depth;

    private static bool _initialized;
    // Increment with each new child process that is tracked
    private static int _childProcCounter;

    public static void Initialize(
        string[] args,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
    {
        if (_initialized)
        {
            return;
        }

        _applicationStartTime = DateTimeOffset.UtcNow;
        _sid = CreateSid();
        Environment.SetEnvironmentVariable(SidEnvar, _sid);
        _depth = GetProcessDepth(_sid);
        _settings = ReadSettings();

        InitializeWriters();
        _initialized = true;

        string appPath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];
        Start(appPath, args, filePath, lineNumber);
    }

    internal static string CreateSid()
    {
        // Use trim to ensure no accidental leading or trailing slashes
        var sid = Environment.GetEnvironmentVariable(SidEnvar)?.Trim('/');

        // If we are the root process we must create our own 'root' SID,
        // otherwise append a new UUID to the existing root.
        sid = string.IsNullOrEmpty(sid)
            ? Guid.NewGuid().ToString("D")
            : $"{sid}/{Guid.NewGuid():D}";

        return sid;
    }

    /// <summary>
    /// Get "depth" of current process relative to top-level Trace2 process.
    /// </summary>
    /// <returns>Depth of current process.</returns>
    internal static int GetProcessDepth(string sid)
    {
        const char processSeparator = '/';

        int count = 0;
        for (var i = 0; i < sid.Length; i++) // use for-loop to avoid IEnumerable overhead from a foreach-loop
        {
            if (sid[i] == processSeparator)
                count++;
        }

        return count;
    }

    private static void Start(string appPath,
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

    public static void Stop(
        int exitCode,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
    {
        WriteExit(exitCode, filePath, lineNumber);
        DisposeWriters();
    }

    public static void WriteChildStart(
        DateTimeOffset startTime,
        Trace2ProcessClass processClass,
        bool useShell,
        string appName,
        string argv,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
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
            Depth = _depth,
        });
    }

    public static void WriteChildExit(
        DateTimeOffset startTime,
        int pid,
        int code,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0) =>
        WriteChildExit(DateTimeOffset.UtcNow - startTime, pid, code, filePath, lineNumber);

    public static void WriteChildExit(
        TimeSpan relativeTime,
        int pid,
        int code,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0) =>
        WriteChildExit(relativeTime.TotalSeconds, pid, code, filePath, lineNumber);

    public static void WriteChildExit(
        double relativeTime,
        int pid,
        int code,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
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
            Depth = _depth
        });
    }

    public static void WriteError(
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
            Depth = _depth
        });
    }

    public static IDisposable StartRegion(
        string category,
        string label,
        string message = "",
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
    {
        int nesting = RegionNesting.Value + 1;
        RegionNesting.Value = nesting;
        return new RegionScope(category, label, filePath, lineNumber, message, BuildThreadName(), nesting);
    }

    internal static void WriteRegionEnter(
        string category,
        string label,
        string message,
        string thread,
        int nesting,
        string filePath,
        int lineNumber)
    {
        WriteMessage(new RegionEnterMessage()
        {
            Event = Trace2Event.RegionEnter,
            Sid = _sid,
            Time = DateTimeOffset.UtcNow,
            Category = category,
            Label = label,
            Message = message == "" ? label : message,
            Thread = thread,
            File = Path.GetFileName(filePath),
            Line = lineNumber,
            ElapsedTime = (DateTimeOffset.UtcNow - _applicationStartTime).TotalSeconds,
            Nesting = nesting,
            Depth = _depth
        });
    }

    internal static void WriteRegionLeave(
        double relativeTime,
        string category,
        string label,
        string message,
        string thread,
        int nesting,
        string filePath,
        int lineNumber)
    {
        WriteMessage(new RegionLeaveMessage()
        {
            Event = Trace2Event.RegionLeave,
            Sid = _sid,
            Time = DateTimeOffset.UtcNow,
            Category = category,
            Label = label,
            Message = message == "" ? label : message,
            Thread = thread,
            File = Path.GetFileName(filePath),
            Line = lineNumber,
            ElapsedTime = (DateTimeOffset.UtcNow - _applicationStartTime).TotalSeconds,
            RelativeTime = relativeTime,
            Nesting = nesting,
            Depth = _depth
        });
    }

    internal static void CompleteRegion(int nesting)
    {
        RegionNesting.Value = Math.Max(0, nesting - 1);
    }

    private static void DisposeWriters()
    {
        lock (WritersLock)
        {
            try
            {
                for (int i = Writers.Count - 1; i >= 0; i--)
                {
                    using (Writers[i])
                    {
                        Writers.RemoveAt(i);
                    }
                }
            }
            catch
            {
                /* squelch */
            }
        }
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

    private static Trace2Settings ReadSettings()
    {
        var settings = new Trace2Settings();
        var gitConfig = new Lazy<Dictionary<string,string>>(ReadGitConfig);

        AddTarget(settings, gitConfig,
            Trace2FormatTarget.Event,
            Constants.EnvironmentVariables.GitTrace2Event,
            Constants.GitConfiguration.Trace2.EventTarget);
        AddTarget(settings, gitConfig,
            Trace2FormatTarget.Normal,
            Constants.EnvironmentVariables.GitTrace2Normal,
            Constants.GitConfiguration.Trace2.NormalTarget);
        AddTarget(settings, gitConfig,
            Trace2FormatTarget.Performance,
            Constants.EnvironmentVariables.GitTrace2Performance,
            Constants.GitConfiguration.Trace2.PerformanceTarget);

        return settings;
    }

    private static void AddTarget(
        Trace2Settings settings,
        Lazy<Dictionary<string,string>> gitConfig,
        Trace2FormatTarget format,
        string environmentVariable,
        string configurationProperty)
    {
        string value = Environment.GetEnvironmentVariable(environmentVariable);
        if (value is null)
        {
            string key = $"{Constants.GitConfiguration.Trace2.SectionName}.{configurationProperty}";
            value = gitConfig.Value.GetValueOrDefault(key);
        }

        if (value is not null)
        {
            settings.Targets.Add(format, value);
        }
    }

    private static Dictionary<string, string> ReadGitConfig()
    {
        string programName = OperatingSystem.IsWindows() ? "git.exe" : "git";
        string gitExecPath = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.GitExecutablePath);
        string candidatePath = string.IsNullOrEmpty(gitExecPath)
            ? null
            : Path.Combine(gitExecPath, programName);
        string gitPath = candidatePath is not null && File.Exists(candidatePath)
            ? candidatePath
            : programName;

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Read all Git's 'trace2.*' configuration in one shot to avoid repeated calls
            var startInfo = new ProcessStartInfo(gitPath, "config -z --get-regexp trace2\\..*")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using Process process = Process.Start(startInfo);
            string data = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                string[] kvps = data.Split('\0', StringSplitOptions.RemoveEmptyEntries);
                foreach (string kvp in kvps)
                {
                    string[] parts = kvp.Split('\n', count: 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        dict[key] = value;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // ignore
        }

        return dict;
    }

    private static void InitializeWriters()
    {
        // Set up the correct writer for every enabled format target.
        foreach (var formatTarget in _settings.Targets)
        {
            if (TryGetPipeName(formatTarget.Value, out string name)) // Write to named pipe/socket
            {
                AddWriter(new Trace2PipeWriter(formatTarget.Key, name));
            }
            else if (formatTarget.Value.IsTruthy()) // Write to stderr
            {
                AddWriter(new Trace2TextWriter(formatTarget.Key, Console.Error));
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

    private static void WriteVersion(
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

    private static void WriteStart(
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

    private static void WriteExit(int code, string filePath = "", int lineNumber = 0)
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

    private static void AddWriter(ITrace2Writer writer)
    {
        lock (WritersLock)
        {
            // Try not to add the same writer more than once
            if (Writers.Contains(writer))
                return;

            Writers.Add(writer);
        }
    }

    private static void WriteMessage(Trace2Message message)
    {
        if (!_initialized)
        {
            return;
        }

        lock (WritersLock)
        {
            if (Writers.Count == 0)
            {
                return;
            }

            foreach (var writer in Writers)
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
        if (Thread.CurrentThread.ManagedThreadId == 1)
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
