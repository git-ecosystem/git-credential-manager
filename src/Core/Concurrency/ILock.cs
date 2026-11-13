using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager.Concurrency;

/// <summary>
/// Represents a disposable lock with asynchronous acquisition and synchronous release.
/// </summary>
/// <remarks>
/// Releasing the lock allows it to be acquired again. Disposing the lock releases it
/// and prevents further acquisition.
/// </remarks>
public interface ILock : IDisposable
{
    /// <summary>
    /// Returns true if the lock is currently held by this instance, false otherwise.
    /// </summary>
    bool IsHeld { get; }

    /// <summary>
    /// Try to acquire the lock asynchronously with a timeout.
    /// </summary>
    /// <exception cref="OperationCanceledException">Thrown if cancellation is requested before acquisition.</exception>
    /// <param name="timeout">Time period to wait to acquire the lock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes with true if the lock was acquired, false if the timeout was reached.</returns>
    Task<bool> TryAcquireAsync(TimeSpan timeout, CancellationToken ct = default);

    /// <summary>
    /// Release the lock, allowing subsequent acquisition.
    /// Releasing a lock that is not held has no effect.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the lock has been disposed.</exception>
    void Release();
}

public static class LockExtensions
{
    extension (ILock @lock)
    {
        /// <summary>
        /// Acquire the lock asynchronously, waiting until it is available or canceled.
        /// </summary>
        public Task AcquireAsync(CancellationToken ct = default) =>
            @lock.AcquireAsync(Timeout.InfiniteTimeSpan, ct);

        /// <summary>
        /// Try to acquire the lock without waiting for another owner.
        /// </summary>
        public bool TryAcquire() => @lock.TryAcquireAsync(TimeSpan.Zero).GetAwaiter().GetResult();

        /// <summary>
        /// Acquire the lock asynchronously with a timeout.
        /// If the lock is already held by another process, this method will wait
        /// until the lock is available, the timeout is reached, or the cancellation
        /// token is triggered.
        /// </summary>
        /// <exception cref="TimeoutException">Thrown if the timeout is reached before the lock is acquired.</exception>
        /// <exception cref="OperationCanceledException">Thrown if cancellation is requested before acquisition.</exception>
        /// <param name="timeout">Time period to wait to acquire the lock.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes successfully when the lock is acquired.</returns>
        public async Task AcquireAsync(TimeSpan timeout, CancellationToken ct = default)
        {
            if (!await @lock.TryAcquireAsync(timeout, ct).ConfigureAwait(false))
            {
                throw new TimeoutException($"Failed to acquire lock within {timeout.TotalSeconds} seconds.");
            }
        }
    }
}
