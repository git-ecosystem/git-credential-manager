using System;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("tRuE", true)]
        [InlineData("yes", true)]
        [InlineData("YES", true)]
        [InlineData("yEs", true)]
        [InlineData("on", true)]
        [InlineData("ON", true)]
        [InlineData("oN", true)]
        [InlineData("1", true)]
        [InlineData("false", false)]
        [InlineData("i am a random string", false)]
        [InlineData("", false)]
        [InlineData("     ", false)]
        [InlineData("\t", false)]
        [InlineData(null, false)]
        public void StringExtensions_IsTruthy(string input, bool expected)
        {
            if (expected)
            {
                Assert.True(StringExtensions.IsTruthy(input));
            }
            else
            {
                Assert.False(StringExtensions.IsTruthy(input));
            }
        }

        [Theory]
        [InlineData("false", true)]
        [InlineData("FALSE", true)]
        [InlineData("fAlSe", true)]
        [InlineData("no", true)]
        [InlineData("NO", true)]
        [InlineData("nO", true)]
        [InlineData("off", true)]
        [InlineData("OFF", true)]
        [InlineData("oFf", true)]
        [InlineData("0", true)]
        [InlineData("true", false)]
        [InlineData("i am a random string", false)]
        [InlineData("", false)]
        [InlineData("     ", false)]
        [InlineData("\t", false)]
        [InlineData(null, false)]
        public void StringExtensions_IsFalsey(string input, bool expected)
        {
            if (expected)
            {
                Assert.True(StringExtensions.IsFalsey(input));
            }
            else
            {
                Assert.False(StringExtensions.IsFalsey(input));
            }
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("tRuE", true)]
        [InlineData("yes", true)]
        [InlineData("YES", true)]
        [InlineData("yEs", true)]
        [InlineData("on", true)]
        [InlineData("ON", true)]
        [InlineData("oN", true)]
        [InlineData("1", true)]
        [InlineData("false", false)]
        [InlineData("FALSE", false)]
        [InlineData("fAlSe", false)]
        [InlineData("no", false)]
        [InlineData("NO", false)]
        [InlineData("nO", false)]
        [InlineData("off", false)]
        [InlineData("OFF", false)]
        [InlineData("oFf", false)]
        [InlineData("0", false)]
        [InlineData("i am a random string", null)]
        [InlineData("", null)]
        [InlineData("     ", null)]
        [InlineData("\t", null)]
        [InlineData(null, null)]
        public void StringExtensions_ToBooleany(string input, bool? expected)
        {
            Assert.Equal(expected, StringExtensions.ToBooleany(input));
        }

        [Theory]
        [InlineData("true", false, true)]
        [InlineData("TRUE", false, true)]
        [InlineData("tRuE", false, true)]
        [InlineData("yes", false, true)]
        [InlineData("YES", false, true)]
        [InlineData("yEs", false, true)]
        [InlineData("on", false, true)]
        [InlineData("ON", false, true)]
        [InlineData("oN", false, true)]
        [InlineData("1", false, true)]
        [InlineData("false", true, false)]
        [InlineData("FALSE", true, false)]
        [InlineData("fAlSe", true, false)]
        [InlineData("no", true, false)]
        [InlineData("NO", true, false)]
        [InlineData("nO", true, false)]
        [InlineData("off", true, false)]
        [InlineData("OFF", true, false)]
        [InlineData("oFf", true, false)]
        [InlineData("0", true, false)]
        [InlineData("i am a random string", true, true)]
        [InlineData("", true, true)]
        [InlineData("     ", true, true)]
        [InlineData("\t", true, true)]
        [InlineData(null, true, true)]
        [InlineData("i am a random string", false, false)]
        [InlineData("", false, false)]
        [InlineData("     ", false, false)]
        [InlineData("\t", false, false)]
        [InlineData(null, false, false)]
        public void StringExtensions_ToBooleanyOrDefault(string input, bool defaultValue, bool expected)
        {
            Assert.Equal(expected, StringExtensions.ToBooleanyOrDefault(input, defaultValue));
        }

        [Theory]
        [InlineData("", '/', "")]
        [InlineData("/", '/', "")]
        [InlineData("foo", '/', "foo")]
        [InlineData("foo/", '/', "foo")]
        [InlineData("foo/bar", '/', "foo")]
        [InlineData("foo/bar/", '/', "foo")]
        public void StringExtensions_TruncateLastIndexOf(string input, char c, string expected)
        {
            string actual = StringExtensions.TruncateFromIndexOf(input, c);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StringExtensions_TruncateLastIndexOf_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => StringExtensions.TruncateFromIndexOf(null, '/'));
        }

        [Theory]
        [InlineData("", '/', "")]
        [InlineData("/", '/', "")]
        [InlineData("foo", '/', "foo")]
        [InlineData("foo/", '/', "foo")]
        [InlineData("foo/bar", '/', "foo")]
        [InlineData("foo/bar/", '/', "foo/bar")]
        public void StringExtensions_TruncateFromLastIndexOf(string input, char c, string expected)
        {
            string actual = StringExtensions.TruncateFromLastIndexOf(input, c);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StringExtensions_TruncateFromLastIndexOf_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => StringExtensions.TruncateFromLastIndexOf(null, '/'));
        }

        [Theory]
        [InlineData("", '/', "")]
        [InlineData("/", '/', "")]
        [InlineData("foo", '/', "foo")]
        [InlineData("foo/", '/', "")]
        [InlineData("foo/bar", '/', "bar")]
        [InlineData("foo/bar/", '/', "bar/")]
        public void StringExtensions_TrimUntilIndexOf_Character(string input, char c, string expected)
        {
            string actual = StringExtensions.TrimUntilIndexOf(input, c);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StringExtensions_TrimUntilIndexOf_Character_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => StringExtensions.TrimUntilIndexOf(null, '/'));
        }

        [Theory]
        [InlineData("", "://", "")]
        [InlineData("://", "://", "")]
        [InlineData("foo", "://", "foo")]
        [InlineData("foo://", "://", "")]
        [InlineData("foo://bar", "://", "bar")]
        [InlineData("foo://bar/", "://", "bar/")]
        public void StringExtensions_TrimUntilIndexOf_String(string input, string trim, string expected)
        {
            string actual = StringExtensions.TrimUntilIndexOf(input, trim);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("fooTRIMbarTRIMsoup", "TRIM", StringComparison.Ordinal, "barTRIMsoup")]
        [InlineData("fooTRIMbarTRIMsoup", "trim", StringComparison.Ordinal, "fooTRIMbarTRIMsoup")]
        [InlineData("fooTRIMbarTRIMsoup", "tRiM", StringComparison.Ordinal, "fooTRIMbarTRIMsoup")]
        [InlineData("fooTRIMbarTRIMsoup", "TRIM", StringComparison.OrdinalIgnoreCase, "barTRIMsoup")]
        [InlineData("fooTRIMbarTRIMsoup", "trim", StringComparison.OrdinalIgnoreCase, "barTRIMsoup")]
        [InlineData("fooTRIMbarTRIMsoup", "tRiM", StringComparison.OrdinalIgnoreCase, "barTRIMsoup")]
        public void StringExtensions_TrimUntilIndexOf_String_ComparisonType(string input, string trim, StringComparison comparisonType, string expected)
        {
            string actual = StringExtensions.TrimUntilIndexOf(input, trim, comparisonType);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StringExtensions_TrimUntilIndexOf_String_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => StringExtensions.TrimUntilIndexOf(null, "://"));
        }

        [Theory]
        [InlineData("", '/', "")]
        [InlineData("/", '/', "")]
        [InlineData("foo", '/', "foo")]
        [InlineData("foo/", '/', "")]
        [InlineData("foo/bar", '/', "bar")]
        [InlineData("foo/bar/", '/', "")]
        public void StringExtensions_TrimUntilLastIndexOf_Character(string input, char c, string expected)
        {
            string actual = StringExtensions.TrimUntilLastIndexOf(input, c);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StringExtensions_TrimUntilLastIndexOf_Character_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => StringExtensions.TrimUntilLastIndexOf(null, '/'));
        }

        [Theory]
        [InlineData("", "://", "")]
        [InlineData("://", "://", "")]
        [InlineData("foo", "://", "foo")]
        [InlineData("foo://", "://", "")]
        [InlineData("foo://bar", "://", "bar")]
        [InlineData("foo://bar/", "://", "bar/")]
        [InlineData("foo:/bar/baz", ":", "/bar/baz")]
        public void StringExtensions_TrimUntilLastIndexOf_String(string input, string trim, string expected)
        {
            string actual = StringExtensions.TrimUntilLastIndexOf(input, trim);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("fooTRIMbarTRIMsoup", "TRIM", StringComparison.Ordinal, "soup")]
        [InlineData("fooTRIMbarTRIMsoup", "trim", StringComparison.Ordinal, "fooTRIMbarTRIMsoup")]
        [InlineData("fooTRIMbarTRIMsoup", "tRiM", StringComparison.Ordinal, "fooTRIMbarTRIMsoup")]
        [InlineData("fooTRIMbarTRIMsoup", "TRIM", StringComparison.OrdinalIgnoreCase, "soup")]
        [InlineData("fooTRIMbarTRIMsoup", "trim", StringComparison.OrdinalIgnoreCase, "soup")]
        [InlineData("fooTRIMbarTRIMsoup", "tRiM", StringComparison.OrdinalIgnoreCase, "soup")]
        public void StringExtensions_TrimUntilLastIndexOf_String_ComparisonType(string input, string trim, StringComparison comparisonType, string expected)
        {
            string actual = StringExtensions.TrimUntilLastIndexOf(input, trim, comparisonType);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StringExtensions_TrimUntilLastIndexOf_String_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => StringExtensions.TrimUntilLastIndexOf(null, "://"));
        }

        [Theory]
        [InlineData("fooTRIMbar", "TRIM", "foobar")]
        [InlineData("fooTRIMbar", "trim", "fooTRIMbar")]
        [InlineData("fooTRIMbar", "tRiM", "fooTRIMbar")]
        public void StringExtensions_TrimMiddle_String(string input, string trim, string expected)
        {
            string actual = StringExtensions.TrimMiddle(input, trim);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("fooTRIMbar", "TRIM", StringComparison.Ordinal, "foobar")]
        [InlineData("fooTRIMbar", "trim", StringComparison.Ordinal, "fooTRIMbar")]
        [InlineData("fooTRIMbar", "tRiM", StringComparison.Ordinal, "fooTRIMbar")]
        [InlineData("fooTRIMbar", "TRIM", StringComparison.OrdinalIgnoreCase, "foobar")]
        [InlineData("fooTRIMbar", "trim", StringComparison.OrdinalIgnoreCase, "foobar")]
        [InlineData("fooTRIMbar", "tRiM", StringComparison.OrdinalIgnoreCase, "foobar")]
        public void StringExtensions_TrimMiddle_String_ComparisonType(string input, string trim, StringComparison comparisonType, string expected)
        {
            string actual = StringExtensions.TrimMiddle(input, trim, comparisonType);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("FooBar", "foo_bar")]
        [InlineData("fooBar", "foo_bar")]
        [InlineData("FBBaz", "fb_baz")]
        [InlineData("Foo", "foo")]
        [InlineData("Fo", "fo")]
        [InlineData("fO", "f_o")]
        [InlineData("OO", "oo")]
        [InlineData("F", "f")]
        [InlineData("", "")]
        public void StringExtensions_ToSnakeCase_Converts_Correctly(string input, string expected)
        {
            Assert.Equal(expected, input.ToSnakeCase());
        }
    }
}
