// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
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
        [InlineData("fooTRIMbar", "TRIM", StringComparison.Ordinal, "bar")]
        [InlineData("fooTRIMbar", "trim", StringComparison.Ordinal, "fooTRIMbar")]
        [InlineData("fooTRIMbar", "tRiM", StringComparison.Ordinal, "fooTRIMbar")]
        [InlineData("fooTRIMbar", "TRIM", StringComparison.OrdinalIgnoreCase, "bar")]
        [InlineData("fooTRIMbar", "trim", StringComparison.OrdinalIgnoreCase, "bar")]
        [InlineData("fooTRIMbar", "tRiM", StringComparison.OrdinalIgnoreCase, "bar")]
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
    }
}
