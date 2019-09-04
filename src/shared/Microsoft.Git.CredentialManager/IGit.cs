// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager
{
    public interface IGitConfiguration : IDisposable
    {
        /// <summary>
        /// Enumerate all configuration entries invoking the specified callback for each entry.
        /// </summary>
        /// <param name="cb">Callback to invoke for each configuration entry.</param>
        void Enumerate(GitConfigurationEnumerationCallback cb);

        /// <summary>
        /// Get a snapshot of the configuration filtered to the specified level.
        /// </summary>
        /// <param name="level">Configuration level filter.</param>
        /// <returns>Git configuration snapshot.</returns>
        IGitConfiguration GetFilteredConfiguration(GitConfigurationLevel level);

        /// <summary>
        /// Try and get the value of a configuration entry as a string.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="value">Configuration entry value.</param>
        /// <returns>True if the value was found, false otherwise.</returns>
        bool TryGetValue(string name, out string value);

        /// <summary>
        /// Set the value of a configuration entry.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="value">Configuration entry value.</param>
        void SetValue(string name, string value);

        /// <summary>
        /// Deletes a configuration entry from the highest level.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        void DeleteEntry(string name);

        /// <summary>
        /// Get all values of a multivar configuration entry.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="regexp">Regular expression to filter which variables we're interested in. Use null to indicate all.</param>
        /// <returns>All values of the multivar configuration entry.</returns>
        IEnumerable<string> GetMultivarValue(string name, string regexp);

        /// <summary>
        /// Set a multivar configuration entry value.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="regexp">Regular expression to indicate which values to replace.</param>
        /// <param name="value">Configuration entry value.</param>
        /// <remarks>If the regular expression does not match any existing entry, a new entry is created.</remarks>
        void SetMultivarValue(string name, string regexp, string value);

        /// <summary>
        /// Deletes one or several entries from a multivar.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="regexp">Regular expression to indicate which values to delete.</param>
        void DeleteMultivarEntry(string name, string regexp);
    }

    /// <summary>
    /// Invoked for each Git configuration entry during an enumeration (<see cref="IGitConfiguration.Enumerate"/>).
    /// </summary>
    /// <param name="name">Name of the current configuration entry.</param>
    /// <param name="value">Value of the current configuration entry.</param>
    /// <returns>True to continue enumeration, false to stop enumeration.</returns>
    public delegate bool GitConfigurationEnumerationCallback(string name, string value);

    public interface IGit : IDisposable
    {
        /// <summary>
        /// Get a snapshot of the configuration for the system, user, and optionally a specified repository.
        /// </summary>
        /// <param name="repositoryPath">Optional repository path from which to load local configuration.</param>
        /// <returns>Git configuration snapshot.</returns>
        IGitConfiguration GetConfiguration(string repositoryPath);

        /// <summary>
        /// Resolve the given path to a containing repository, or null if the path is not inside a Git repository.
        /// </summary>
        /// <param name="path">Path to resolve.</param>
        /// <returns>Git repository root path, or null if <paramref name="path"/> is not inside of a Git repository.</returns>
        string GetRepositoryPath(string path);
    }

    public enum GitConfigurationLevel
    {
        ProgramData,
        System,
        Xdg,
        Global,
        Local,
    }

    public static class GitExtensions
    {
        /// <summary>
        /// Get a snapshot of the configuration for the system and user.
        /// </summary>
        /// <param name="git">Git object.</param>
        /// <returns>Git configuration snapshot.</returns>
        public static IGitConfiguration GetConfiguration(this IGit git) => git.GetConfiguration(null);

        /// <summary>
        /// Get the value of a configuration entry as a string.
        /// </summary>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">A configuration entry with the specified key was not found.</exception>
        /// <param name="config">Configuration object.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <returns>Configuration entry value.</returns>
        public static string GetValue(this IGitConfiguration config, string name)
        {
            if (!config.TryGetValue(name, out string value))
            {
                throw new KeyNotFoundException($"Git configuration entry with the name '{name}' was not found.");
            }

            return value;
        }

        /// <summary>
        /// Get the value of a configuration entry as a string.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="section">Configuration section name.</param>
        /// <param name="property">Configuration property name.</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">A configuration entry with the specified key was not found.</exception>
        /// <returns>Configuration entry value.</returns>
        public static string GetValue(this IGitConfiguration config, string section, string property)
        {
            return GetValue(config, $"{section}.{property}");
        }

        /// <summary>
        /// Get the value of a scoped configuration entry as a string.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="section">Configuration section name.</param>
        /// <param name="scope">Configuration section scope.</param>
        /// <param name="property">Configuration property name.</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">A configuration entry with the specified key was not found.</exception>
        /// <returns>Configuration entry value.</returns>
        public static string GetValue(this IGitConfiguration config, string section, string scope, string property)
        {
            if (scope is null)
            {
                return GetValue(config, section, property);
            }

            return GetValue(config, $"{section}.{scope}.{property}");
        }

        /// <summary>
        /// Try and get the value of a configuration entry as a string.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="section">Configuration section name.</param>
        /// <param name="property">Configuration property name.</param>
        /// <param name="value">Configuration entry value.</param>
        /// <returns>True if the value was found, false otherwise.</returns>
        public static bool TryGetValue(this IGitConfiguration config, string section, string property, out string value)
        {
            return config.TryGetValue($"{section}.{property}", out value);
        }

        /// <summary>
        /// Try and get the value of a configuration entry as a string.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="section">Configuration section name.</param>
        /// <param name="scope">Configuration section scope.</param>
        /// <param name="property">Configuration property name.</param>
        /// <param name="value">Configuration entry value.</param>
        /// <returns>True if the value was found, false otherwise.</returns>
        public static bool TryGetValue(this IGitConfiguration config, string section, string scope, string property, out string value)
        {
            if (scope is null)
            {
                return TryGetValue(config, section, property, out value);
            }

            return config.TryGetValue($"{section}.{scope}.{property}", out value);
        }
    }
}
