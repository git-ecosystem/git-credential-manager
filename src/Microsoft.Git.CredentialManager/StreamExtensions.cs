using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    public static class StreamExtensions
    {
        public static IDictionary<string, string> ReadDictionary(this TextReader reader) =>
            ReadDictionary(reader, StringComparer.Ordinal);

        public static IDictionary<string, string> ReadDictionary(this TextReader reader, StringComparer comparer)
        {
            var dict = new Dictionary<string, string>(comparer);

            string line;
            while ((line = reader.ReadLine()) != null && !string.IsNullOrWhiteSpace(line))
            {
                ParseLine(dict, line);
            }

            return dict;
        }

        public static Task<IDictionary<string, string>> ReadDictionaryAsync(this TextReader reader) =>
            ReadDictionaryAsync(reader, StringComparer.Ordinal);

        public static async Task<IDictionary<string, string>> ReadDictionaryAsync(this TextReader reader, StringComparer comparer)
        {
            var dict = new Dictionary<string, string>(comparer);

            string line;
            while ((line = await reader.ReadLineAsync()) != null && !string.IsNullOrWhiteSpace(line))
            {
                ParseLine(dict, line);
            }

            return dict;
        }

        public static void WriteDictionary(this TextWriter writer, IDictionary<string, string> dict)
        {
            foreach (var kvp in dict)
            {
                WriteKeyValuePair(writer, kvp);
            }

            // Write terminating line
            writer.WriteLine();
        }

        public static async Task WriteDictionaryAsync(this TextWriter writer, IDictionary<string, string> dict)
        {
            foreach (var kvp in dict)
            {
                await WriteKeyValuePairAsync(writer, kvp);
            }

            // Write terminating line
            await writer.WriteLineAsync();
        }

        private static void WriteKeyValuePair(this TextWriter writer, KeyValuePair<string, string> kvp)
            => WriteKeyValuePair(writer, kvp.Key, kvp.Value);

        private static void WriteKeyValuePair(this TextWriter writer, string key, string value)
        {
            writer.WriteLine("{0}={1}", key, value);
        }

        private static Task WriteKeyValuePairAsync(this TextWriter writer, KeyValuePair<string, string> kvp)
            => WriteKeyValuePairAsync(writer, kvp.Key, kvp.Value);

        private static Task WriteKeyValuePairAsync(this TextWriter writer, string key, string value)
        {
            return writer.WriteLineAsync($"{key}={value}");
        }

        private static void ParseLine(IDictionary<string,string> dict, string line)
        {
            int splitIndex = line.IndexOf('=');
            if (splitIndex > 0)
            {
                string key = line.Substring(0, splitIndex);
                string value = line.Substring(splitIndex + 1);

                dict[key] = value;
            }
        }
    }
}
