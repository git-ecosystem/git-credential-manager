// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class StreamExtensionsTests
    {
        private const string LF   = "\n";
        private const string CRLF = "\r\n";

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
        public void StreamExtensions_WriteDictionary_TextWriterLF_EmptyDictionary_WritesLineLF()
        {
            var input = new Dictionary<string, string>();

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: LF);

            Assert.Equal(LF, output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_TextWriterCRLF_EmptyDictionary_WritesLineCRLF()
        {
            var input = new Dictionary<string, string>();

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: CRLF);

            Assert.Equal(CRLF, output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_TextWriterLF_Entries_WritesKVPsAndLF()
        {
            var input = new Dictionary<string, string>
            {
                ["a"] = "1",
                ["b"] = "2",
                ["c"] = "3"
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: LF);

            Assert.Equal("a=1\nb=2\nc=3\n\n", output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_TextWriterCRLF_Entries_WritesKVPsAndCRLF()
        {
            var input = new Dictionary<string, string>
            {
                ["a"] = "1",
                ["b"] = "2",
                ["c"] = "3"
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: CRLF);

            Assert.Equal("a=1\r\nb=2\r\nc=3\r\n\r\n", output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_TextWriterLF_EntriesWithSpaces_WritesKVPsAndLF()
        {
            var input = new Dictionary<string, string>
            {
                ["key a"] = "value 1",
                ["  key b  "] = " value 2 ",
                ["\tvalue\tc\t"] = "\t3\t"
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: LF);

            Assert.Equal("key a=value 1\n  key b  = value 2 \n\tvalue\tc\t=\t3\t\n\n", output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_TextWriterCRLF_EntriesWithSpaces_WritesKVPsAndCRLF()
        {
            var input = new Dictionary<string, string>
            {
                ["key a"] = "value 1",
                ["  key b  "] = " value 2 ",
                ["\tvalue\tc\t"] = "\t3\t"
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: CRLF);

            Assert.Equal("key a=value 1\r\n  key b  = value 2 \r\n\tvalue\tc\t=\t3\t\r\n\r\n", output);
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

        private static string WriteStringStream(IDictionary<string, string> input, Action<TextWriter, IDictionary<string, string>> action, string newLine)
        {
            var output = new StringBuilder();
            using (var writer = new StringWriter(output){NewLine = newLine})
            {
                action(writer, input);
            }

            return output.ToString();
        }

        #endregion
    }
}
