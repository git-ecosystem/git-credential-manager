using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitCredentialManager;

public abstract class Trace2Message
{
    private const int SourceColumnMaxWidth = 23;
    private const string NormalPerfTimeFormat = "HH:mm:ss.ffffff";

    protected const string EmptyPerformanceSpan =  "|     |           |           |             ";
    protected static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(new SnakeCaseNamingPolicy()) }
    };

    [JsonPropertyName("event")]
    [JsonPropertyOrder(1)]
    public Trace2Event Event { get; set; }

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

    public abstract string ToJson();

    public abstract string ToNormalString();

    public abstract string ToPerformanceString();

    protected abstract string BuildPerformanceSpan();

    protected string BuildNormalString()
    {
        string message = GetEventMessage(Trace2FormatTarget.Normal);

        // The normal format uses local time rather than UTC time.
        string time = Time.ToLocalTime().ToString(NormalPerfTimeFormat);
        string source = GetSource();

        // Git's TRACE2 normal format is:
        // [<time> SP <filename>:<line> SP+] <event-name> [[SP] <event-message>] LF
        return $"{time} {source,-33} {Event.ToString().ToSnakeCase()} {message}";
    }

    protected string BuildPerformanceString()
    {
        string message = GetEventMessage(Trace2FormatTarget.Performance);

        // The performance format uses local time rather than UTC time.
        var time = Time.ToLocalTime().ToString(NormalPerfTimeFormat);
        var source = GetSource();

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
        return $"{time} {source,-29}| d{Depth} | {Thread,-24} | {Event.ToString().ToSnakeCase(),-12} {BuildPerformanceSpan()} | {message}";
    }

    protected abstract string GetEventMessage(Trace2FormatTarget formatTarget);

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

    internal static string BuildTimeSpan(double time)
    {
        var timeString = time.ToString("F6");
        var component = new PerformanceFormatSpan()
        {
            Size = 11,
            BeginPadding = 2,
            EndPadding = 1
        };

        return BuildSpan(component, timeString);
    }

    internal static string BuildCategorySpan(string category)
    {
        var component = new PerformanceFormatSpan()
        {
            Size = 13,
            BeginPadding = 1,
            EndPadding = 1
        };

        return BuildSpan(component, category);
    }

    internal static string BuildRepoSpan(int repo)
    {
        var component = new PerformanceFormatSpan()
        {
            Size = 5,
            BeginPadding = 1,
            EndPadding = 2
        };

        return BuildSpan(component, $"r{repo}");
    }

    private static string BuildSpan(PerformanceFormatSpan component, string data)
    {
        var paddingTotal = component.BeginPadding + component.EndPadding;
        var dataLimit = component.Size - paddingTotal;
        var sizeDifference = dataLimit - data.Length;

        if (sizeDifference <= 0)
        {
            if (double.TryParse(data, out _))
            {
                // Remove all padding for values that take up the entire span
                if (Math.Abs(sizeDifference) == paddingTotal)
                {
                    component.BeginPadding = 0;
                    component.EndPadding = 0;
                }
                else
                {
                    // Decrease BeginPadding for large time values that don't occupy entire span
                    component.BeginPadding += sizeDifference;
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
            component.EndPadding += sizeDifference;
        }

        var beginPadding = new string(' ', component.BeginPadding);
        var endPadding = new string(' ', component.EndPadding);

        return $"{beginPadding}{data}{endPadding}";
    }
}

public class VersionMessage : Trace2Message
{
    [JsonPropertyName("evt")]
    [JsonPropertyOrder(8)]
    public string Evt { get; set; }

    [JsonPropertyName("exe")]
    [JsonPropertyOrder(9)]
    public string Exe { get; set; }

    public override string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public override string ToNormalString()
    {
        return BuildNormalString();
    }

    public override string ToPerformanceString()
    {
        return BuildPerformanceString();
    }

    protected override string BuildPerformanceSpan()
    {
        return EmptyPerformanceSpan;
    }

    protected override string GetEventMessage(Trace2FormatTarget formatTarget)
    {
        return Exe.ToLower();
    }
}

public class StartMessage : Trace2Message
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("argv")]
    [JsonPropertyOrder(9)]
    public List<string> Argv { get; set; }

    public override string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public override string ToNormalString()
    {
        return BuildNormalString();
    }

    public override string ToPerformanceString()
    {
        return BuildPerformanceString();
    }

    protected override string BuildPerformanceSpan()
    {
        return $"|     |{BuildTimeSpan(ElapsedTime)}|           |             ";
    }

    protected override string GetEventMessage(Trace2FormatTarget formatTarget)
    {
        return string.Join(" ", Argv);
    }
}

