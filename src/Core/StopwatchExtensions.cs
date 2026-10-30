using System;
using System.Diagnostics;
using System.Threading;

namespace GitCredentialManager;

public static class StopwatchExtensions
{
    extension(Stopwatch sw)
    {
        /// <summary>
        /// Gets the remaining time from a total time span, based on the elapsed time of the stopwatch.
        /// </summary>
        /// <param name="total">The total time span to calculate the remaining time from.</param>
        /// <returns>The remaining time span.</returns>
        /// <remarks>
        /// <para>
        /// If the total time span is <see cref="Timeout.InfiniteTimeSpan"/>, the method will return
        /// <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </para>
        /// <para>
        /// If the elapsed time of the stopwatch exceeds the total time span, the method will return
        /// <see cref="TimeSpan.Zero"/>.
        /// </para>
        /// </remarks>
        public TimeSpan GetRemaining(TimeSpan total)
        {
            if (total == TimeSpan.Zero) // never any time remaining
            {
                return TimeSpan.Zero;
            }

            if (total == Timeout.InfiniteTimeSpan) // always time remaining
            {
                return total;
            }

            TimeSpan remaining = total - sw.Elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
