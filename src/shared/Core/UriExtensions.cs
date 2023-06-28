using System;
using System.Collections.Generic;
using System.Net;
using System.Web;

namespace GitCredentialManager
{
    public static class UriExtensions
    {
        public static IDictionary<string, string> ParseQueryString(string queryString)
        {
            var dict = new Dictionary<string, string>();

            string[] queryParts = queryString.Split('&');
            foreach (var queryPart in queryParts)
            {
                if (string.IsNullOrWhiteSpace(queryPart)) continue;

                string[] parts = queryPart.Split('=');

                var key = HttpUtility.UrlDecode(parts[0]);

                string value = null;
                if (parts.Length > 1)
                {
                    value = HttpUtility.UrlDecode(parts[1]);
                }

                dict[key] = value;
            }

            return dict;
        }

        public static IDictionary<string, string> GetQueryParameters(this Uri uri)
        {
            return ParseQueryString(uri.Query.TrimStart('?'));
        }

        public static bool TryGetUserInfo(this Uri uri, out string userName, out string password)
        {
            EnsureArgument.NotNull(uri, nameof(uri));
            userName = null;
            password = null;

            if (string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                return false;
            }

            /* According to RFC 3986 section 3.2.1 (https://www.rfc-editor.org/rfc/rfc3986#section-3.2.1)
             * the user information component of a URI should look like:
             *
             *     url-encode(username):url-encode(password)
             */
            string[] split = uri.UserInfo.Split(new[] {':'}, count: 2);

            if (split.Length > 0)
            {
                userName = WebUtility.UrlDecode(split[0]);
            }
            if (split.Length > 1)
            {
                password = WebUtility.UrlDecode(split[1]);
            }

            return split.Length > 0;
        }

        public static string GetUserName(this Uri uri)
        {
            return TryGetUserInfo(uri, out string userName, out _) ? userName : null;
        }

        public static Uri WithoutUserInfo(this Uri uri)
        {
            if (string.IsNullOrEmpty(uri.UserInfo))
            {
                return uri;
            }

            return new UriBuilder(uri) {UserName = string.Empty, Password = string.Empty}.Uri;
        }

        public static IEnumerable<string> GetGitConfigurationScopes(this Uri uri)
        {
            EnsureArgument.NotNull(uri, nameof(uri));

            string schemeAndDelim = $"{uri.Scheme}{Uri.SchemeDelimiter}";
            string host = uri.Host.TrimEnd('/');
            // If port is default, don't append
            string port = uri.IsDefaultPort ? "" : $":{uri.Port}";
            string path = uri.AbsolutePath.Trim('/');

            // Unfold the path by component, right-to-left
            while (!string.IsNullOrWhiteSpace(path))
            {
                yield return $"{schemeAndDelim}{host}{port}/{path}";

                // Trim off the last path component
                if (!TryTrimString(path, StringExtensions.TruncateFromLastIndexOf, '/', out path))
                {
                    break;
                }
            }

            // Check whether the URL only contains hostname.
            // This usually means the host is on your local network.
            if (!string.IsNullOrWhiteSpace(host) &&
                !host.Contains("."))
            {
                yield return $"{schemeAndDelim}{host}{port}";
                // If we have reached this point, there are no more subdomains to unfold, so exit early.
                yield break;
            }

            // Unfold the host by sub-domain, left-to-right
            while (!string.IsNullOrWhiteSpace(host))
            {
                if (host.Contains(".")) // Do not emit just the TLD
                {
                    yield return $"{schemeAndDelim}{host}{port}";
                }

                // Trim off the left-most sub-domain
                if (!TryTrimString(host, StringExtensions.TrimUntilIndexOf, '.', out host))
                {
                    break;
                }
            }
        }

        private static bool TryTrimString(string input, Func<string, char, string> func, char c, out string output)
        {
            output = func(input, c);
            return !StringComparer.Ordinal.Equals(input, output);
        }
    }
}
