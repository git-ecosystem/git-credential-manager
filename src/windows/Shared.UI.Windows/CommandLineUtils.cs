using System;
using System.Linq;

namespace Microsoft.Git.CredentialManager.UI
{
    public static class CommandLineUtils
    {
        public static bool TryGetSwitch(string[] args, string name)
        {
            return args.Any(arg => StringComparer.OrdinalIgnoreCase.Equals(arg, name));
        }

        public static string GetParameter(string[] args, string name)
        {
            int index = Array.FindIndex(args, x => StringComparer.OrdinalIgnoreCase.Equals(x, name));

            if (-1 < index && index + 1 < args.Length)
            {
                return args[index + 1];
            }

            return null;
        }
    }
}
