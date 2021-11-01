using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitCredentialManager.Tests.Objects
{
    public class TestGitConfiguration : IGitConfiguration
    {
        public const string CanonicalPathPrefix = "/my/path/prefix";

        public IDictionary<string, IList<string>> System { get; set; } =
            new Dictionary<string, IList<string>>(GitConfigurationKeyComparer.Instance);
        public IDictionary<string, IList<string>> Global { get; set; } =
            new Dictionary<string, IList<string>>(GitConfigurationKeyComparer.Instance);
        public IDictionary<string, IList<string>> Local { get; set; } =
            new Dictionary<string, IList<string>>(GitConfigurationKeyComparer.Instance);

        #region IGitConfiguration

        public void Enumerate(GitConfigurationLevel level, GitConfigurationEnumerationCallback cb)
        {
            foreach (var (dictLevel, dict) in GetDictionaries(level))
            {
                foreach (var kvp in dict)
                {
                    foreach (var value in kvp.Value)
                    {
                        var entry = new GitConfigurationEntry(kvp.Key, value);
                        if (!cb(entry))
                        {
                            break;
                        }
                    }
                }
            }
        }

        public bool TryGet(GitConfigurationLevel level, GitConfigurationType type, string name, out string value)
        {
            value = null;

            // Proceed in order from least to most specific level and read the entry value
            foreach (var (_, dict) in GetDictionaries(level))
            {
                if (dict.TryGetValue(name, out var values))
                {
                    if (values.Count > 1)
                    {
                        throw new Exception("Configuration entry is a multivar");
                    }

                    if (values.Count == 1)
                    {
                        value = values[0];

                        if (type == GitConfigurationType.Path)
                        {
                            // Create "fake" canonical path
                            value = value.Replace("~", CanonicalPathPrefix);
                        }
                    }
                }
            }

            return value != null;
        }

        public void Set(GitConfigurationLevel level, string name, string value)
        {
            IDictionary<string, IList<string>> dict = GetDictionary(level);

            if (!dict.TryGetValue(name, out var values))
            {
                values = new List<string>();
                dict[name] = values;
            }

            // Simulate git
            if (values.Count > 1)
            {
                throw new Exception("Configuration entry is a multivar");
            }

            if (values.Count == 1)
            {
                values[0] = value;
            }
            else if (values.Count == 0)
            {
                values.Add(value);
            }
        }

        public void Add(GitConfigurationLevel level, string name, string value)
        {
            IDictionary<string, IList<string>> dict = GetDictionary(level);

            if (!dict.TryGetValue(name, out IList<string> values))
            {
                values = new List<string>();
                dict[name] = values;
            }

            values.Add(value);
        }

        public void Unset(GitConfigurationLevel level, string name)
        {
            IDictionary<string, IList<string>> dict = GetDictionary(level);

            // Simulate git
            if (dict.TryGetValue(name, out var values) && values.Count > 1)
            {
                throw new Exception("Configuration entry is a multivar");
            }

            dict.Remove(name);
        }

        public IEnumerable<string> GetAll(GitConfigurationLevel level, GitConfigurationType type, string name)
        {
            foreach (var (_, dict) in GetDictionaries(level))
            {
                if (dict.TryGetValue(name, out IList<string> values))
                {
                    foreach (string value in values)
                    {
                        yield return value;
                    }
                }
            }
        }

        public IEnumerable<string> GetRegex(GitConfigurationLevel level, GitConfigurationType type, string nameRegex, string valueRegex)
        {
            foreach (var (_, dict) in GetDictionaries(level))
            {
                foreach (string key in dict.Keys)
                {
                    if (Regex.IsMatch(key, nameRegex))
                    {
                        var values = dict[key].Where(x => Regex.IsMatch(x, valueRegex));
                        foreach (string value in values)
                        {
                            yield return value;
                        }
                    }
                }
            }
        }

        public void ReplaceAll(GitConfigurationLevel level, string nameRegex, string valueRegex, string value)
        {
            IDictionary<string, IList<string>> dict = GetDictionary(level);

            if (!dict.TryGetValue(nameRegex, out IList<string> values))
            {
                values = new List<string>();
                dict[nameRegex] = values;
            }

            bool updated = false;
            for (int i = 0; i < values.Count; i++)
            {
                // Update matching values
                if (Regex.IsMatch(values[i], valueRegex))
                {
                    values[i] = value;
                    updated = true;
                }
            }

            // If no existing value was found to update, add a new one
            if (!updated)
            {
                values.Add(value);
            }
        }

        public void UnsetAll(GitConfigurationLevel level, string name, string valueRegex)
        {
            IDictionary<string, IList<string>> dict = GetDictionary(level);

            if (dict.TryGetValue(name, out IList<string> values))
            {
                for (int i = 0; i < values.Count;)
                {
                    // Remove matching values
                    if (Regex.IsMatch(values[i], valueRegex))
                    {
                        values.RemoveAt(i);
                    }
                    else
                    {
                        // Move to the next value
                        i++;
                    }
                }

                // If we've removed all values, remove the top-level list from the multivar dictionary
                if (values.Count == 0)
                {
                    dict.Remove(name);
                }
            }
        }

        #endregion

        private IDictionary<string, IList<string>> GetDictionary(GitConfigurationLevel level)
        {
            switch (level)
            {
                case GitConfigurationLevel.System:
                    return System;
                case GitConfigurationLevel.Global:
                    return Global;
                case GitConfigurationLevel.Local:
                    return Local;
                case GitConfigurationLevel.All:
                    throw new ArgumentException("Must specify a specific level");
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported level");
            }
        }

        private IEnumerable<(GitConfigurationLevel level, IDictionary<string, IList<string>> dict)> GetDictionaries(
            GitConfigurationLevel level)
        {
            switch (level)
            {
                case GitConfigurationLevel.System:
                    yield return (GitConfigurationLevel.System, System);
                    break;
                case GitConfigurationLevel.Global:
                    yield return (GitConfigurationLevel.Global, Global);
                    break;
                case GitConfigurationLevel.Local:
                    yield return (GitConfigurationLevel.Local, Local);
                    break;
                case GitConfigurationLevel.All:
                    yield return (GitConfigurationLevel.System, System);
                    yield return (GitConfigurationLevel.Global, Global);
                    yield return (GitConfigurationLevel.Local, Local);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported level");
            }
        }
    }
}
