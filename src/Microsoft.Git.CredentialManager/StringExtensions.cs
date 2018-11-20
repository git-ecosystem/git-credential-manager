using System;

namespace Microsoft.Git.CredentialManager
{
    public static class StringExtensions
    {
        public static bool IsTruthy(this string str)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(str, bool.TrueString) ||
                   StringComparer.OrdinalIgnoreCase.Equals(str, "1") ||
                   StringComparer.OrdinalIgnoreCase.Equals(str, "on") ||
                   StringComparer.OrdinalIgnoreCase.Equals(str, "yes");
        }
    }
}
