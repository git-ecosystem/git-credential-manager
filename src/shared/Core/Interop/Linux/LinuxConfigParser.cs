using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GitCredentialManager.Interop.Linux;

public class LinuxConfigParser
{
#if NETFRAMEWORK
    private const string SQ = "'";
    private const string DQ = "\"";
    private const string Hash = "#";
#else
    private const char SQ = '\'';
    private const char DQ = '"';
    private const char Hash = '#';
#endif

    private static readonly Regex LineRegex = new(@"^\s*(?<key>[a-zA-Z0-9\.-]+)\s*=\s*(?<value>.+?)\s*(?:#.*)?$");

    private readonly ITrace _trace;

    public LinuxConfigParser(ITrace trace)
    {
        EnsureArgument.NotNull(trace, nameof(trace));

        _trace = trace;
    }

    public IDictionary<string, string> Parse(string content)
    {
        var result = new Dictionary<string, string>(GitConfigurationKeyComparer.Instance);

        IEnumerable<string> lines = content.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            // Ignore empty lines or full-line comments
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(Hash))
                continue;

            var match = LineRegex.Match(trimmedLine);
            if (!match.Success)
            {
                _trace.WriteLine($"Invalid config line format: {line}");
                continue;
            }

            string key = match.Groups["key"].Value;
            string value = match.Groups["value"].Value;

            // Remove enclosing quotes from the value, if any
            if ((value.StartsWith(DQ) && value.EndsWith(DQ)) || (value.StartsWith(SQ) && value.EndsWith(SQ)))
                value = value.Substring(1, value.Length - 2);

            result[key] = value;
        }

        return result;
    }
}
