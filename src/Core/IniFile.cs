using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitCredentialManager
{
    public class IniFile
    {
        public IniFile()
        {
            Sections = new Dictionary<IniSectionName, IniSection>();
        }

        public IDictionary<IniSectionName, IniSection> Sections { get; }

        public bool TryGetSection(string name, string subName, out IniSection section)
        {
            return Sections.TryGetValue(new IniSectionName(name, subName), out section);
        }

        public bool TryGetSection(string name, out IniSection section)
        {
            return Sections.TryGetValue(new IniSectionName(name), out section);
        }
    }

    [DebuggerDisplay("{DebuggerDisplay}")]
    public readonly struct IniSectionName : IEquatable<IniSectionName>
    {
        public IniSectionName(string name, string subName = null)
        {
            Name = name;
            SubName = string.IsNullOrEmpty(subName) ? null : subName;
        }

        public string Name { get; }

        public string SubName { get; }

        public bool Equals(IniSectionName other)
        {
            // Main section name is case-insensitive, but subsection name IS case-sensitive!
            return StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name) &&
                   StringComparer.Ordinal.Equals(SubName, other.SubName);
        }

        public override bool Equals(object obj)
        {
            return obj is IniSectionName other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.ToLowerInvariant().GetHashCode() : 0) * 397) ^
                       (SubName != null ? SubName.GetHashCode() : 0);
            }
        }

        private string DebuggerDisplay => SubName is null ? Name : $"{Name} \"{SubName}\"";
    }

    [DebuggerDisplay("{DebuggerDisplay}")]
    public class IniSection
    {
        public IniSection(IniSectionName name)
        {
            Name = name;
            Properties = new List<IniProperty>();
        }

        public IniSectionName Name { get; }

        public IList<IniProperty> Properties { get; }

        public bool TryGetProperty(string name, out string value)
        {
            if (TryGetMultiProperty(name, out IEnumerable<string> values))
            {
                value = values.Last();
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetMultiProperty(string name, out IEnumerable<string> values)
        {
            IniProperty[] props = Properties
                .Where(x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, name))
                .ToArray();

            if (props.Length == 0)
            {
                values = Array.Empty<string>();
                return false;
            }

            values = props.Select(x => x.Value);
            return true;
        }

        private string DebuggerDisplay => Name.SubName is null
            ? $"{Name.Name} [Properties: {Properties.Count}]"
            : $"{Name.Name} \"{Name.SubName}\" [Properties: {Properties.Count}]";
    }

    [DebuggerDisplay("{DebuggerDisplay}")]
    public class IniProperty
    {
        public IniProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }

        private string DebuggerDisplay => $"{Name}={Value}";
    }

    public static class IniSerializer
    {
        private static readonly Regex SectionRegex =
            new Regex(@"^\[[^\S#]*(?'name'[^\s#\]]*?)(?:\s+""(?'sub'.+)"")?\s*\]", RegexOptions.Compiled);

        private static readonly Regex PropertyRegex =
            new Regex(@"^[^\S#]*?(?'name'[^\s#]+)\s*=(?'value'.*)?$", RegexOptions.Compiled);

        public static IniFile Deserialize(IFileSystem fs, string path)
        {
            IEnumerable<string> lines = fs.ReadAllLines(path).Select(x => x.Trim());

            var iniFile = new IniFile();
            IniSection section = null;

            foreach (string line in lines)
            {
                Match match = SectionRegex.Match(line);
                if (match.Success)
                {
                    string mainName = match.Groups["name"].Value;
                    string subName = match.Groups["sub"].Value;

                    // Skip empty-named sections
                    if (string.IsNullOrWhiteSpace(mainName))
                    {
                        continue;
                    }

                    if (!iniFile.TryGetSection(mainName, subName, out section))
                    {
                        var sectionName = new IniSectionName(mainName, subName);
                        section = new IniSection(sectionName);
                        iniFile.Sections[sectionName] = section;
                    }

                    continue;
                }

                match = PropertyRegex.Match(line);
                if (match.Success)
                {
                    if (section is null)
                    {
                        throw new Exception("Missing section header");
                    }

                    string propName = match.Groups["name"].Value;
                    string propValue = match.Groups["value"].Value.Trim();

                    // Trim trailing comments
                    int firstDQuote = propValue.IndexOf('"');
                    int lastDQuote = propValue.LastIndexOf('"');
                    int commentIdx = propValue.LastIndexOf('#');
                    if (commentIdx > -1)
                    {
                        bool insideDQuotes = firstDQuote > -1 && lastDQuote > -1 &&
                                             (firstDQuote < commentIdx && commentIdx < lastDQuote);

                        if (!insideDQuotes)
                        {
                            propValue = propValue.Substring(0, commentIdx).Trim();
                        }
                    }

                    // Trim book-ending double quotes: "foo" => foo
                    if (propValue.Length > 1 && propValue[0] == '"' &&
                        propValue[propValue.Length - 1] == '"')
                    {
                        propValue = propValue.Substring(1, propValue.Length - 2);
                    }

                    var property = new IniProperty(propName, propValue);
                    section.Properties.Add(property);
                }
            }

            return iniFile;
        }
    }
}
