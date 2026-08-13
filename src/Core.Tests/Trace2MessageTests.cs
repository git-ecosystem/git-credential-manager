using System;
using System.Collections.Generic;
using System.Text.Json;
using GitCredentialManager;
using Xunit;

namespace Core.Tests;

public class Trace2MessageTests
{
    [Theory]
    [InlineData(0.013772,     "  0.013772 ")]
    [InlineData(26.316083,    " 26.316083 ")]
    [InlineData(100.316083,   "100.316083 ")]
    [InlineData(1000.316083,  "1000.316083")]
    [InlineData(10000.316083, "10000.316083")]
    [InlineData(100000.31608, "100000.316080")]
    public void BuildTimeSpan_Match_Returns_Expected_String(double input, string expected)
    {
        var actual = PerformanceFormatFields.GetTimeSpan(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildRepoSpan_Match_Returns_Expected_String()
    {
        var input = 1;
        var expected = " r1  ";
        var actual = PerformanceFormatFields.GetRepoSpan(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("foo",               " foo         ")]
    [InlineData("foobar",            " foobar      ")]
    [InlineData("foo_bar_baz",       " foo_bar_baz ")]
    [InlineData("foobarbazfoo",      " foobarbazfo ")]
    public void BuildCategorySpan_Match_Returns_Expected_String(string input, string expected)
    {
        var actual = PerformanceFormatFields.GetCategorySpan(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Event_Message_Without_Snake_Case_ToJson_Creates_Expected_Json()
    {
        var errorMessage = new ErrorMessage
        {
            Sid = "123",
            Thread = "main",
            Time = new DateTimeOffset(),
            File = "foo.cs",
            Line = 1,
            Depth = 1,
            Message = "bar",
            ParameterizedMessage = "baz"
        };

        var expected = "{\"event\":\"error\",\"sid\":\"123\",\"thread\":\"main\",\"time\":\"0001-01-01T00:00:00+00:00\",\"file\":\"foo.cs\",\"line\":1,\"depth\":1,\"msg\":\"bar\",\"fmt\":\"baz\"}";
        var actual = errorMessage.ToJson();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Event_Message_With_Snake_Case_ToJson_Creates_Expected_Json()
    {
        var childStartMessage = new ChildStartMessage
        {
            Sid = "123",
            Thread = "main",
            Time = new DateTimeOffset(),
            File = "foo.cs",
            Line = 1,
            Depth = 1,
            Id = 1,
            Classification = Trace2ProcessClass.UiHelper,
            UseShell = false,
            Argv = new List<string>() { "bar", "baz" },
            ElapsedTime = 0.05
        };

        var expected = "{\"event\":\"child_start\",\"sid\":\"123\",\"thread\":\"main\",\"time\":\"0001-01-01T00:00:00+00:00\",\"file\":\"foo.cs\",\"line\":1,\"depth\":1,\"t_abs\":0.05,\"argv\":[\"bar\",\"baz\"],\"child_id\":1,\"child_class\":\"ui_helper\",\"use_shell\":false}";
        var actual = childStartMessage.ToJson();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Thread_Events_ToJson_Create_Expected_Json()
    {
        var startMessage = new ThreadStartMessage
        {
            Sid = "123",
            Thread = "AppMain",
            Time = new DateTimeOffset(),
            File = "foo.cs",
            Line = 1,
            Depth = 1
        };
        var exitMessage = new ThreadExitMessage
        {
            Sid = "123",
            Thread = "AppMain",
            Time = new DateTimeOffset(),
            File = "foo.cs",
            Line = 2,
            Depth = 1,
            RelativeTime = 0.05
        };

        Assert.Equal(
            "{\"event\":\"thread_start\",\"sid\":\"123\",\"thread\":\"AppMain\",\"time\":\"0001-01-01T00:00:00+00:00\",\"file\":\"foo.cs\",\"line\":1,\"depth\":1}",
            startMessage.ToJson());
        Assert.Equal(
            "{\"event\":\"thread_exit\",\"sid\":\"123\",\"thread\":\"AppMain\",\"time\":\"0001-01-01T00:00:00+00:00\",\"file\":\"foo.cs\",\"line\":2,\"depth\":1,\"t_rel\":0.05}",
            exitMessage.ToJson());
    }

    [Fact]
    public void Data_Event_ToJson_Creates_Expected_Json()
    {
        var message = new DataMessage
        {
            Sid = "123",
            Thread = "main",
            Time = new DateTimeOffset(),
            File = "foo.cs",
            Line = 1,
            Depth = 1,
            ElapsedTime = 0.05,
            RelativeTime = 0.01,
            Repo = 1,
            Nesting = 2,
            Category = "index",
            Key = "read/cache_nr",
            Value = "3552"
        };

        const string expected = "{\"event\":\"data\",\"sid\":\"123\",\"thread\":\"main\",\"time\":\"0001-01-01T00:00:00+00:00\",\"file\":\"foo.cs\",\"line\":1,\"depth\":1,\"t_abs\":0.05,\"t_rel\":0.01,\"repo\":1,\"nesting\":2,\"category\":\"index\",\"key\":\"read/cache_nr\",\"value\":\"3552\"}";

        Assert.Equal(expected, message.ToJson());
    }

    [Fact]
    public void CommandName_Event_ToJson_Creates_Expected_Json()
    {
        var message = new CommandNameMessage
        {
            Sid = "123",
            Thread = "main",
            Time = new DateTimeOffset(),
            File = "foo.cs",
            Line = 1,
            Depth = 1,
            Name = "get",
            Hierarchy = "git/get"
        };

        const string expected = "{\"event\":\"cmd_name\",\"sid\":\"123\",\"thread\":\"main\",\"time\":\"0001-01-01T00:00:00+00:00\",\"file\":\"foo.cs\",\"line\":1,\"depth\":1,\"name\":\"get\",\"hierarchy\":\"git/get\"}";

        Assert.Equal(expected, message.ToJson());
    }

    [Fact]
    public void DataJson_Event_ToJson_Creates_Expected_Json()
    {
        using JsonDocument document = JsonDocument.Parse(
            "{\"count\":2,\"items\":[\"one\",\"two\"]}");

        var message = new DataJsonMessage
        {
            Sid = "123",
            Thread = "main",
            Time = new DateTimeOffset(),
            File = "foo.cs",
            Line = 1,
            Depth = 1,
            ElapsedTime = 0.05,
            RelativeTime = 0.01,
            Repo = 1,
            Nesting = 2,
            Category = "index",
            Key = "read/statistics",
            Value = document.RootElement
        };

        const string expected = "{\"event\":\"data_json\",\"sid\":\"123\",\"thread\":\"main\",\"time\":\"0001-01-01T00:00:00+00:00\",\"file\":\"foo.cs\",\"line\":1,\"depth\":1,\"t_abs\":0.05,\"t_rel\":0.01,\"repo\":1,\"nesting\":2,\"category\":\"index\",\"key\":\"read/statistics\",\"value\":{\"count\":2,\"items\":[\"one\",\"two\"]}}";

        Assert.Equal(expected, message.ToJson());
    }
}
