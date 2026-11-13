using System;
using System.Threading;

namespace GitCredentialManager;

public enum JitterDirection
{
    /// <summary>
    /// Apply up to half the jitter amount in either direction.
    /// </summary>
    Both,

    /// <summary>
    /// Apply up to the full jitter amount in the positive direction.
    /// </summary>
    PositiveOnly,

    /// <summary>
    /// Apply up to the full jitter amount in the negative direction.
    /// </summary>
    NegativeOnly,
}

public static class TimeSpanExtensions
{
    extension(TimeSpan ts)
    {
        /// <summary>
        /// Return a new <see cref="TimeSpan"/> with a random signed adjustment applied.
        /// </summary>
        /// <param name="maximum">
        /// The maximum signed adjustment. Positive values add time; negative values subtract time.
        /// </param>
        /// <param name="random">
        /// An optional Random instance to use for generating the random amount of time to add to the instance.
        /// If null, <see cref="Random.Shared"/> will be used.
        /// </param>
        /// <returns>A new <see cref="TimeSpan"/> with the random adjustment applied.</returns>
        /// <remarks>
        /// <para>
        /// If the maximum <see cref="TimeSpan"/> is less than <see cref="TimeSpan.Zero"/>, a random
        /// amount of time will be subtracted from the original <see cref="TimeSpan"/>.
        /// </para>
        /// <para>
        /// If the TimeSpan is <see cref="Timeout.InfiniteTimeSpan"/>, no additional time will be added
        /// and the original <see cref="TimeSpan"/> will be returned.
        /// </para>
        /// </remarks>
        public TimeSpan AddRandom(TimeSpan maximum, Random random = null)
        {
            if (ts == Timeout.InfiniteTimeSpan)
                return ts; // no random addition for infinite timeouts

            random ??= Random.Shared;

            double randomValue = random.NextDouble() * maximum.TotalMilliseconds;
            return ts + TimeSpan.FromMilliseconds(randomValue);
        }

        /// <summary>
        /// Return a new <see cref="TimeSpan"/> sampled from a jitter window whose total width
        /// is the original duration's magnitude multiplied by the specified jitter factor.
        /// </summary>
        /// <param name="jitterFactor">
        /// A value between 0.0 and 1.0 defining the total jitter window as a fraction
        /// of the original duration's magnitude.
        /// </param>
        /// <param name="direction">The placement of the jitter window relative to the original duration.</param>
        /// <param name="random">
        /// An optional Random instance to use for generating the jitter.
        /// If null, <see cref="Random.Shared"/> will be used.
        /// </param>
        /// <returns>A new <see cref="TimeSpan"/> with the added jitter.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="jitterFactor"/> is not between 0.0 and 1.0, or
        /// <paramref name="direction"/> is not a defined jitter direction.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Both directions split the jitter window equally above and below the original duration.
        /// Positive-only and negative-only directions place the full window on the selected side.
        /// </para>
        /// <para>
        /// If the TimeSpan is <see cref="Timeout.InfiniteTimeSpan"/>, no jitter will be added
        /// and the original <see cref="TimeSpan"/> will be returned.
        /// </para>
        /// </remarks>
        public TimeSpan AddJitter(double jitterFactor, JitterDirection direction = JitterDirection.Both, Random random = null)
        {
            if (double.IsNaN(jitterFactor) || jitterFactor is < 0.0 or > 1.0)
                throw new ArgumentOutOfRangeException(nameof(jitterFactor), "Jitter factor must be between 0.0 and 1.0");

            if (direction is not (JitterDirection.Both or JitterDirection.PositiveOnly or JitterDirection.NegativeOnly))
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown jitter direction.");

            if (ts == Timeout.InfiniteTimeSpan)
                return ts; // no jitter for infinite timeouts

            random ??= Random.Shared;

            double jitter = Math.Abs(ts.TotalMilliseconds) * jitterFactor;
            if (direction == JitterDirection.Both)
                jitter /= 2;

            double min = direction is JitterDirection.Both or JitterDirection.NegativeOnly
                ? ts.TotalMilliseconds - jitter
                : ts.TotalMilliseconds;

            double max = direction is JitterDirection.Both or JitterDirection.PositiveOnly
                ? ts.TotalMilliseconds + jitter
                : ts.TotalMilliseconds;

            double randomValue = random.NextDouble() * (max - min) + min;

            return TimeSpan.FromMilliseconds(randomValue);
        }
    }
}
