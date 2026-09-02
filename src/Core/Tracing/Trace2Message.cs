using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace GitCredentialManager;

/// <summary>
/// The different event types tracked in the Trace2 tracing system.
/// </summary>
public enum Trace2Event
{
    [JsonStringEnumMemberName("version")]
    Version,
    [JsonStringEnumMemberName("start")]
    Start,
    [JsonStringEnumMemberName("exit")]
    Exit,
    [JsonStringEnumMemberName("child_start")]
    ChildStart,
    [JsonStringEnumMemberName("child_exit")]
    ChildExit,
    [JsonStringEnumMemberName("error")]
    Error,
    [JsonStringEnumMemberName("region_enter")]
    RegionEnter,
    [JsonStringEnumMemberName("region_leave")]
    RegionLeave,
    [JsonStringEnumMemberName("thread_start")]
    ThreadStart,
    [JsonStringEnumMemberName("thread_exit")]
    ThreadExit,
    [JsonStringEnumMemberName("data")]
    Data,
    [JsonStringEnumMemberName("data_json")]
    DataJson,
    [JsonStringEnumMemberName("cmd_name")]
    CommandName,
}

[JsonSerializable(typeof(VersionMessage))]
[JsonSerializable(typeof(StartMessage))]
[JsonSerializable(typeof(ExitMessage))]
[JsonSerializable(typeof(ChildStartMessage))]
[JsonSerializable(typeof(ChildExitMessage))]
[JsonSerializable(typeof(ThreadStartMessage))]
[JsonSerializable(typeof(ThreadExitMessage))]
[JsonSerializable(typeof(ErrorMessage))]
[JsonSerializable(typeof(RegionEnterMessage))]
[JsonSerializable(typeof(RegionLeaveMessage))]
[JsonSerializable(typeof(DataMessage))]
[JsonSerializable(typeof(CommandNameMessage))]
[JsonSerializable(typeof(DataJsonMessage))]
[JsonSourceGenerationOptions(
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true
)]
public partial class Trace2JsonContext : JsonSerializerContext;

internal class PerformanceFormatFields
{
    public static readonly PerformanceFormatFields Empty = new();

    private const string EmptySpans = "|     |           |           |             ";
    private const string EmptyRepo = "     ";
    private const string EmptyTime = "           ";
    private const string EmptyCategory = "             ";

    public int? Repo { get; init; }
    public double? ElapsedTime { get; init; }
    public double? RelativeTime { get; init; }
    public string Category { get; init; }

    public override string ToString()
    {
        if (ReferenceEquals(this, Empty))
        {
            return EmptySpans;
        }

        var sb = new StringBuilder("|");
        sb.Append(Repo is not null ? GetRepoSpan(Repo.Value) : EmptyRepo);

        sb.Append('|');
        sb.Append(ElapsedTime is not null ? GetTimeSpan(ElapsedTime.Value) : EmptyTime);

        sb.Append('|');
        sb.Append(RelativeTime is not null ? GetTimeSpan(RelativeTime.Value) : EmptyTime);

        sb.Append('|');
        sb.Append(Category is not null ? GetCategorySpan(Category) : EmptyCategory);

        return sb.ToString();
    }

    internal static string GetRepoSpan(int repo) =>
        GetSpan($"r{repo}", 1, 2, 5);

    internal static string GetTimeSpan(double time) =>
        GetSpan(time.ToString("F6"), 2, 1, 11);

    internal static string GetCategorySpan(string category) =>
        GetSpan(category, 1, 1, 13);

