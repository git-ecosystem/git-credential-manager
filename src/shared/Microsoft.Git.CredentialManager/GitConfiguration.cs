// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
        bool TryGet(string name, out string value);

        /// <summary>
        /// Set the value of a configuration entry.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="value">Configuration entry value.</param>
        void Set(string name, string value);

        /// <summary>
        /// Add a new value for a configuration entry.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="value">Configuration entry value.</param>
        void Add(string name, string value);

        /// <summary>
        /// Deletes a configuration entry from the highest level.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        void Unset(string name);

        /// <summary>
        /// Get all value of a multivar configuration entry.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <returns>All values of the multivar configuration entry.</returns>
        IEnumerable<string> GetAll(string name);

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
                        _trace.WriteLine($"Failed to enumerate config entries (exit={git.ExitCode}, level={_filterLevel})");
                        throw CreateGitException(git, "Failed to enumerate all Git configuration entries");
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

        public bool TryGet(string name, out string value)
        {
            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config --null {level} {QuoteCmdArg(name)}"))
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
                        _trace.WriteLine($"Failed to read Git configuration entry '{name}'. (exit={git.ExitCode}, level={_filterLevel})");
                        value = null;
                        return false;
                }

                string data = git.StandardOutput.ReadToEnd();
                string[] entries = data.Split('\0');
                if (entries.Length > 0)
                {
                    value = entries[0];
                    return true;
                }

                value = null;
                return false;
            }
        }

        public void Set(string name, string value)
        {
            if (_filterLevel == GitConfigurationLevel.All)
            {
                throw new InvalidOperationException("Must have a specific configuration level filter to modify values.");
            }

            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config {level} {QuoteCmdArg(name)} {QuoteCmdArg(value)}"))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        _trace.WriteLine($"Failed to set config entry '{name}' to value '{value}' (exit={git.ExitCode}, level={_filterLevel})");
                        throw CreateGitException(git, $"Failed to set Git configuration entry '{name}'");
                }
            }
        }

        public void Add(string name, string value)
        {
            if (_filterLevel == GitConfigurationLevel.All)
            {
                throw new InvalidOperationException("Must have a specific configuration level filter to add values.");
            }

            string level = GetLevelFilterArg();
            using (Process git = _git.CreateProcess($"config {level} --add {QuoteCmdArg(name)} {QuoteCmdArg(value)}"))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        _trace.WriteLine($"Failed to add config entry '{name}' with value '{value}' (exit={git.ExitCode}, level={_filterLevel})");
                        throw CreateGitException(git, $"Failed to add Git configuration entry '{name}'");
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
            using (Process git = _git.CreateProcess($"config {level} --unset {QuoteCmdArg(name)}"))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                    case 5: // Trying to unset a value that does not exist
                        break;
                    default:
                        _trace.WriteLine($"Failed to unset config entry '{name}' (exit={git.ExitCode}, level={_filterLevel})");
                        throw CreateGitException(git, $"Failed to unset Git configuration entry '{name}'");
                }
            }
        }

        public IEnumerable<string> GetAll(string name)
        {
            string level = GetLevelFilterArg();

            var gitArgs = $"config --null {level} --get-all {QuoteCmdArg(name)}";

            using (Process git = _git.CreateProcess(gitArgs))
            {
                git.Start();

                // TODO: don't read in all the data at once; stream it
                string data = git.StandardOutput.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        string[] entries = data.Split('\0');

                        // Because each line terminates with the \0 character, splitting leaves us with one
                        // bogus blank entry at the end of the array which we should ignore
                        for (var i = 0; i < entries.Length - 1; i++)
                        {
                            yield return entries[i];
                        }
                        break;

                    case 1: // No results
                        break;

                    default:
                        _trace.WriteLine($"Failed to get all config entries '{name}' (exit={git.ExitCode}, level={_filterLevel})");
                        throw CreateGitException(git, $"Failed to get all Git configuration entries '{name}'");
                }
            }
        }

        public IEnumerable<string> GetRegex(string nameRegex, string valueRegex)
        {
            string level = GetLevelFilterArg();

            var gitArgs = $"config --null {level} --get-regex {QuoteCmdArg(nameRegex)}";
            if (valueRegex != null)
            {
                gitArgs += $" {QuoteCmdArg(valueRegex)}";
            }

            using (Process git = _git.CreateProcess(gitArgs))
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
                        _trace.WriteLine($"Failed to get all multivar regex '{nameRegex}' and value regex '{valueRegex}' (exit={git.ExitCode}, level={_filterLevel})");
                        throw CreateGitException(git, $"Failed to get Git configuration multi-valued entries with name regex '{nameRegex}'");
                }

                string[] entries = data.Split('\0');
                foreach (string entry in entries)
                {
                    string[] kvp = entry.Split(new[]{'\n'}, count: 2);

                    if (kvp.Length == 2)
                    {
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
            var gitArgs = $"config {level} --replace-all {QuoteCmdArg(name)} {QuoteCmdArg(value)}";
            if (valueRegex != null)
            {
                gitArgs += $" {QuoteCmdArg(valueRegex)}";
            }

            using (Process git = _git.CreateProcess(gitArgs))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        _trace.WriteLine($"Failed to replace all multivar '{name}' and value regex '{valueRegex}' with new value '{value}' (exit={git.ExitCode}, level={_filterLevel})");
                        throw CreateGitException(git, $"Failed to replace all Git configuration multi-valued entries '{name}'");
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
            var gitArgs = $"config {level} --unset-all {QuoteCmdArg(name)}";
            if (valueRegex != null)
            {
                gitArgs += $" {QuoteCmdArg(valueRegex)}";
            }

            using (Process git = _git.CreateProcess(gitArgs))
            {
                git.Start();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                    case 5: // Trying to unset a value that does not exist
                        break;
                    default:
                        _trace.WriteLine($"Failed to unset all multivar '{name}' with value regex '{valueRegex}' (exit={git.ExitCode}, level={_filterLevel})");
                        throw CreateGitException(git, $"Failed to unset all Git configuration multi-valued entries '{name}'");
                }
            }
        }

        private Exception CreateGitException(Process git, string message)
        {
            var exceptionMessage = new StringBuilder();
            string gitMessage = git.StandardError.ReadToEnd();

            if (!string.IsNullOrWhiteSpace(gitMessage))
            {
                exceptionMessage.AppendLine(gitMessage);
            }

            exceptionMessage.AppendLine(message);
            exceptionMessage.AppendLine($"Exit code: '{git.ExitCode}'");
            exceptionMessage.AppendLine($"Configuration level: {_filterLevel}");

            throw new Exception(exceptionMessage.ToString());
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

        public static string QuoteCmdArg(string str)
        {
            bool needsQuotes = string.IsNullOrEmpty(str);
            var result = new StringBuilder();

            for (int i = 0; i < (str?.Length ?? 0); i++)
            {
                switch (str![i])
                {
                    case '"':
                        result.Append("\\\"");
                        needsQuotes = true;
                        break;

                    case ' ':
                    case '{':
                    case '*':
                    case '?':
                    case '\r':
                    case '\n':
                    case '\t':
                    case '\'':
                        result.Append(str[i]);
                        needsQuotes = true;
                        break;

                    case '\\':
                        int end = i;

                        // Copy all the '\'s in this run.
                        while (end < str.Length && str[end] == '\\')
                        {
                            result.Append('\\');
                            end++;
                        }

                        // If we ended the run of '\'s with a '"' then we need to double up the number of '\'s.
                        // The '"' will be escaped on the next pass of the loop.
                        // Also if we have reached the end of the string, and we need to book-end the result
                        // with double quotes ('"') we should escape all the '\'s to prevent ending on an
                        // escaped '"' in the result.
                        if (end < str.Length && str[end] == '"' ||
                            end == str.Length && needsQuotes)
                        {
                            result.Append('\\', end - i);
                        }

                        // Back-off one character
                        if (end > i)
                        {
                            end--;
                        }

                        i = end;
                        break;

                    default:
                        result.Append(str[i]);
                        break;
                }
            }

            if (needsQuotes)
            {
                result.Insert(0, '"');
                result.Append('"');
            }

            return result.ToString();
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
        public static string Get(this IGitConfiguration config, string name)
        {
            if (!config.TryGet(name, out string value))
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
        public static string Get(this IGitConfiguration config, string section, string property)
        {
            return Get(config, $"{section}.{property}");
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
        public static string Get(this IGitConfiguration config, string section, string scope, string property)
        {
            if (scope is null)
            {
                return Get(config, section, property);
            }

            return Get(config, $"{section}.{scope}.{property}");
        }

        /// <summary>
        /// Try and get the value of a configuration entry as a string.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="section">Configuration section name.</param>
        /// <param name="property">Configuration property name.</param>
        /// <param name="value">Configuration entry value.</param>
        /// <returns>True if the value was found, false otherwise.</returns>
        public static bool TryGet(this IGitConfiguration config, string section, string property, out string value)
        {
            return config.TryGet($"{section}.{property}", out value);
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
        public static bool TryGet(this IGitConfiguration config, string section, string scope, string property, out string value)
        {
            if (scope is null)
            {
                return TryGet(config, section, property, out value);
            }

            return config.TryGet($"{section}.{scope}.{property}", out value);
        }
    }
}
