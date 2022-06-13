using System.Linq;

namespace GitCredentialManager
{
    public static class QuoteUtils
    {
        public static string QuoteCmdArg(string str)
        {
            char[] needsQuoteChars = {'"', ' ', '\\', '\n', '\r', '\t'};
            bool needsQuotes = str.Any(x => needsQuoteChars.Contains(x));

            if (!needsQuotes)
            {
                return str;
            }

            // Replace all '\' characters with an escaped '\\', and all '"' with '\"'
            string escapedStr = str.Replace("\\", "\\\\").Replace("\"", "\\\"");

            // Bookend the escaped string with double-quotes '"'
            return $"\"{escapedStr}\"";
        }
    }
}
