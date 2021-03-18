// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Performs serialization and deserialization of INI files <seealso cref="IniFile"/>.
    /// </summary>
    public class IniSerializer
    {
        private static readonly Regex SectionRegex = new Regex(@"\[\s*(?<name>.+?)(?:\s+\""(?<scope>.+)\"")?\s*\]", RegexOptions.Compiled);
        private static readonly Regex PropertyRegex = new Regex(@"(?<name>\S+?)\s*\=\s*(?<value>.+)?", RegexOptions.Compiled);

        /// <summary>
        /// Serialize the given INI file to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="file">INI file to serialize.</param>
        /// <param name="writer"><see cref="TextWriter"/> to serialize the INI file to.</param>
        public void Serialize(IniFile file, TextWriter writer)
        {
            foreach (IniSection section in file.Sections)
            {
                if (section.Properties.Any())
                {
                    WriteSectionHeader(writer, section);
                    writer.WriteLine();

                    foreach (var property in section.Properties)
                    {
                        WriteProperty(writer, property);
                        writer.WriteLine();
                    }

                    writer.WriteLine();
                    writer.Flush();
                }
            }
        }

        /// <summary>
        /// Deserialize an INI file from a <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="TextReader"/> to deserialize the INI file from.</param>
        /// <returns>INI file.</returns>
        public IniFile Deserialize(TextReader reader)
        {
            var file = new IniFile();

            IniSection currentSection = null;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (TryParseSection(line, out IniSection newSection))
                {
                    if (file.TryGetSection(newSection.Name, newSection.Scope, out IniSection existingSection))
                    {
                        currentSection = existingSection;
                    }
                    else
                    {
                        currentSection = newSection;
                        file.Sections.Add(newSection);
                    }
                }
                else if (TryParseProperty(line, out string propertyName, out string propertyValue))
                {
                    if (currentSection is null)
                    {
                        throw new Exception("Invalid INI file. Properties must exist in a section.");
                    }

                    currentSection.Properties[propertyName] = propertyValue;
                }
                else
                {
                    // Invalid line
                }
            }

            return file;
        }

        #region Writer Helpers

        private void WriteProperty(TextWriter writer, KeyValuePair<string, string> property)
        {
            writer.Write('\t');
            writer.Write(property.Key);
            writer.Write(" =");
            if (!string.IsNullOrWhiteSpace(property.Value))
            {
                writer.Write(' ');
                writer.Write(property.Value);
            }
        }

        private void WriteSectionHeader(TextWriter writer, IniSection section)
        {
            writer.Write('[');
            writer.Write(section.Name);
            if (section.Scope != null)
            {
                writer.Write(" \"{0}\"", section.Scope);
            }

            writer.Write(']');
        }

        #endregion

        #region Parsing Helpers

        private bool TryParseSection(string line, out IniSection section)
        {
            var match = SectionRegex.Match(line);
            if (match.Success)
            {
                string name = match.Groups["name"].Value;
                string scope = match.Groups["scope"].Success ? match.Groups["scope"].Value : null;

                section = new IniSection(name, scope);
                return true;
            }

            section = null;
            return false;
        }

        private bool TryParseProperty(string line, out string propertyName, out string propertyValue)
        {
            var match = PropertyRegex.Match(line);
            if (match.Success)
            {
                propertyName = match.Groups["name"].Value;
                propertyValue = match.Groups["value"].Success ? match.Groups["value"].Value : null;

                return true;
            }

            propertyName = null;
            propertyValue = null;
            return false;
        }

        #endregion
    }
}
