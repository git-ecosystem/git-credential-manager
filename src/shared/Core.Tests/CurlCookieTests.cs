using System;
using System.Collections.Generic;
using System.Net;
using GitCredentialManager;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Core.Tests;

public class CurlCookieParserTests
{
    [Fact]
    public void CurlCookieParser_EmptyFile_ReturnsNoCookies()
    {
        const string content = "";

        var trace = new NullTrace();
        var parser = new CurlCookieParser(trace);

        IList<Cookie> actual = parser.Parse(content);

        Assert.Empty(actual);
    }

    [Fact]
    public void CurlCookieParser_Parse_MissingFields_SkipsInvalidLines()
    {
        const string content =
            // Valid cookie
            ".example.com\tTRUE\t/path/here\tTRUE\t0\tcookie1\tvalue1\n" +

            // Missing several fields - not a valid cookie so should be skipped
            ".example.com\tTRUE\tTRUE\tcookie1\tvalue1\n";

        var trace = new NullTrace();
        var parser = new CurlCookieParser(trace);

        IList<Cookie> actual = parser.Parse(content);

        Assert.Equal(1, actual.Count);
        AssertCookie(actual[0], ".example.com", "/path/here", true, 0, "cookie1", "value1");
    }

    [Fact]
    public void CurlCookieParser_Parse_MissingFields_ReturnsValidCookiesWithDefaults()
    {
        const string content =
            // Empty path field (default "/")
            ".example.com\tTRUE\t\tTRUE\t852852\tcookie1\tvalue1\n" +

            // Empty expiration field (default 0)
            ".example.com\tTRUE\t/path/here\tTRUE\t\tcookie1\tvalue1";

        var trace = new NullTrace();
        var parser = new CurlCookieParser(trace);

        IList<Cookie> actual = parser.Parse(content);

        Assert.Equal(2, actual.Count);
        AssertCookie(actual[0], ".example.com", "/", true, 852852, "cookie1", "value1");
        AssertCookie(actual[1], ".example.com", "/path/here", true, 0, "cookie1", "value1");
    }

    [Fact]
    public void CurlCookieParser_Parse_ValidFields_ReturnsValidCookies()
    {
        const string content =
            ".example.com\tTRUE\t/path\tTRUE\t0\tcookie1\tvalue1\n" +
            ".example.com\tfAlSe\t/path\ttRuE\t0\tcookie1\tvalue1\n" +
            ".example.com\tTRUE\t/path\tTRUE\t0\tcookie1 with spaces\tvalue1 with spaces\n" +
            ".example.com\tFALSE\t/path\tTRUE\t0\tcookie1\tvalue1\n" +
            "example.com\tFALSE\t/path\tTRUE\t0\tcookie1\tvalue1\n" +
            "example.com\tTRUE\t/path\tTRUE\t0\tcookie1\tvalue1\n" +
            ".example.com\tTRUE\t/path\tFALSE\t0\tcookie1\tvalue1\n" +
            ".example.com\tTRUE\t/path\tFALSE\t401654\tcookie1\tvalue1\n";

        var trace = new NullTrace();
        var parser = new CurlCookieParser(trace);

        IList<Cookie> actual = parser.Parse(content);

        Assert.Equal(8, actual.Count);
        AssertCookie(actual[0], ".example.com", "/path", true, 0, "cookie1", "value1");
        AssertCookie(actual[1], "example.com", "/path", true, 0, "cookie1", "value1");
        AssertCookie(actual[2], ".example.com", "/path", true, 0, "cookie1 with spaces", "value1 with spaces");
        AssertCookie(actual[3], "example.com", "/path", true, 0, "cookie1", "value1");
        AssertCookie(actual[4], "example.com", "/path", true, 0, "cookie1", "value1");
        AssertCookie(actual[5], "example.com", "/path", true, 0, "cookie1", "value1");
        AssertCookie(actual[6], ".example.com", "/path", false, 0, "cookie1", "value1");
        AssertCookie(actual[7], ".example.com", "/path", false, 401654, "cookie1", "value1");
    }

    [Fact]
    public void CurlCookieParser_Parse_Comments_ReturnsCookies()
    {
        const string content =
            // Comment block
            "# This is a cookie file with various comments!\n" +
            "# Lines starting with # are comments, except those that\n" +
            "# start with exactly '#HttpOnly_'.. two #s is a comment still!\n" +

            // This is still a comment!
            "##HttpOnly_ <-- this is a comment still!\n" +

            // Valid line
            ".example.com\tTRUE\t/\tTRUE\t0\tcookie1\tvalue1\n" +

            // Commented out cookie line
            "#.example.com\tTRUE\t/\tTRUE\t0\tcookie1\tvalue1\n" +

            // Valid cookie but HTTP only
            "#HttpOnly_.example.com\tTRUE\t/\tTRUE\t0\tcookie1\tvalue1\n";

        var trace = new NullTrace();
        var parser = new CurlCookieParser(trace);

        IList<Cookie> actual = parser.Parse(content);

        Assert.Equal(2, actual.Count);
        AssertCookie(actual[0], ".example.com", "/", true, 0, "cookie1", "value1");
        AssertCookie(actual[1], ".example.com", "/", true, 0, "cookie1", "value1");
    }

    private static void AssertCookie(Cookie cookie, string domain, string path, bool secureOnly, long expires, string name, string value)
    {
        Assert.Equal(name, cookie.Name);
        Assert.Equal(value, cookie.Value);
        Assert.Equal(domain, cookie.Domain);
        Assert.Equal(path, cookie.Path);
        Assert.Equal(secureOnly, cookie.Secure);
        Assert.Equal(expires, cookie.Expires.Subtract(DateTime.UnixEpoch).TotalSeconds);
    }
}
