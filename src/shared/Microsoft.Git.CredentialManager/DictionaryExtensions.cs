// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;

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
    }
}
