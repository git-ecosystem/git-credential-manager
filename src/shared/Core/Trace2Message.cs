using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace GitCredentialManager;

public abstract class Trace2Message
{
    private const int SourceColumnMaxWidth = 23;
    private const string NormalPerfTimeFormat = "HH:mm:ss.ffffff";

    protected const string EmptyPerformanceSpan =  "|     |           |           |             ";
    protected const string TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffff'Z'";

    [JsonProperty("event")]
    public Trace2Event Event { get; set; }

    [JsonProperty("sid")]
    public string Sid { get; set; }

    [JsonProperty("thread")]
    public string Thread { get; set; }

    [JsonProperty("time")]
    public DateTimeOffset Time { get; set; }

    [JsonProperty("file")]

    public string File { get; set; }

    [JsonProperty("line")]
    public int Line { get; set; }

    [JsonProperty("depth")]
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
    [JsonProperty("evt")]
    public string Evt { get; set; }

    [JsonProperty("exe")]
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
    [JsonProperty("t_abs")]
    public double ElapsedTime { get; set; }

    [JsonProperty("argv")]
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
    [JsonProperty("t_abs")]
    public double ElapsedTime { get; set; }

    [JsonProperty("code")]
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
    [JsonProperty("child_id")]
    public long Id { get; set; }

    [JsonProperty("child_class")]
    public Trace2ProcessClass Classification { get; set; }

    [JsonProperty("use_shell")]
    public bool UseShell { get; set; }

    [JsonProperty("argv")]
    public IList<string> Argv { get; set; }

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
    [JsonProperty("child_id")]
    public long Id { get; set; }

    [JsonProperty("pid")]
    public int Pid { get; set; }

    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("t_rel")]
    public double RelativeTime { get; set; }

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
    [JsonProperty("msg")] public string Message { get; set; }

    [JsonProperty("format")] public string ParameterizedMessage { get; set; }

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
    [JsonProperty("repo")]

    // Defaults to 1, as does Git.
    // See https://git-scm.com/docs/api-trace2#Documentation/technical/api-trace2.txt-codeltrepo-idgtcode for details.
    public int Repo { get; set; } = 1;

    // Nested regions are not currently supported in GCM, so this value should always be 1.
    [JsonProperty("nesting")]
    public int Nesting { get; set; } = 1;

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }

    public double ElapsedTime { get; set; }
}

public class RegionEnterMessage : RegionMessage
{
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
    public double RelativeTime { get; set; }

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
