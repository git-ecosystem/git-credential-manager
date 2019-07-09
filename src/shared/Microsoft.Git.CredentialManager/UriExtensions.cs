// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager
{
    public static class UriExtensions
    {
        public static IEnumerable<string> GetGitConfigurationScopes(this Uri uri)
        {
            EnsureArgument.NotNull(uri, nameof(uri));

            string schemeAndDelim = $"{uri.Scheme}{Uri.SchemeDelimiter}";
            string host = uri.Host.TrimEnd('/');
            string path = uri.AbsolutePath.Trim('/');

            // Unfold the path by component, right-to-left
            while (!string.IsNullOrWhiteSpace(path))
            {
                yield return $"{schemeAndDelim}{host}/{path}";

                // Trim off the last path component
                if (!TryTrimString(path, StringExtensions.TruncateFromLastIndexOf, '/', out path))
                {
                    break;
                }
            }

            // Unfold the host by sub-domain, left-to-right
            while (!string.IsNullOrWhiteSpace(host))
            {
                if (host.Contains(".")) // Do not emit just the TLD
                {
                    yield return $"{schemeAndDelim}{host}";
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
