using System;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class EnsureArgumentTests
    {
        [Theory]
        [InlineData(5, 0, 10, true, true)]
        [InlineData(0, 0, 10, true, true)]
        [InlineData(10, 0, 10, true, true)]
        [InlineData(0, -10, 0, true, true)]
        [InlineData(-10, -10, 0, true, true)]
        public void EnsureArgument_InRange_DoesNotThrow(int x, int lower, int upper, bool lowerInc, bool upperInc)
        {
            EnsureArgument.InRange(x, nameof(x), lower, upper, lowerInc, upperInc);
        }

        [Theory]
        [InlineData(-3, 0, 10, true, true)]
        [InlineData(13, 0, 10, true, true)]
        [InlineData(10, 0, 10, true, false)]
        [InlineData(0, 0, 10, false, true)]
        [InlineData(-10, -10, 0, false, true)]
        [InlineData(0, -10, 0, true, false)]
        public void EnsureArgument_InRange_ThrowsException(int x, int lower, int upper, bool lowerInc, bool upperInc)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => EnsureArgument.InRange(x, nameof(x), lower, upper, lowerInc, upperInc)
            );
        }
    }
}
