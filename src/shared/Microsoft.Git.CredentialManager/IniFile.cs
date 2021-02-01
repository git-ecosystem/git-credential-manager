// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents an INI based configuration file.
    /// </summary>
    public class IniFile
    {
        /// <summary>
        /// All sections in this INI file.
        /// </summary>
        public IList<IniSection> Sections { get; } = new List<IniSection>();

        /// <summary>
        /// Attempt to find a section in this file with the given name and optional scope.
        /// </summary>
        /// <param name="name">Section name.</param>
        /// <param name="scope">Optional section scope.</param>
        /// <param name="section">Section with the specified name and scope.</param>
        /// <returns>True if a section was found, false otherwise.</returns>
        public bool TryGetSection(string name, string scope, out IniSection section)
        {
            EnsureArgument.NotNullOrWhiteSpace(name, nameof(name));

            foreach (IniSection s in Sections)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(name, s.Name) &&
                    StringComparer.OrdinalIgnoreCase.Equals(scope, s.Scope))
                {
                    section = s;
                    return true;
                }
            }

            section = null;
            return false;
        }

        /// <summary>
        /// Try to get the value of a property in the INI file.
        /// </summary>
        /// <param name="section">Section name.</param>
        /// <param name="scope">Optional section scope.</param>
        /// <param name="property">Property name.</param>
        /// <param name="value">Value of the property.</param>
        /// <returns>True if the property was present and found, false otherwise.</returns>
        /// <remarks>Null is a valid value.</remarks>
        public bool TryGetValue(string section, string scope, string property, out string value)
        {
            value = null;
            return TryGetSection(section, scope, out IniSection sectionObj) &&
                   sectionObj.Properties.TryGetValue(property, out value);
        }

        /// <summary>
        /// Set the value of a property in the INI file.
        /// </summary>
        /// <param name="section">Section name.</param>
        /// <param name="scope">Optional section scope.</param>
        /// <param name="property">Property name.</param>
        /// <param name="value">Value of the property.</param>
        /// <remarks>Null is a valid value. Use <see cref="UnsetValue"/> to remove the property.</remarks>
        public void SetValue(string section, string scope, string property, string value)
        {
            if (!TryGetSection(section, scope, out IniSection sectionObj))
            {
                sectionObj = new IniSection(section, scope);
                Sections.Add(sectionObj);
            }

            sectionObj.Properties[property] = value;
        }

        /// <summary>
        /// Unset/remove a property from the INI file.
        /// </summary>
        /// <param name="section">Section name.</param>
        /// <param name="scope">Optional section scope.</param>
        /// <param name="property">Property name.</param>
        public void UnsetValue(string section, string scope, string property)
        {
            if (TryGetSection(section, scope, out IniSection sectionObj))
            {
                sectionObj.Properties.Remove(property);
            }
        }
    }

    [DebuggerDisplay("{DebuggerDisplay}")]
    public class IniSection
    {
        public IniSection(string name, string scope)
        {
            Name = name;
            Scope = scope;
        }

        /// <summary>
        /// Name of the INI file section.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional scope of the INI file section.
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// All properties in this section.
        /// </summary>
        public IDictionary<string, string> Properties { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string DebuggerDisplay => Scope == null ? $"[{Name}]" : $"[{Name} \"{Scope}\"]";
    }
}