public class ExitMessage : Trace2Message
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("code")]
    [JsonPropertyOrder(9)]
    public int Code { get; set; }

    public override string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public override string ToNormalString()
    {
        return BuildNormalString();
    }

    public override string ToPerformanceString()
    {
        return BuildPerformanceString();
    }

    protected override string BuildPerformanceSpan()
    {
        return $"|     |{BuildTimeSpan(ElapsedTime)}|           |             ";
    }

    protected override string GetEventMessage(Trace2FormatTarget formatTarget)
    {
        return $"elapsed:{ElapsedTime} code:{Code}";
    }
}

public class ChildStartMessage : Trace2Message
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

    public override string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public override string ToNormalString()
    {
        return BuildNormalString();
    }

    public override string ToPerformanceString()
    {
        return BuildPerformanceString();
    }

    protected override string BuildPerformanceSpan()
    {
        return $"|     |{BuildTimeSpan(ElapsedTime)}|           |             ";
    }

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

public class ChildExitMessage : Trace2Message
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

    public override string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public override string ToNormalString()
    {
        return BuildNormalString();
    }

    public override string ToPerformanceString()
    {
        return BuildPerformanceString();
    }

    protected override string BuildPerformanceSpan()
    {
        return $"|     |{BuildTimeSpan(ElapsedTime)}|{BuildTimeSpan(RelativeTime)}|             ";
    }

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

public class ErrorMessage : Trace2Message
{
    [JsonPropertyName("msg")]
    [JsonPropertyOrder(8)]
    public string Message { get; set; }

    [JsonPropertyName("fmt")]
    [JsonPropertyOrder(9)]
    public string ParameterizedMessage { get; set; }

    public override string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public override string ToNormalString()
    {
        return BuildNormalString();
    }

    public override string ToPerformanceString()
    {
        return BuildPerformanceString();
    }

    protected override string BuildPerformanceSpan()
    {
        return EmptyPerformanceSpan;
    }

    protected override string GetEventMessage(Trace2FormatTarget formatTarget)
    {
        return Message;
    }
}

public abstract class RegionMessage : Trace2Message
{
    [JsonPropertyName("t_abs")]
    [JsonPropertyOrder(8)]
    public double ElapsedTime { get; set; }

    [JsonPropertyName("repo")]
    [JsonPropertyOrder(9)]
    // Defaults to 1, as does Git.
    // See https://git-scm.com/docs/api-trace2#Documentation/technical/api-trace2.txt-codeltrepo-idgtcode for details.
    public int Repo { get; set; } = 1;

    // TODO: Remove default value if support for nested regions is implemented.
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
}

public class RegionEnterMessage : RegionMessage
{
    public override string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public override string ToNormalString()
    {
        return BuildNormalString();
    }

    public override string ToPerformanceString()
    {
        return BuildPerformanceString();
    }

    protected override string BuildPerformanceSpan()
    {
        return $"|{BuildRepoSpan(Repo)}|{BuildTimeSpan(ElapsedTime)}|           |{BuildCategorySpan(Category)}";
    }

    protected override string GetEventMessage(Trace2FormatTarget formatTarget)
    {
        return Message;
    }
}

public class RegionLeaveMessage : RegionMessage
{
    [JsonPropertyOrder(14)]
    public double RelativeTime { get; set; }

    public override string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public override string ToNormalString()
    {
        return BuildNormalString();
    }

    public override string ToPerformanceString()
    {
        return BuildPerformanceString();
    }

    protected override string BuildPerformanceSpan()
    {
        return $"|{BuildRepoSpan(Repo)}|{BuildTimeSpan(ElapsedTime)}|{BuildTimeSpan(RelativeTime)}|{BuildCategorySpan(Category)}";
    }

    protected override string GetEventMessage(Trace2FormatTarget formatTarget)
    {
        return Message;
    }
}

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) =>
        name.ToSnakeCase();
}
