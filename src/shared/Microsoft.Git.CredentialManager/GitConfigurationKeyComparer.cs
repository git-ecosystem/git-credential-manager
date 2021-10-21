using System;

namespace GitCredentialManager
{
    /// <summary>
    /// Represents string comparison of Git configuration entry key names.
    /// </summary>
    /// <remarks>
    /// Git configuration entries have the form "section[.scope].property", where the
    /// scope part is optional.
    /// <para/>
    /// The section and property components are NOT case sensitive.
    /// The scope component if present IS case sensitive.
    /// </remarks>
    public class GitConfigurationKeyComparer : StringComparer
    {
        public static readonly GitConfigurationKeyComparer Instance = new GitConfigurationKeyComparer();

        public static readonly StringComparer SectionComparer = OrdinalIgnoreCase;
        public static readonly StringComparer ScopeComparer = Ordinal;
        public static readonly StringComparer PropertyComparer = OrdinalIgnoreCase;

        private GitConfigurationKeyComparer() { }

        public override int Compare(string x, string y)
        {
            TrySplit(x, out string xSection, out string xScope, out string xProperty);
            TrySplit(y, out string ySection, out string yScope, out string yProperty);

            int cmpSection = OrdinalIgnoreCase.Compare(xSection, ySection);
            if (cmpSection != 0) return cmpSection;

            int cmpProperty = OrdinalIgnoreCase.Compare(xProperty, yProperty);
            if (cmpProperty != 0) return cmpProperty;

            return Ordinal.Compare(xScope, yScope);
        }

        public override bool Equals(string x, string y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            TrySplit(x, out string xSection, out string xScope, out string xProperty);
            TrySplit(y, out string ySection, out string yScope, out string yProperty);

            // Section and property names are not case sensitive, but the inner 'scope' IS case sensitive!
            return OrdinalIgnoreCase.Equals(xSection, ySection) &&
                   OrdinalIgnoreCase.Equals(xProperty, yProperty) &&
                   Ordinal.Equals(xScope, yScope);
        }

        public override int GetHashCode(string obj)
        {
            TrySplit(obj, out string section, out string scope, out string property);

            int code = OrdinalIgnoreCase.GetHashCode(section) ^
                       OrdinalIgnoreCase.GetHashCode(property);

            return scope is null
                ? code
                : code ^ Ordinal.GetHashCode(scope);
        }

        public static bool TrySplit(string str, out string section, out string scope, out string property)
        {
            section = null;
            scope = null;
            property = null;

            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            section = str.TruncateFromIndexOf('.');
            property = str.TrimUntilLastIndexOf('.');
            int scopeLength = str.Length - (section.Length + property.Length + 2);
            scope = scopeLength > 0 ? str.Substring(section.Length + 1, scopeLength) : null;

            return true;
        }
    }
}
