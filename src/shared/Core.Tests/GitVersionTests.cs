using System;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class GitVersionTests
    {
        [Theory]
        [InlineData(null, 1)]
        [InlineData("2", 1)]
        [InlineData("3", -1)]
        [InlineData("2.33", 0)]
        [InlineData("2.32.0", 1)]
        [InlineData("2.33.0.windows.0.1", 0)]
        [InlineData("2.33.0.2", -1)]
        public void GitVersion_CompareTo_2_33_0(string input, int expectedCompare)
        {
            GitVersion baseline = new GitVersion(2, 33, 0);
            GitVersion actual = new GitVersion(input);
            Assert.Equal(expectedCompare, baseline.CompareTo(actual));
        }
    }
}