    private static string GetSpan(string data, int beginPadding, int endPadding, int size)
    {
        data ??= string.Empty;
        var paddingTotal = beginPadding + endPadding;
        var dataLimit = size - paddingTotal;
        var sizeDifference = dataLimit - data.Length;

        if (sizeDifference <= 0)
        {
            if (double.TryParse(data, out _))
            {
                // Remove all padding for values that take up the entire span
                if (Math.Abs(sizeDifference) >= paddingTotal)
                {
                    beginPadding = 0;
                    endPadding = 0;
                }
                else
                {
                    // Decrease BeginPadding for large time values that don't occupy entire span
                    beginPadding += sizeDifference;
                }
            }
            else
            {
                // Truncate value
                data = data.Substring(0, dataLimit);
            }
        }

        if (data.Length < dataLimit)
        {
            // Increase end padding for short values
            endPadding += sizeDifference;
        }

        var beginPaddingStr = new string(' ', beginPadding);
        var endPaddingStr = new string(' ', endPadding);

        return $"{beginPaddingStr}{data}{endPaddingStr}";
    }
}

public abstract class Trace2Message(Trace2Event @event)
{
    private const int SourceColumnMaxWidth = 23;
    private const string NormalPerfTimeFormat = "HH:mm:ss.ffffff";

    [JsonPropertyName("event")]
    [JsonPropertyOrder(1)]
    public Trace2Event Event { get; set; } = @event;

    [JsonPropertyName("sid")]
    [JsonPropertyOrder(2)]
    public string Sid { get; set; }

    [JsonPropertyName("thread")]
    [JsonPropertyOrder(3)]
    public string Thread { get; set; }

    [JsonPropertyName("time")]
    [JsonPropertyOrder(4)]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("file")]
    [JsonPropertyOrder(5)]
    public string File { get; set; }

    [JsonPropertyName("line")]
    [JsonPropertyOrder(6)]
    public int Line { get; set; }

    [JsonPropertyName("depth")]
    [JsonPropertyOrder(7)]
    public int Depth { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this, GetJsonTypeInfo());

    public string ToNormalString()
    {
        string message = GetEventMessage(Trace2FormatTarget.Normal);

        // The normal format uses local time rather than UTC time.
        string time = Time.ToLocalTime().ToString(NormalPerfTimeFormat);
        string source = GetSource();
        string eventName = Event.ToString().ToSnakeCase();

        // Git's TRACE2 normal format is:
        // [<time> SP <filename>:<line> SP+] <event-name> [[SP] <event-message>] LF
        return $"{time} {source,-33} {eventName} {message}";
    }

    public string ToPerformanceString()
    {
        string message = GetEventMessage(Trace2FormatTarget.Performance);

        // The performance format uses local time rather than UTC time.
        string time = Time.ToLocalTime().ToString(NormalPerfTimeFormat);
        string source = GetSource();
        string eventName = Event.ToString().ToSnakeCase();
        PerformanceFormatFields fields = GetPerformanceFields();

        // Git's TRACE2 performance format is:
        // [<time> SP <filename>:<line> SP+
        //     BAR SP] d<depth> SP
        //     BAR SP <thread-name> SP+
        //     BAR SP <event-name> SP+
        //     BAR SP [r<repo-id>] SP+
        //     BAR SP [<t_abs>] SP+
        //     BAR SP [<t_rel>] SP+
        //     BAR SP [<category>] SP+
        //     BAR SP DOTS* <perf-event-message>
        //     LF
        return $"{time} {source,-29}| d{Depth} | {Thread,-24} | {eventName,-12} {fields} | {message}";
    }

    private protected virtual PerformanceFormatFields GetPerformanceFields() => PerformanceFormatFields.Empty;

    protected abstract string GetEventMessage(Trace2FormatTarget formatTarget);

    protected abstract JsonTypeInfo GetJsonTypeInfo();

    private string GetSource()
    {
        // Source column format is file:line
        string source = $"{File}:{Line}";
        if (source.Length > SourceColumnMaxWidth)
        {
            return TraceUtils.FormatSource(source, SourceColumnMaxWidth);
        }

        return source;
    }
}

public class VersionMessage() : Trace2Message(Trace2Event.Version)
{
    [JsonPropertyName("evt")]
    [JsonPropertyOrder(8)]
    public string Evt { get; set; }

    [JsonPropertyName("exe")]
    [JsonPropertyOrder(9)]
    public string Exe { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.VersionMessage;

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) => Exe.ToLowerInvariant();
}

