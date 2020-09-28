// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Invoked for each Git configuration entry during an enumeration (<see cref="IGitConfiguration.Enumerate"/>).
    /// </summary>
    /// <param name="name">Name of the current configuration entry.</param>
    /// <param name="value">Value of the current configuration entry.</param>
    /// <returns>True to continue enumeration, false to stop enumeration.</returns>
    public delegate bool GitConfigurationEnumerationCallback(string name, string value);

    public enum GitConfigurationLevel
    {
        All,
        ProgramData,
        System,
        Xdg,
        Global,
        Local,
    }

    public interface IGitConfiguration
    {
        /// <summary>
        /// Enumerate all configuration entries invoking the specified callback for each entry.
        /// </summary>
        /// <param name="cb">Callback to invoke for each configuration entry.</param>
        void Enumerate(GitConfigurationEnumerationCallback cb);

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
        void Unset(string name);

        /// <summary>
        /// Get all values of a multivar configuration entry.
        /// </summary>
        /// <param name="nameRegex">Configuration entry name regular expression.</param>
        /// <param name="valueRegex">Regular expression to filter which variables we're interested in. Use null to indicate all.</param>
        /// <returns>All values of the multivar configuration entry.</returns>
        IEnumerable<string> GetRegex(string nameRegex, string valueRegex);

        /// <summary>
        /// Set a multivar configuration entry value.
        /// </summary>
        /// <param name="nameRegex">Configuration entry name regular expression.</param>
        /// <param name="valueRegex">Regular expression to indicate which values to replace.</param>
        /// <param name="value">Configuration entry value.</param>
        /// <remarks>If the regular expression does not match any existing entry, a new entry is created.</remarks>
        void ReplaceAll(string nameRegex, string valueRegex, string value);

        /// <summary>
        /// Deletes one or several entries from a multivar.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="valueRegex">Regular expression to indicate which values to delete.</param>
        void UnsetAll(string name, string valueRegex);
    }

    public class GitProcessConfiguration : IGitConfiguration
    {
        private readonly ITrace _trace;
        private readonly GitProcess _git;
        private readonly GitConfigurationLevel? _filterLevel;

        internal GitProcessConfiguration(ITrace trace, GitProcess git, GitConfigurationLevel filterLevel = GitConfigurationLevel.All)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(git, nameof(git));

            _trace = trace;
            _git = git;
            _filterLevel = filterLevel;
        }

        public void Enumerate(GitConfigurationEnumerationCallback cb)
        {
            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config --null {level} --list"))
            {
                git.Start();
                // To avoid deadlocks, always read the output stream first and then wait
                // TODO: don't read in all the data at once; stream it
                string data = git.StandardOutput.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        throw new Exception(
                            $"Failed to enumerate all Git configuration entries. Exit code '{git.ExitCode}' (level={_filterLevel})");
                }

                IEnumerable<string> entries = data.Split('\0').Where(x => !string.IsNullOrWhiteSpace(x));
                foreach (string entry in entries)
                {
                    string[] kvp = entry.Split(new[]{'\n'}, count: 2);

                    if (kvp.Length == 2 && !cb(kvp[0], kvp[1]))
                    {
                        break;
                    }
                }
            }
        }

        public bool TryGetValue(string name, out string value)
        {
            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config {level} {name}"))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    case 1: // Not found
                        value = null;
                        return false;
                    default: // Error
                        _trace.WriteLine(
                            $"Failed to read Git configuration entry '{name}'. Exit code '{git.ExitCode}' (level={_filterLevel})");
                        value = null;
                        return false;
                }

                string data = git.StandardOutput.ReadToEnd().TrimEnd('\n');

                if (string.IsNullOrWhiteSpace(data))
                {
                    value = null;
                    return false;
                }

                value = data;
                return true;
            }
        }

        public void SetValue(string name, string value)
        {
            if (_filterLevel == GitConfigurationLevel.All)
            {
                throw new InvalidOperationException("Must have a specific configuration level filter to modify values.");
            }

            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config {level} {name} \"{value}\""))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        throw new Exception(
                            $"Failed to set Git configuration entry '{name}' to '{value}'. Exit code '{git.ExitCode}' (level={_filterLevel})");
                }
            }
        }

        public void Unset(string name)
        {
            if (_filterLevel == GitConfigurationLevel.All)
            {
                throw new InvalidOperationException("Must have a specific configuration level filter to modify values.");
            }

            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config {level} --unset {name}"))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        throw new Exception(
                           $"Failed to unset Git configuration entry '{name}'. Exit code '{git.ExitCode}' (level={_filterLevel})");
                }
            }
        }

        public IEnumerable<string> GetRegex(string nameRegex, string valueRegex)
        {
            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config --null {level} --get-regex {nameRegex} {valueRegex}"))
            {
                git.Start();
                // To avoid deadlocks, always read the output stream first and then wait
                // TODO: don't read in all the data at once; stream it
                string data = git.StandardOutput.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                    case 1: // No results
                        break;
                    default:
                        throw new Exception(
                            $"Failed to get Git configuration multi-valued entry '{nameRegex}' with value regex '{valueRegex}'. Exit code '{git.ExitCode}' (level={_filterLevel})");
                }

                string[] entries = data.Split('\0');
                foreach (string entry in entries)
                {
                    string[] kvp = entry.Split(new[]{'\n'}, count: 2);

                    if (kvp.Length == 2) {
                        yield return kvp[1];
                    }
                }
            }
        }

        public void ReplaceAll(string name, string valueRegex, string value)
        {
            if (_filterLevel == GitConfigurationLevel.All)
            {
                throw new InvalidOperationException("Must have a specific configuration level filter to modify values.");
            }

            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config {level} --replace-all {name} {value} {valueRegex}"))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        throw new Exception(
                            $"Failed to set Git configuration multi-valued entry '{name}' with value regex '{valueRegex}' to value '{value}'. Exit code '{git.ExitCode}' (level={_filterLevel})");
                }
            }
        }

        public void UnsetAll(string name, string valueRegex)
        {
            if (_filterLevel == GitConfigurationLevel.All)
            {
                throw new InvalidOperationException("Must have a specific configuration level filter to modify values.");
            }

            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config {level} --unset-all {name} {valueRegex}"))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                    case 5: // Trying to unset a value that does not exist
                        break;
                    default:
                        throw new Exception(
                            $"Failed to unset all Git configuration multi-valued entries '{name}' with value regex '{valueRegex}'. Exit code '{git.ExitCode}' (level={_filterLevel})");
                }
            }
        }

        private string GetLevelFilterArg()
        {
            switch (_filterLevel)
            {
                case GitConfigurationLevel.ProgramData:
                case GitConfigurationLevel.Xdg:
                    return null;
                case GitConfigurationLevel.System:
                    return "--system";
                case GitConfigurationLevel.Global:
                    return "--global";
                case GitConfigurationLevel.Local:
                    return "--local";
                default:
                    return null;
            }
        }
    }

    public static class GitConfigurationExtensions
    {
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
