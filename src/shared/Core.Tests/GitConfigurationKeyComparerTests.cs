using Xunit;

namespace GitCredentialManager.Tests
{
    public class GitConfigurationKeyComparerTests
    {
        [Theory]
        [InlineData("", "", true)]
        [InlineData(null, null, true)]
        [InlineData("   ", " ", true)]
        [InlineData("foo", "foo", true)]
        [InlineData("foo", "FOO", true)]
        [InlineData("foo", "bar", false)]
        [InlineData("foo.bar", "foo.bar", true)]
        [InlineData("foo.bar", "foo.fish", false)]
        [InlineData("fish.bar", "foo.bar", false)]
        [InlineData("foo.bar", "FOO.BAR", true)]
        [InlineData("foo.bar", "foo.BAR", true)]
        [InlineData("foo.bar", "FOO.bar", true)]
        [InlineData("foo.example.com.bar", "foo.example.com.bar", true)]
        [InlineData("foo.example.com.bar", "foo.example.com.BAR", true)]
        [InlineData("foo.example.com.bar", "FOO.example.com.BAR", true)]
        [InlineData("foo.example.com.bar", "FOO.example.com.bar", true)]
        [InlineData("foo.example.com.bar", "foo.EXAMPLE.COM.bar", false)]
        public void GitConfigurationKeyComparer_Equals(string x, string y, bool expected)
        {
            bool actual = GitConfigurationKeyComparer.Instance.Equals(x, y);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("", "", 0)]
        [InlineData(null, null, 0)]
        [InlineData("   ", " ", 0)]
        [InlineData("foo", "foo", 0)]
        [InlineData("foo", "FOO", 0)]
        [InlineData("foo", "bar", 4)]
        [InlineData("foo.bar", "foo.bar", 0)]
        [InlineData("foo.bar", "foo.fish", -4)]
        [InlineData("fish.bar", "foo.bar", -6)]
        [InlineData("foo.bar", "FOO.BAR", 0)]
        [InlineData("foo.bar", "foo.BAR", 0)]
        [InlineData("foo.bar", "FOO.bar", 0)]
        [InlineData("foo.example.com.bar", "foo.example.com.bar", 0)]
        [InlineData("foo.example.com.bar", "foo.example.com.BAR", 0)]
        [InlineData("foo.example.com.bar", "FOO.example.com.BAR", 0)]
        [InlineData("foo.example.com.bar", "FOO.example.com.bar", 0)]
        [InlineData("foo.example.com.bar", "foo.EXAMPLE.COM.bar", 32)]
        public void GitConfigurationKeyComparer_Compare(string x, string y, int expected)
        {
            int actual = GitConfigurationKeyComparer.Instance.Compare(x, y);
            Assert.Equal(expected, actual);
        }
    }
}
