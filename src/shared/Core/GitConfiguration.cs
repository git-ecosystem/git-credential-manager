using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GitCredentialManager
{
    /// <summary>
    /// Invoked for each Git configuration entry during an enumeration (<see cref="IGitConfiguration.Enumerate"/>).
    /// </summary>
    /// <param name="entry">Current configuration entry.</param>
    /// <returns>True to continue enumeration, false to stop enumeration.</returns>
    public delegate bool GitConfigurationEnumerationCallback(GitConfigurationEntry entry);

    public enum GitConfigurationLevel
    {
        All,
        System,
        Global,
        Local,
        Unknown,
    }

    public enum GitConfigurationType
    {
        Raw,
        Bool,
        Path
    }

    public interface IGitConfiguration
    {
        /// <summary>
        /// Enumerate all configuration entries invoking the specified callback for each entry.
        /// </summary>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="cb">Callback to invoke for each configuration entry.</param>
        void Enumerate(GitConfigurationLevel level, GitConfigurationEnumerationCallback cb);

        /// <summary>
        /// Try and get the value of a configuration entry as a string.
        /// </summary>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="type">Type constraint to which the config value should be canonicalized.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="value">Configuration entry value.</param>
        /// <returns>True if the value was found, false otherwise.</returns>
        bool TryGet(GitConfigurationLevel level, GitConfigurationType type, string name, out string value);

        /// <summary>
        /// Set the value of a configuration entry.
        /// </summary>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="value">Configuration entry value.</param>
        void Set(GitConfigurationLevel level, string name, string value);

        /// <summary>
        /// Add a new value for a configuration entry.
        /// </summary>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="value">Configuration entry value.</param>
        void Add(GitConfigurationLevel level, string name, string value);

        /// <summary>
        /// Deletes a configuration entry from the highest level.
        /// </summary>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="name">Configuration entry name.</param>
        void Unset(GitConfigurationLevel level, string name);

        /// <summary>
        /// Get all value of a multivar configuration entry.
        /// </summary>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="type">Type constraint to which the config values should be canonicalized.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <returns>All values of the multivar configuration entry.</returns>
        IEnumerable<string> GetAll(GitConfigurationLevel level, GitConfigurationType type, string name);

        /// <summary>
        /// Get all values of a multivar configuration entry.
        /// </summary>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="type">Type constraint to which the config values should be canonicalized.</param>
        /// <param name="nameRegex">Configuration entry name regular expression.</param>
        /// <param name="valueRegex">Regular expression to filter which variables we're interested in. Use null to indicate all.</param>
        /// <returns>All values of the multivar configuration entry.</returns>
        IEnumerable<string> GetRegex(GitConfigurationLevel level, GitConfigurationType type, string nameRegex, string valueRegex);

        /// <summary>
        /// Set a multivar configuration entry value.
        /// </summary>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="nameRegex">Configuration entry name regular expression.</param>
        /// <param name="valueRegex">Regular expression to indicate which values to replace.</param>
        /// <param name="value">Configuration entry value.</param>
        /// <remarks>If the regular expression does not match any existing entry, a new entry is created.</remarks>
        void ReplaceAll(GitConfigurationLevel level, string nameRegex, string valueRegex, string value);

        /// <summary>
        /// Deletes one or several entries from a multivar.
        /// </summary>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="valueRegex">Regular expression to indicate which values to delete.</param>
        void UnsetAll(GitConfigurationLevel level, string name, string valueRegex);
    }

    public class GitProcessConfiguration : IGitConfiguration
    {
        private static readonly GitVersion TypeConfigMinVersion = new GitVersion(2, 18, 0);

        private readonly ITrace _trace;
        private readonly GitProcess _git;

        internal GitProcessConfiguration(ITrace trace, GitProcess git)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(git, nameof(git));

            _trace = trace;
            _git = git;
        }

        public void Enumerate(GitConfigurationLevel level, GitConfigurationEnumerationCallback cb)
        {
            string levelArg = GetLevelFilterArg(level);
            using (ChildProcess git = _git.CreateProcess($"config --null {levelArg} --list"))
            {
                git.Start(Trace2ProcessClass.Git);
                // To avoid deadlocks, always read the output stream first and then wait
                // TODO: don't read in all the data at once; stream it
                string data = git.StandardOutput.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        _trace.WriteLine($"Failed to enumerate config entries (exit={git.ExitCode}, level={level})");
                        throw GitProcess.CreateGitException(git, "Failed to enumerate all Git configuration entries");
                }

                var name  = new StringBuilder();
                var value = new StringBuilder();
                int i = 0;
                while (i < data.Length)
                {
                    name.Clear();
                    value.Clear();

                    // Read key name (LF terminated)
                    while (i < data.Length && data[i] != '\n')
                    {
                        name.Append(data[i++]);
                    }

                    if (i >= data.Length)
                    {
                        _trace.WriteLine("Invalid Git configuration output. Expected newline terminator (\\n) after key.");
                        break;
                    }

                    // Skip the LF terminator
                    i++;

                    // Read value (null terminated)
                    while (i < data.Length && data[i] != '\0')
                    {
                        value.Append(data[i++]);
                    }

                    if (i >= data.Length)
                    {
                        _trace.WriteLine("Invalid Git configuration output. Expected null terminator (\\0) after value.");
                        break;
                    }

                    // Skip the null terminator
                    i++;

                    var entry = new GitConfigurationEntry(name.ToString(), value.ToString());

                    if (!cb(entry))
                    {
                        break;
                    }
                }
            }
        }

        public bool TryGet(GitConfigurationLevel level, GitConfigurationType type, string name, out string value)
        {
            string levelArg = GetLevelFilterArg(level);
            string typeArg = GetCanonicalizeTypeArg(type);
            using (ChildProcess git = _git.CreateProcess($"config --null {levelArg} {typeArg} {QuoteCmdArg(name)}"))
            {
                git.Start(Trace2ProcessClass.Git);
                // To avoid deadlocks, always read the output stream first and then wait
                // TODO: don't read in all the data at once; stream it
                string data = git.StandardOutput.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    case 1: // Not found
                        value = null;
                        return false;
                    default: // Error
                        _trace.WriteLine($"Failed to read Git configuration entry '{name}'. (exit={git.ExitCode}, level={level})");
                        value = null;
                        return false;
                }

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

        public void Set(GitConfigurationLevel level, string name, string value)
        {
            EnsureSpecificLevel(level);

            string levelArg = GetLevelFilterArg(level);
            using (ChildProcess git = _git.CreateProcess($"config {levelArg} {QuoteCmdArg(name)} {QuoteCmdArg(value)}"))
            {
                git.Start(Trace2ProcessClass.Git);
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        _trace.WriteLine($"Failed to set config entry '{name}' to value '{value}' (exit={git.ExitCode}, level={level})");
                        throw GitProcess.CreateGitException(git, $"Failed to set Git configuration entry '{name}'");
                }
            }
        }

        public void Add(GitConfigurationLevel level, string name, string value)
        {
            EnsureSpecificLevel(level);

            string levelArg = GetLevelFilterArg(level);
            using (ChildProcess git = _git.CreateProcess($"config {levelArg} --add {QuoteCmdArg(name)} {QuoteCmdArg(value)}"))
            {
                git.Start(Trace2ProcessClass.Git);
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        _trace.WriteLine($"Failed to add config entry '{name}' with value '{value}' (exit={git.ExitCode}, level={level})");
                        throw GitProcess.CreateGitException(git, $"Failed to add Git configuration entry '{name}'");
                }
            }
        }

        public void Unset(GitConfigurationLevel level, string name)
        {
            EnsureSpecificLevel(level);

            string levelArg = GetLevelFilterArg(level);
            using (ChildProcess git = _git.CreateProcess($"config {levelArg} --unset {QuoteCmdArg(name)}"))
            {
                git.Start(Trace2ProcessClass.Git);
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                    case 5: // Trying to unset a value that does not exist
                        break;
                    default:
                        _trace.WriteLine($"Failed to unset config entry '{name}' (exit={git.ExitCode}, level={level})");
                        throw GitProcess.CreateGitException(git, $"Failed to unset Git configuration entry '{name}'");
                }
            }
        }

        public IEnumerable<string> GetAll(GitConfigurationLevel level, GitConfigurationType type, string name)
        {
            string levelArg = GetLevelFilterArg(level);
            string typeArg = GetCanonicalizeTypeArg(type);

            var gitArgs = $"config --null {levelArg} {typeArg} --get-all {QuoteCmdArg(name)}";

            using (ChildProcess git = _git.CreateProcess(gitArgs))
            {
                git.Start(Trace2ProcessClass.Git);
                // To avoid deadlocks, always read the output stream first and then wait
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
                        _trace.WriteLine($"Failed to get all config entries '{name}' (exit={git.ExitCode}, level={level})");
                        throw GitProcess.CreateGitException(git, $"Failed to get all Git configuration entries '{name}'");
                }
            }
        }

        public IEnumerable<string> GetRegex(GitConfigurationLevel level, GitConfigurationType type, string nameRegex, string valueRegex)
        {
            string levelArg = GetLevelFilterArg(level);
            string typeArg = GetCanonicalizeTypeArg(type);

            var gitArgs = $"config --null {levelArg} {typeArg} --get-regex {QuoteCmdArg(nameRegex)}";
            if (valueRegex != null)
            {
                gitArgs += $" {QuoteCmdArg(valueRegex)}";
            }

            using (ChildProcess git = _git.CreateProcess(gitArgs))
            {
                git.Start(Trace2ProcessClass.Git);
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
                        _trace.WriteLine($"Failed to get all multivar regex '{nameRegex}' and value regex '{valueRegex}' (exit={git.ExitCode}, level={level})");
                        throw GitProcess.CreateGitException(git, $"Failed to get Git configuration multi-valued entries with name regex '{nameRegex}'");
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

        public void ReplaceAll(GitConfigurationLevel level, string name, string valueRegex, string value)
        {
            EnsureSpecificLevel(level);

            string levelArg = GetLevelFilterArg(level);
            var gitArgs = $"config {levelArg} --replace-all {QuoteCmdArg(name)} {QuoteCmdArg(value)}";
            if (valueRegex != null)
            {
                gitArgs += $" {QuoteCmdArg(valueRegex)}";
            }

            using (ChildProcess git = _git.CreateProcess(gitArgs))
            {
                git.Start(Trace2ProcessClass.Git);
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    default:
                        _trace.WriteLine($"Failed to replace all multivar '{name}' and value regex '{valueRegex}' with new value '{value}' (exit={git.ExitCode}, level={level})");
                        throw GitProcess.CreateGitException(git, $"Failed to replace all Git configuration multi-valued entries '{name}'");
                }
            }
        }

        public void UnsetAll(GitConfigurationLevel level, string name, string valueRegex)
        {
            EnsureSpecificLevel(level);

            string levelArg = GetLevelFilterArg(level);
            var gitArgs = $"config {levelArg} --unset-all {QuoteCmdArg(name)}";
            if (valueRegex != null)
            {
                gitArgs += $" {QuoteCmdArg(valueRegex)}";
            }

            using (ChildProcess git = _git.CreateProcess(gitArgs))
            {
                git.Start(Trace2ProcessClass.Git);
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                    case 5: // Trying to unset a value that does not exist
                        break;
                    default:
                        _trace.WriteLine($"Failed to unset all multivar '{name}' with value regex '{valueRegex}' (exit={git.ExitCode}, level={level})");
                        throw GitProcess.CreateGitException(git, $"Failed to unset all Git configuration multi-valued entries '{name}'");
                }
            }
        }

        private static void EnsureSpecificLevel(GitConfigurationLevel level)
        {
            if (level == GitConfigurationLevel.All)
            {
                throw new InvalidOperationException("Must have a specific configuration level filter to modify values.");
            }
        }

        private static string GetLevelFilterArg(GitConfigurationLevel level)
        {
            switch (level)
            {
                case GitConfigurationLevel.System:
                    return "--system";
                case GitConfigurationLevel.Global:
                    return "--global";
                case GitConfigurationLevel.Local:
                    return "--local";
                case GitConfigurationLevel.Unknown:
                default:
                    return null;
            }
        }

        private string GetCanonicalizeTypeArg(GitConfigurationType type)
        {
            if (_git.Version >= TypeConfigMinVersion)
            {
                return type switch
                {
                    GitConfigurationType.Bool   => "--type=bool",
                    GitConfigurationType.Path   => "--type=path",
                    _                           => null
                };
            }
            else
            {
                return type switch
                {
                    GitConfigurationType.Bool   => "--bool",
                    GitConfigurationType.Path   => "--path",
                    _                           => null
                };
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
        /// Enumerate all configuration entries invoking the specified callback for each entry.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="cb">Callback to invoke for each matching configuration entry.</param>
        public static void Enumerate(this IGitConfiguration config, GitConfigurationEnumerationCallback cb)
        {
            config.Enumerate(GitConfigurationLevel.All, cb);
        }

        /// <summary>
        /// Enumerate all configuration entries invoking the specified callback for each entry.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="section">Optional section name to filter; use null for any.</param>
        /// <param name="property">Optional property name to filter; use null for any.</param>
        /// <param name="cb">Callback to invoke for each matching configuration entry.</param>
        public static void Enumerate(this IGitConfiguration config,
            GitConfigurationLevel level, string section, string property, GitConfigurationEnumerationCallback cb)
        {
            config.Enumerate(level, entry =>
            {
                if (GitConfigurationKeyComparer.TrySplit(entry.Key, out string entrySection, out _, out string entryProperty) &&
                    (section  is null || GitConfigurationKeyComparer.SectionComparer.Equals(section, entrySection)) &&
                    (property is null || GitConfigurationKeyComparer.PropertyComparer.Equals(property, entryProperty)))
                {
                    return cb(entry);
                }

                return true;
            });
        }

        /// <summary>
        /// Enumerate all configuration entries invoking the specified callback for each entry.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="section">Optional section name to filter; use null for any.</param>
        /// <param name="property">Optional property name to filter; use null for any.</param>
        /// <param name="cb">Callback to invoke for each matching configuration entry.</param>
        public static void Enumerate(this IGitConfiguration config, string section, string property, GitConfigurationEnumerationCallback cb)
        {
            Enumerate(config, GitConfigurationLevel.All, section, property, cb);
        }

        /// <summary>
        /// Get the value of a configuration entry as a string.
        /// </summary>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">A configuration entry with the specified key was not found.</exception>
        /// <param name="config">Configuration object.</param>
        /// <param name="level">Filter to the specific configuration level.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <returns>Configuration entry value.</returns>
        public static string Get(this IGitConfiguration config, GitConfigurationLevel level, string name)
        {
            if (!config.TryGet(level, GitConfigurationType.Raw, name, out string value))
            {
                throw new KeyNotFoundException($"Git configuration entry with the name '{name}' was not found.");
            }

            return value;
        }

        /// <summary>
        /// Get the value of a configuration entry as a string.
        /// </summary>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">A configuration entry with the specified key was not found.</exception>
        /// <param name="config">Configuration object.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <returns>Configuration entry value.</returns>
        public static string Get(this IGitConfiguration config, string name)
        {
            return Get(config, GitConfigurationLevel.All, name);
        }

        /// <summary>
        /// Try and get the value of a configuration entry as a string.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <param name="isPath">Whether the entry should be canonicalized as a path.</param>
        /// <param name="value">Configuration entry value.</param>
        /// <returns>True if the value was found, false otherwise.</returns>
        public static bool TryGet(this IGitConfiguration config, string name, bool isPath, out string value)
        {
            return config.TryGet(GitConfigurationLevel.All,
                isPath ? GitConfigurationType.Path : GitConfigurationType.Raw,
                name, out value);
        }

        /// <summary>
        /// Get all value of a multivar configuration entry.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="name">Configuration entry name.</param>
        /// <returns>All values of the multivar configuration entry.</returns>
        public static IEnumerable<string> GetAll(this IGitConfiguration config, string name)
        {
            return config.GetAll(GitConfigurationLevel.All, GitConfigurationType.Raw, name);
        }

        /// <summary>
        /// Get all values of a multivar configuration entry.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="nameRegex">Configuration entry name regular expression.</param>
        /// <param name="valueRegex">Regular expression to filter which variables we're interested in. Use null to indicate all.</param>
        /// <returns>All values of the multivar configuration entry.</returns>
        public static IEnumerable<string> GetRegex(this IGitConfiguration config, string nameRegex, string valueRegex)
        {
            return config.GetRegex(GitConfigurationLevel.All, GitConfigurationType.Raw, nameRegex, valueRegex);
        }
    }
}
