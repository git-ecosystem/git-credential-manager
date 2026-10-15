using System;
using System.Threading;
using GitCredentialManager;
using Xunit;

namespace Core.Tests;

public class TimeSpanExtensionsTests
{
    [Theory]
    [InlineData(1000, 0.0, JitterDirection.Both, 1000, 1000)]
    [InlineData(1000, 0.1, JitterDirection.Both, 950, 1050)]
    [InlineData(1000, 0.5, JitterDirection.Both, 750, 1250)]
    [InlineData(1000, 1.0, JitterDirection.Both, 500, 1500)]
    [InlineData(1000, 0.5, JitterDirection.PositiveOnly, 1000, 1500)]
    [InlineData(1000, 0.5, JitterDirection.NegativeOnly, 500, 1000)]
    [InlineData(-1000, 0.5, JitterDirection.PositiveOnly, -1000, -500)]
    [InlineData(-1000, 0.5, JitterDirection.NegativeOnly, -1500, -1000)]
    public void AddJitter_ReturnsExpectedRange(int milliseconds, double jitterFactor,
        JitterDirection direction, int minimumMilliseconds, int maximumMilliseconds)
    {
        TimeSpan ts = TimeSpan.FromMilliseconds(milliseconds);
        TimeSpan minimum = TimeSpan.FromMilliseconds(minimumMilliseconds);
        TimeSpan maximum = TimeSpan.FromMilliseconds(maximumMilliseconds);

        TimeSpan lower = ts.AddJitter(jitterFactor, direction, new FixedRandom(0.0));
        TimeSpan midpoint = ts.AddJitter(jitterFactor, direction, new FixedRandom(0.5));
        TimeSpan upper = ts.AddJitter(jitterFactor, direction, new FixedRandom(Math.BitDecrement(1.0)));

        Assert.Equal(minimum, lower);
        Assert.Equal(TimeSpan.FromMilliseconds((minimumMilliseconds + maximumMilliseconds) / 2.0), midpoint);
        Assert.InRange(upper, maximum - TimeSpan.FromTicks(1), maximum);
    }

    [Theory]
    [InlineData(JitterDirection.Both)]
    [InlineData(JitterDirection.PositiveOnly)]
    [InlineData(JitterDirection.NegativeOnly)]
    public void AddJitter_Infinite_ReturnsInfinite(JitterDirection direction)
    {
        Assert.Equal(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan.AddJitter(0.5, direction));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void AddJitter_ThrowsArgumentOutOfRangeException_ForInvalidJitterFactor(double jitterFactor)
    {
        TimeSpan ts = TimeSpan.FromMilliseconds(1000);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => ts.AddJitter(jitterFactor));
        Assert.Equal("jitterFactor", error.ParamName);
    }

    [Theory]
    [InlineData((JitterDirection)(-1))]
    [InlineData((JitterDirection)3)]
    public void AddJitter_ThrowsArgumentOutOfRangeException_ForInvalidDirection(JitterDirection direction)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimeSpan.FromMilliseconds(100).AddJitter(0.5, direction));
        Assert.Equal("direction", error.ParamName);
    }

    [Theory]
    [InlineData(50, 125)]
    [InlineData(-50, 75)]
    public void AddRandom_AppliesSignedAdjustment(int maximumMilliseconds, int expectedMilliseconds)
    {
        TimeSpan result = TimeSpan.FromMilliseconds(100)
            .AddRandom(TimeSpan.FromMilliseconds(maximumMilliseconds), new FixedRandom(0.5));

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), result);
    }

    [Fact]
    public void AddRandom_Infinite_ReturnsInfinite()
    {
        Assert.Equal(Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan.AddRandom(TimeSpan.FromMilliseconds(50), new FixedRandom(0.5)));
    }

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
