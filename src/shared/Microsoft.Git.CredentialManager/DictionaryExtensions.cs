// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Microsoft.Git.CredentialManager
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Get the value of a dictionary entry as 'booleany' (either 'truthy' or 'falsey').
        /// </summary>
        /// <param name="dict">Dictionary.</param>
        /// <param name="key">Dictionary entry key.</param>
        /// <param name="defaultValue">Default value if the key is not present, or was neither 'truthy' or 'falsey'.</param>
        /// <returns>Dictionary entry value.</returns>
        /// <remarks>
        /// 'Truthy' and 'fasley' is defined by the implementation of <see cref="StringExtensions.IsTruthy"/> and <see cref="StringExtensions.IsFalsey"/>.
        /// </remarks>
        public static bool GetBooleanyOrDefault(this IReadOnlyDictionary<string, string> dict, string key, bool defaultValue)
        {
            if (dict.TryGetValue(key, out string value))
            {
                return value.ToBooleanyOrDefault(defaultValue);
            }

            return defaultValue;
        }

        public static string ToQueryString(this IDictionary<string, string> dict)
        {
            var sb = new StringBuilder();
            int i = 0;

            foreach (var kvp in dict)
            {
                string key  = HttpUtility.UrlEncode(kvp.Key);
                string value = HttpUtility.UrlEncode(kvp.Value);

                if (i > 0)
                {
                    sb.Append('&');
                }

                sb.AppendFormat("{0}={1}", key, value);

                i++;
            }

            return sb.ToString();
        }
    }
}