public class StartMessage() : Trace2Message(Trace2Event.Start)
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("argv")]
    [JsonPropertyOrder(9)]
    public List<string> Argv { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.StartMessage;

    private protected override PerformanceFormatFields GetPerformanceFields() => new()
    {
        ElapsedTime = ElapsedTime
    };

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) => string.Join(" ", Argv);
}

public class ExitMessage() : Trace2Message(Trace2Event.Exit)
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("code")]
    [JsonPropertyOrder(9)]
    public int Code { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.ExitMessage;

    private protected override PerformanceFormatFields GetPerformanceFields() => new()
    {
        ElapsedTime = ElapsedTime
    };

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) => $"elapsed:{ElapsedTime} code:{Code}";
}

public class ChildStartMessage() : Trace2Message(Trace2Event.ChildStart)
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("argv")]
    [JsonPropertyOrder(9)]
    public IList<string> Argv { get; set; }

    [JsonPropertyName("child_id")]
    [JsonPropertyOrder(10)]
    public long Id { get; set; }

    [JsonPropertyName("child_class")]
    [JsonPropertyOrder(11)]
    public Trace2ProcessClass Classification { get; set; }

    [JsonPropertyName("use_shell")]
    [JsonPropertyOrder(12)]
    public bool UseShell { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.ChildStartMessage;

    private protected override PerformanceFormatFields GetPerformanceFields() => new()
    {
        ElapsedTime = ElapsedTime
    };

    protected override string GetEventMessage(Trace2FormatTarget formatTarget)
    {
        var sb = new StringBuilder();

        if (formatTarget == Trace2FormatTarget.Performance)
            sb.Append($"[ch{Id}]");
        else
            sb.Append($"[{Id}]");

        sb.Append($" {string.Join(" ", Argv)}");

        return sb.ToString();
    }
}

public class ChildExitMessage() : Trace2Message(Trace2Event.ChildExit)
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("t_rel")]
    [JsonPropertyOrder(9)]
    public double RelativeTime { get; set; }

    [JsonPropertyName("child_id")]
    [JsonPropertyOrder(10)]
    public long Id { get; set; }

    [JsonPropertyName("pid")]
    [JsonPropertyOrder(11)]
    public int Pid { get; set; }

    [JsonPropertyName("code")]
    [JsonPropertyOrder(12)]
    public int Code { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.ChildExitMessage;

    private protected override PerformanceFormatFields GetPerformanceFields() => new()
    {
        ElapsedTime = ElapsedTime,
        RelativeTime = RelativeTime
    };

    protected override string GetEventMessage(Trace2FormatTarget formatTarget)
    {
        var sb = new StringBuilder();

        if (formatTarget == Trace2FormatTarget.Performance)
            sb.Append($"[ch{Id}]");
        else
            sb.Append($"[{Id}]");

        sb.Append($" pid:{Pid} code:{Code} elapsed:{RelativeTime}");
        return sb.ToString();
    }
}

public class CommandNameMessage() : Trace2Message(Trace2Event.CommandName)
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(8)]
    public string Name { get; set; }

    [JsonPropertyName("hierarchy")]
    [JsonPropertyOrder(9)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Hierarchy { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.CommandNameMessage;

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) =>
        string.IsNullOrEmpty(Hierarchy)
            ? Name
            : $"{Name} ({Hierarchy})";
}

public class ThreadStartMessage() : Trace2Message(Trace2Event.ThreadStart)
{
    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.ThreadStartMessage;

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) => Thread;
}

public class ThreadExitMessage() : Trace2Message(Trace2Event.ThreadExit)
{
    [JsonPropertyName("t_rel")]
    [JsonPropertyOrder(8)]
    public double RelativeTime { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.ThreadExitMessage;

    private protected override PerformanceFormatFields GetPerformanceFields() => new()
    {
        RelativeTime = RelativeTime
    };

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) => $"elapsed:{RelativeTime}";
}

public class ErrorMessage() : Trace2Message(Trace2Event.Error)
{
    [JsonPropertyName("msg")]
    [JsonPropertyOrder(8)]
    public string Message { get; set; }

