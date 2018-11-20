using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class StreamExtensionsTests
    {
        [Fact]
        public void StreamExtensions_ReadDictionary_EmptyString_ReturnsEmptyDictionary()
        {
            string input = string.Empty;

            var output = ReadStringStream(input, StreamExtensions.ReadDictionary);

            Assert.NotNull(output);
            Assert.Equal(0, output.Count);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_TerminatedLF_ReturnsDictionary()
        {
            string input = "a=1\nb=2\nc=3\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);
            Assert.Contains(KeyValuePair.Create("a", "1"), output);
            Assert.Contains(KeyValuePair.Create("b", "2"), output);
            Assert.Contains(KeyValuePair.Create("c", "3"), output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_TerminatedCRLF_ReturnsDictionary()
        {
            string input = "a=1\r\nb=2\r\nc=3\r\n\r\n";

            var output = ReadStringStream(input, StreamExtensions.ReadDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);
            Assert.Contains(KeyValuePair.Create("a", "1"), output);
            Assert.Contains(KeyValuePair.Create("b", "2"), output);
            Assert.Contains(KeyValuePair.Create("c", "3"), output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_CaseSensitive_ReturnsDictionaryWithMultipleEntries()
        {
            string input = "a=1\nA=2\n\n";

            var output = ReadStringStream(input, x => StreamExtensions.ReadDictionary(x, StringComparer.Ordinal));

            Assert.NotNull(output);
            Assert.Equal(2, output.Count);
            Assert.Contains(KeyValuePair.Create("a", "1"), output);
            Assert.Contains(KeyValuePair.Create("A", "2"), output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_CaseInsensitive_ReturnsDictionaryWithLastValue()
        {
            string input = "a=1\nA=2\n\n";

            var output = ReadStringStream(input, x => StreamExtensions.ReadDictionary(x, StringComparer.OrdinalIgnoreCase));

            Assert.NotNull(output);
            Assert.Equal(1, output.Count);
            Assert.Contains(KeyValuePair.Create("a", "2"), output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_Spaces_ReturnsCorrectKeysAndValues()
        {
            string input = "key a=value 1\n  key b  = 2 \nkey\tc\t=\t3\t\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);
            Assert.Contains(KeyValuePair.Create("key a", "value 1"), output);
            Assert.Contains(KeyValuePair.Create("  key b  ", " 2 "), output);
            Assert.Contains(KeyValuePair.Create("key\tc\t", "\t3\t"), output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_EqualsInValues_ReturnsCorrectKeysAndValues()
        {
            string input = "a=value=1\nb=value=2\nc=value=3\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);
            Assert.Contains(KeyValuePair.Create("a", "value=1"), output);
            Assert.Contains(KeyValuePair.Create("b", "value=2"), output);
            Assert.Contains(KeyValuePair.Create("c", "value=3"), output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_EmptyDictionary_WritesLineTerminator()
        {
            var input = new Dictionary<string, string>();

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary);

            Assert.Equal(Environment.NewLine, output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_Entries_WritesKVPsAndLineTerminator()
        {
            var input = new Dictionary<string, string>
            {
                ["a"] = "1",
                ["b"] = "2",
                ["c"] = "3"
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary);

            Assert.Equal("a=1\nb=2\nc=3\n\n", output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_EntriesWithSpaces_WritesKVPsAndLineTerminator()
        {
            var input = new Dictionary<string, string>
            {
                ["key a"] = "value 1",
                ["  key b  "] = " value 2 ",
                ["\tvalue\tc\t"] = "\t3\t"
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary);

            Assert.Equal("key a=value 1\n  key b  = value 2 \n\tvalue\tc\t=\t3\t\n\n", output);
        }

        #region Helpers

        private static IDictionary<string, string> ReadStringStream(string input, Func<TextReader, IDictionary<string, string>> func)
        {
            IDictionary<string, string> output;
            using (var reader = new StringReader(input))
            {
                output = func(reader);
            }

            return output;
        }

        private static string WriteStringStream(IDictionary<string, string> input, Action<TextWriter, IDictionary<string, string>> action)
        {
            var output = new StringBuilder();
            using (var writer = new StringWriter(output))
            {
                action(writer, input);
            }

            return output.ToString();
        }

        #endregion
    }
}
