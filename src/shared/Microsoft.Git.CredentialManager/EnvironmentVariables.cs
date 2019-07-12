// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Simple wrapper dictionary around environment variables.
    /// </summary>
    public interface IEnvironmentVariables : IReadOnlyDictionary<string, string> { }

    public class EnvironmentVariables : IEnvironmentVariables
    {
        private readonly IReadOnlyDictionary<string, string> _envars;

        public EnvironmentVariables(IDictionary variables)
        {
            EnsureArgument.NotNull(variables, nameof(variables));

            // On Windows it is technically possible to get env vars which differ only by case
            // even though the general assumption is that they are case insensitive on Windows.
            // For example, some of the standard .NET types like System.Diagnostics.Process
            // will fail to start a process on Windows if given duplicate environment variables.
            // See this issue for more information: https://github.com/dotnet/corefx/issues/13146

            // If we're on the Windows platform we should de-duplicate by setting the string
            // comparer to OrdinalIgnoreCase.
            var comparer = PlatformUtils.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

            var dict = new Dictionary<string, string>(comparer);

            foreach (var key in variables.Keys)
            {
                if (key is string name && variables[key] is string value)
                {
                    dict[name] = value;
                }
            }

            _envars = dict;
        }

        #region IReadOnlyDictionary<string, string>

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _envars.GetEnumerator();
        public int Count => _envars.Count;
        public bool ContainsKey(string key) => _envars.ContainsKey(key);

        /// <summary>
        /// Try and get the value of an environment variable as a string.
        /// </summary>
        /// <param name="name">Environment variable name.</param>
        /// <param name="value">Environment variable value.</param>
        /// <returns>True if the value was found, false otherwise.</returns>
        public bool TryGetValue(string name, out string value) => _envars.TryGetValue(name, out value);

        public string this[string name] => _envars[name];
        public IEnumerable<string> Keys => _envars.Keys;
        public IEnumerable<string> Values => _envars.Values;

        #endregion
    }

    public static class EnvironmentVariablesExtensions
    {
        /// <summary>
        /// Get the value of an environment variable as a string.
        /// </summary>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">An environment variable with the specified name was not found.</exception>
        /// <param name="envars">Environment variables.</param>
        /// <param name="name">Environment variable name.</param>
        /// <returns>Environment variable value.</returns>
        public static string GetValue(this IEnvironmentVariables envars, string name)
        {
            if (envars.TryGetValue(name, out string value))
            {
                return value;
            }

            throw new KeyNotFoundException("An environment variable with the specified name was not found.");
        }

        /// <summary>
        /// Get the value of an environment variable as 'booleany' (either 'truthy' or 'falsey').
        /// </summary>
        /// <param name="envars">Environment variables.</param>
        /// <param name="name">Environment variable name.</param>
        /// <param name="defaultValue">Default value if the environment variable is not set, or was neither 'truthy' or 'falsey'.</param>
        /// <returns>Environment variable value.</returns>
        /// <remarks>
        /// 'Truthy' and 'fasley' is defined by the implementation of <see cref="StringExtensions.IsTruthy"/> and <see cref="StringExtensions.IsFalsey"/>.
        /// </remarks>
        public static bool GetBooleanyOrDefault(this IEnvironmentVariables envars, string name, bool defaultValue)
        {
            if (envars.TryGetValue(name, out string value))
            {
                return value.ToBooleanyOrDefault(defaultValue);
            }

            return defaultValue;
        }
    }
}
