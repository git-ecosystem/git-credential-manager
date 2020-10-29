// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestGitConfiguration : IGitConfiguration
    {
        public TestGitConfiguration(IDictionary<string, IList<string>> config = null)
        {
            Dictionary = config ?? new Dictionary<string, IList<string>>();
        }

        /// <summary>
        /// Backing dictionary for the test configuration entries.
        /// </summary>
        public IDictionary<string, IList<string>> Dictionary { get; }

        /// <summary>
        /// Convenience accessor for the backing <see cref="Dictionary"/> of configuration entries.
        /// </summary>
        /// <param name="key"></param>
        public string this[string key]
        {
            get => TryGetValue(key, out string value) ? value : null;
            set => SetValue(key, value);
        }

        #region IGitConfiguration

        public void Enumerate(GitConfigurationEnumerationCallback cb)
        {
            foreach (var kvp in Dictionary)
            {
                foreach (var value in kvp.Value)
                {
                    if (!cb(kvp.Key, value))
                    {
                        break;
                    }
                }
            }
        }

        public bool TryGetValue(string name, out string value)
        {
            if (Dictionary.TryGetValue(name, out var values))
            {
                // TODO: simulate git
                if (values.Count > 1)
                {
                    throw new Exception("Configuration entry is a multivar");
                }

                if (values.Count == 1)
                {
                    value = values[0];
                    return true;
                }
            }

            value = null;
            return false;
        }

        public void SetValue(string name, string value)
        {
            if (!Dictionary.TryGetValue(name, out IList<string> values))
            {
                values = new List<string>();
                Dictionary[name] = values;
            }

            // TODO: simulate git
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

        public void Add(string name, string value)
        {
            if (!Dictionary.TryGetValue(name, out IList<string> values))
            {
                values = new List<string>();
                Dictionary[name] = values;
            }

            values.Add(value);
        }

        public void Unset(string name)
        {
            // TODO: simulate git
            if (Dictionary.TryGetValue(name, out var values) && values.Count > 1)
            {
                throw new Exception("Configuration entry is a multivar");
            }

            Dictionary.Remove(name);
        }

        public IEnumerable<string> GetAll(string name)
        {
            if (Dictionary.TryGetValue(name, out IList<string> values))
            {
                return values;
            }

            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetRegex(string nameRegex, string valueRegex)
        {
            foreach (string key in Dictionary.Keys)
            {
                if (Regex.IsMatch(key, nameRegex))
                {
                    return Dictionary[key].Where(x => Regex.IsMatch(x, valueRegex));
                }
            }

            return Enumerable.Empty<string>();
        }

        public void ReplaceAll(string nameRegex, string valueRegex, string value)
        {
            if (!Dictionary.TryGetValue(nameRegex, out IList<string> values))
            {
                values = new List<string>();
                Dictionary[nameRegex] = values;
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

        public void UnsetAll(string name, string valueRegex)
        {
            if (Dictionary.TryGetValue(name, out IList<string> values))
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
                    Dictionary.Remove(name);
                }
            }
        }

        #endregion
    }
}