    [JsonPropertyName("fmt")]
    [JsonPropertyOrder(9)]
    public string ParameterizedMessage { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.ErrorMessage;

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) => Message;
}

public abstract class RegionMessage(Trace2Event @event) : Trace2Message(@event)
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("repo")]
    [JsonPropertyOrder(9)]
    // Defaults to 1, as does Git.
    // See https://git-scm.com/docs/api-trace2#Documentation/technical/api-trace2.txt-codeltrepo-idgtcode for details.
    public int Repo { get; set; } = 1;

    [JsonPropertyName("nesting")]
    [JsonPropertyOrder(10)]
    public int Nesting { get; set; } = 1;

    [JsonPropertyName("category")]
    [JsonPropertyOrder(11)]
    public string Category { get; set; }

    [JsonPropertyName("label")]
    [JsonPropertyOrder(12)]
    public string Label { get; set; }

    [JsonPropertyName("msg")]
    [JsonPropertyOrder(13)]
    public string Message { get; set; }

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) => Message;
}

public class RegionEnterMessage() : RegionMessage(Trace2Event.RegionEnter)
{
    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.RegionEnterMessage;

    private protected override PerformanceFormatFields GetPerformanceFields() => new()
    {
        Repo = Repo,
        ElapsedTime = ElapsedTime,
        Category = Category
    };
}

public class RegionLeaveMessage() : RegionMessage(Trace2Event.RegionLeave)
{
    [JsonPropertyName("t_rel")]
    [JsonPropertyOrder(14)]
    public double RelativeTime { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.RegionLeaveMessage;

    private protected override PerformanceFormatFields GetPerformanceFields() => new()
    {
        Repo = Repo,
        ElapsedTime = ElapsedTime,
        RelativeTime = RelativeTime,
        Category = Category
    };
}

public class DataMessage() : Trace2Message(Trace2Event.Data)
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("t_rel")]
    [JsonPropertyOrder(9)]
    public double RelativeTime { get; set; }

    [JsonPropertyName("repo")]
    [JsonPropertyOrder(10)]
    public int Repo { get; set; } = 1;

    [JsonPropertyName("nesting")]
    [JsonPropertyOrder(11)]
    public int Nesting { get; set; }

    [JsonPropertyName("category")]
    [JsonPropertyOrder(12)]
    public string Category { get; set; }

    [JsonPropertyName("key")]
    [JsonPropertyOrder(13)]
    public string Key { get; set; }

    [JsonPropertyName("value")]
    [JsonPropertyOrder(14)]
    public string Value { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.DataMessage;

    private protected override PerformanceFormatFields GetPerformanceFields() => new()
    {
        Repo = Repo,
        ElapsedTime = ElapsedTime,
        RelativeTime = RelativeTime,
        Category = Category
    };

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) => $"{Key}:{Value}";
}

public class DataJsonMessage() : Trace2Message(Trace2Event.DataJson)
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("t_rel")]
    [JsonPropertyOrder(9)]
    public double RelativeTime { get; set; }

    [JsonPropertyName("repo")]
    [JsonPropertyOrder(10)]
    public int Repo { get; set; } = 1;

    [JsonPropertyName("nesting")]
    [JsonPropertyOrder(11)]
    public int Nesting { get; set; }

    [JsonPropertyName("category")]
    [JsonPropertyOrder(12)]
    public string Category { get; set; }

    [JsonPropertyName("key")]
    [JsonPropertyOrder(13)]
    public string Key { get; set; }

    [JsonPropertyName("value")]
    [JsonPropertyOrder(14)]
    public JsonElement Value { get; set; }

    protected override JsonTypeInfo GetJsonTypeInfo() => Trace2JsonContext.Default.DataJsonMessage;

    private protected override PerformanceFormatFields GetPerformanceFields() => new()
    {
        Repo = Repo,
        ElapsedTime = ElapsedTime,
        RelativeTime = RelativeTime,
        Category = Category
    };

    protected override string GetEventMessage(Trace2FormatTarget formatTarget) => $"{Key}:{Value.GetRawText()}";
}
