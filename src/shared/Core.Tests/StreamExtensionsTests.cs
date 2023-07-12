using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class StreamExtensionsTests
    {
        private const string LF   = "\n";
        private const string CRLF = "\r\n";

        #region Dictionary

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
            AssertDictionary("1", "a", output);
            AssertDictionary("2", "b", output);
            AssertDictionary("3", "c", output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_TerminatedCRLF_ReturnsDictionary()
        {
            string input = "a=1\r\nb=2\r\nc=3\r\n\r\n";

            var output = ReadStringStream(input, StreamExtensions.ReadDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);
            AssertDictionary("1", "a", output);
            AssertDictionary("2", "b", output);
            AssertDictionary("3", "c", output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_CaseSensitive_ReturnsDictionaryWithMultipleEntries()
        {
            string input = "a=1\nA=2\n\n";

            var output = ReadStringStream(input, x => StreamExtensions.ReadDictionary(x, StringComparer.Ordinal));

            Assert.NotNull(output);
            Assert.Equal(2, output.Count);
            AssertDictionary("1", "a", output);
            AssertDictionary("2", "A", output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_CaseInsensitive_ReturnsDictionaryWithLastValue()
        {
            string input = "a=1\nA=2\n\n";

            var output = ReadStringStream(input, x => StreamExtensions.ReadDictionary(x, StringComparer.OrdinalIgnoreCase));

            Assert.NotNull(output);
            Assert.Equal(1, output.Count);
            AssertDictionary("2", "a", output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_Spaces_ReturnsCorrectKeysAndValues()
        {
            string input = "key a=value 1\n  key b  = 2 \nkey\tc\t=\t3\t\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);
            AssertDictionary("value 1", "key a", output);
            AssertDictionary(" 2 ", "  key b  ", output);
            AssertDictionary("\t3\t", "key\tc\t", output);
        }

        [Fact]
        public void StreamExtensions_ReadDictionary_EqualsInValues_ReturnsCorrectKeysAndValues()
        {
            string input = "a=value=1\nb=value=2\nc=value=3\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);
            AssertDictionary("value=1", "a", output);
            AssertDictionary("value=2", "b", output);
            AssertDictionary("value=3", "c", output);
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

        #endregion

        #region MultiDictionary

        [Fact]
        public void StreamExtensions_ReadMultiDictionary_EmptyString_ReturnsEmptyDictionary()
        {
            string input = string.Empty;

            var output = ReadStringStream(input, StreamExtensions.ReadMultiDictionary);

            Assert.NotNull(output);
            Assert.Equal(0, output.Count);
        }

        [Fact]
        public void StreamExtensions_ReadMultiDictionary_TerminatedLF_ReturnsDictionary()
        {
            string input = "a=1\nb=2\nc=3\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadMultiDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);

            AssertMultiDictionary(new[] { "1" }, "a", output);
            AssertMultiDictionary(new[] { "2" }, "b", output);
            AssertMultiDictionary(new[] { "3" }, "c", output);
        }

        [Fact]
        public void StreamExtensions_ReadMultiDictionary_TerminatedCRLF_ReturnsDictionary()
        {
            string input = "a=1\r\nb=2\r\nc=3\r\n\r\n";

            var output = ReadStringStream(input, StreamExtensions.ReadMultiDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);
            AssertMultiDictionary(new[] { "1" }, "a", output);
            AssertMultiDictionary(new[] { "2" }, "b", output);
            AssertMultiDictionary(new[] { "3" }, "c", output);
        }

        [Fact]
        public void StreamExtensions_ReadMultiDictionary_CaseSensitive_ReturnsDictionaryWithMultipleEntries()
        {
            string input = "a=1\nA=2\n\n";

            var output = ReadStringStream(input, x => StreamExtensions.ReadMultiDictionary(x, StringComparer.Ordinal));

            Assert.NotNull(output);
            Assert.Equal(2, output.Count);
            AssertMultiDictionary(new[] { "1" }, "a", output);
            AssertMultiDictionary(new[] { "2" }, "A", output);
        }

        [Fact]
        public void StreamExtensions_ReadMultiDictionary_CaseInsensitive_ReturnsDictionaryWithLastValue()
        {
            string input = "a=1\nA=2\n\n";

            var output = ReadStringStream(input, x => StreamExtensions.ReadMultiDictionary(x, StringComparer.OrdinalIgnoreCase));

            Assert.NotNull(output);
            Assert.Equal(1, output.Count);
            AssertMultiDictionary(new[] { "2" }, "a", output);
        }

        [Fact]
        public void StreamExtensions_ReadMultiDictionary_EmptyString_ReturnsKeyWithEmptyStringValue()
        {
            string input = "a=\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadMultiDictionary);

            Assert.NotNull(output);
            Assert.Equal(1, output.Count);

            AssertMultiDictionary(new[] { String.Empty,  }, "a", output);
        }

        [Fact]
        public void StreamExtensions_ReadMultiDictionary_Spaces_ReturnsCorrectKeysAndValues()
        {
            string input = "key a=value 1\n  key b  = 2 \nkey\tc\t=\t3\t\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadMultiDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);

            AssertMultiDictionary(new[] { "value 1" }, "key a", output);
            AssertMultiDictionary(new[] { " 2 " }, "  key b  ", output);
            AssertMultiDictionary(new[] { "\t3\t" }, "key\tc\t", output);
        }

        [Fact]
        public void StreamExtensions_ReadMultiDictionary_EqualsInValues_ReturnsCorrectKeysAndValues()
        {
            string input = "a=value=1\nb=value=2\nc=value=3\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadMultiDictionary);

            Assert.NotNull(output);
            Assert.Equal(3, output.Count);
            AssertMultiDictionary(new[] { "value=1" }, "a", output);
            AssertMultiDictionary(new[] { "value=2" }, "b", output);
            AssertMultiDictionary(new[] { "value=3" }, "c", output);
        }

        [Fact]
        public void StreamExtensions_ReadMultiDictionary_MultiValue_ReturnsDictionary()
        {
            string input = "odd[]=1\neven[]=2\neven[]=4\nodd[]=3\n\n";

            var output = ReadStringStream(input, StreamExtensions.ReadMultiDictionary);

            Assert.NotNull(output);
            Assert.Equal(2, output.Count);
            AssertMultiDictionary(new[] { "1", "3" }, "odd", output);
            AssertMultiDictionary(new[] { "2", "4" }, "even", output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_TextWriterLF_EmptyMultiDictionary_WritesLineLF()
        {
            var input = new Dictionary<string, IList<string>>();

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: LF);

            Assert.Equal(LF, output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_TextWriterCRLF_EmptyMultiDictionary_WritesLineCRLF()
        {
            var input = new Dictionary<string, IList<string>>();

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: CRLF);

            Assert.Equal(CRLF, output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_TextWriterLF_MultiEntries_WritesKVPListsAndLF()
        {
            var input = new Dictionary<string, IList<string>>
            {
                ["a"] = new[] { "1", "2", "3" },
                ["b"] = new[] { "4", "5", },
                ["c"] = new[] { "6" }
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: LF);

            Assert.Equal("a[]=1\na[]=2\na[]=3\nb[]=4\nb[]=5\nc=6\n\n", output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_TextWriterCRLF_MultiEntries_WritesKVPListsAndCRLF()
        {
            var input = new Dictionary<string, IList<string>>
            {
                ["a"] = new[] { "1", "2", "3" },
                ["b"] = new[] { "4", "5", },
                ["c"] = new[] { "6" }
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: CRLF);

            Assert.Equal("a[]=1\r\na[]=2\r\na[]=3\r\nb[]=4\r\nb[]=5\r\nc=6\r\n\r\n", output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_NoMultiEntries_WritesKVPsAndLF()
        {
            var input = new Dictionary<string, IList<string>>
            {
                ["a"] = new[] {"1"},
                ["b"] = new[] {"2"},
                ["c"] = new[] {"3"}
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: LF);

            Assert.Equal("a=1\nb=2\nc=3\n\n", output);
        }

        [Fact]
        public void StreamExtensions_WriteDictionary_MultiEntriesWithEmpty_WritesKVPListsAndLF()
        {
            var input = new Dictionary<string, IList<string>>
            {
                ["a"] = new[] {"1", "2", "", "3", "4"},
                ["b"] = new[] {"5"},
                ["c"] = new[] {"6", "7", ""}
            };

            string output = WriteStringStream(input, StreamExtensions.WriteDictionary, newLine: LF);

            Assert.Equal("a[]=3\na[]=4\nb=5\n\n", output);
        }

        #endregion

        #region Helpers

        private static T ReadStringStream<T>(string input, Func<TextReader, T> func)
        {
            T output;
            using (var reader = new StringReader(input))
            {
                output = func(reader);
            }

            return output;
        }

        private static string WriteStringStream<T>(T input, Action<TextWriter, T> action, string newLine)
        {
            var output = new StringBuilder();
            using (var writer = new StringWriter(output){NewLine = newLine})
            {
                action(writer, input);
            }

            return output.ToString();
        }

        private static void AssertDictionary(string expectedValue, string key, IDictionary<string, string> dict)
        {
            Assert.True(dict.TryGetValue(key, out string actualValue));
            Assert.Equal(expectedValue, actualValue);
        }

        private static void AssertMultiDictionary(IList<string> expectedValues,
            string key,
            IDictionary<string, IList<string>> dict)
        {
            Assert.True(dict.TryGetValue(key, out IList<string> actualValues));
            Assert.Equal(expectedValues, actualValues);
        }

        #endregion
    }
}
