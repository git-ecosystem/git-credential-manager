using System;
using System.Collections.Generic;
using System.Net;
using GitCredentialManager;

namespace GitCredentialManager
{
    public class CurlCookieParser
    {
        private readonly ITrace _trace;

        public CurlCookieParser(ITrace trace)
        {
            _trace = trace;
        }

        public IList<Cookie> Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Array.Empty<Cookie>();
            }

            const string HttpOnlyPrefix = "#HttpOnly_";

            var cookies = new List<Cookie>();

            // Parse the cookie file content
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '\t' }, StringSplitOptions.None);
                if (parts.Length >= 7 && (!parts[0].StartsWith("#") || parts[0].StartsWith(HttpOnlyPrefix)))
                {
                    var domain = parts[0].StartsWith(HttpOnlyPrefix) ? parts[0].Substring(HttpOnlyPrefix.Length) : parts[0];
                    var includeSubdomains = StringComparer.OrdinalIgnoreCase.Equals(parts[1], "TRUE");
                    if (!includeSubdomains)
                    {
                        domain = domain.TrimStart('.');
                    }
                    var path = string.IsNullOrWhiteSpace(parts[2]) ? "/" : parts[2];
                    var secureOnly = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    var expires = ParseExpires(parts[4]);
                    var name = parts[5];
                    var value = parts[6];

                    cookies.Add(new Cookie()
                    {
                        Domain = domain,
                        Path = path,
                        Expires = expires,
                        HttpOnly = true,
                        Secure = secureOnly,
                        Name = name,
                        Value = value,
                    });
                }
                else
                {
                    _trace.WriteLine($"Invalid cookie line: {line}");
                }
            }

            return cookies;
        }

        private static DateTime ParseExpires(string expires)
        {
#if NETFRAMEWORK
            DateTime epoch = new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc);
#else
            DateTime epoch = DateTime.UnixEpoch;
#endif

            if (long.TryParse(expires, out long i))
            {
                return epoch.AddSeconds(i);
            }

            return epoch;
        }
    }
}