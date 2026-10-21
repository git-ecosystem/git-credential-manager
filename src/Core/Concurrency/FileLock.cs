using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager.Concurrency;

/// <summary>
/// A cross-process lock backed by an exclusively opened local file.
/// </summary>
/// <remarks>
/// <para>
/// Requires a filesystem that supports exclusive file sharing and .NET file locking
/// to be enabled. All participants must use the same protocol and must not unlink
/// an active lock file.
/// </para>
/// <para>
/// The owning process ID is written to the lock file, which can be useful for diagnostics.
/// </para>
/// <para>
/// Finite timeouts must be non-negative and no greater than 4,294,967,294 milliseconds,
/// the underlying wait timer's limit. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.
/// </para>
/// </remarks>
public sealed class FileLock : ILock
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan MaximumTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1d);

    private readonly IFileSystem _fs;
    private readonly string _path;

    // Sync protects access to _stream and _disposed.
    // It is also used to ensure that the lock is not disposed while an acquisition is in progress.
    private readonly Lock _sync = new();

    // Gate serializes acquisitions for the same instance in this process.
    // It is released when the lock is released or disposed.
    private readonly SemaphoreSlim _gate = new(1, 1);

    // This token source is cancelled when the lock is disposed, to trigger any in-flight acquisitions.
    private readonly CancellationTokenSource _disposal = new();
    private bool _disposed;

    // Holds the file stream for the lock file while the lock is held. Null if the lock is not held.
    private Stream _stream;

    public FileLock(IFileSystem fs, string absolutePath)
    {
        EnsureArgument.NotNull(fs, nameof(fs));
        EnsureArgument.NotNullOrWhiteSpace(absolutePath, nameof(absolutePath));
        if (!Path.IsPathFullyQualified(absolutePath))
        {
            throw new ArgumentException("Lock file path must be absolute.", nameof(absolutePath));
        }

        _fs = fs;
        _path = Path.GetFullPath(absolutePath);
    }

    public bool IsHeld
    {
        get
        {
            lock (_sync)
            {
                return _stream is not null;
            }
        }
    }

    public async Task<bool> TryAcquireAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        if (timeout != Timeout.InfiniteTimeSpan && (timeout < TimeSpan.Zero || timeout > MaximumTimeout))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout),
                $"Timeout must be between zero and {MaximumTimeout}, or infinite.");
        }

        // Start a stopwatch to measure elapsed time for the timeout.
        // The caller's provider timeout must cover the entire end-to-end acquisition time, including
        // waiting for the gate (to serialise acquisitions for the same instance in this process), and
        // waiting for the lock file to be available.
        var sw = Stopwatch.StartNew();

        // Create a new cancellation token source that will be cancelled if either the caller's token is
        // cancelled or the lock is disposed. Disposal will cause any in-flight acquisitions to be cancelled.
        CancellationTokenSource cts;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ct.ThrowIfCancellationRequested();
            cts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposal.Token);
        }

        using (cts)
        {
            bool entered = false;
            bool acquired = false;
            try
            {
                // Try and enter the gate. If another caller for this instance already holds the lock
                // we will wait for them to release it, or timeout and fail acquisition.
                entered = await _gate.WaitAsync(sw.GetRemaining(timeout), cts.Token).ConfigureAwait(false);
                if (!entered)
                {
                    // We failed to enter the gate within the timeout, so we cannot acquire the lock.
                    // Check if we may have been disposed/cancelled while waiting for the gate.
                    lock (_sync)
                    {
                        ObjectDisposedException.ThrowIf(_disposed, this);
                        ct.ThrowIfCancellationRequested();
                        return false;
                    }
                }

                // We are now the only caller for this instance that can acquire the lock.
                // Let's try to open the lock file exclusively. If we fail, we will wait
                // a short time and retry until the timeout is reached.
                while (true)
                {
                    lock (_sync)
                    {
                        ObjectDisposedException.ThrowIf(_disposed, this);
                        ct.ThrowIfCancellationRequested();

                        // If we have been given a zero timeout we want to try to acquire the lock
                        // at least **once**. Otherwise, check for the remaining time before trying.
                        if (timeout != TimeSpan.Zero && sw.GetRemaining(timeout) == TimeSpan.Zero)
                        {
                            return false;
                        }

                        acquired = TryOpen(ct, out Stream stream);
                        if (acquired)
                        {
                            // We got the lock!
                            _stream = stream;
                            return true;
                        }
                    }

                    // We did not manage to acquire the lock, so we will wait a short time and retry.
                    // Do not wait longer than the remaining timeout!
                    TimeSpan remaining = sw.GetRemaining(timeout);
                    if (remaining == TimeSpan.Zero)
                    {
                        return false;
                    }

                    // Calculate how long we should wait before retrying.
                    // Add a small amount of jitter to avoid lock contention storms.
                    TimeSpan delay = RetryDelay.AddJitter(0.1);
                    if (remaining != Timeout.InfiniteTimeSpan && remaining < delay)
                    {
                        // Shorten the delay to the remaining time if we are close to the timeout.
                        // The next iteration will check the remaining time (necessarily less than
                        // what is left now) and return false indicating we have timed out.
                        delay = remaining;
                    }

                    // Wait before retrying!
                    await Task.Delay(delay, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (_disposal.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(FileLock));
            }
            catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
            {
                throw new OperationCanceledException("Lock acquisition was canceled.", ex, ct);
            }
            finally
            {
                // If we entered the gate but did **not** acquire the lock, we must release the gate
                // so that other callers can try to acquire the lock.
                // If we **did** acquire the lock we should **not** release the gate because the lock
                // is still held, and we want to prevent other callers from acquiring it.
                if (entered && !acquired)
                {
                    _gate.Release();
                }
            }
        }
    }

    public void Release()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ReleaseCore();
        }
    }

    public void Dispose()
    {
        bool disposing = false;
        try
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                disposing = true;
                ReleaseCore();
            }
        }
        finally
        {
            if (disposing)
            {
                // In-flight acquisitions still use these synchronization objects.
                _disposal.Cancel();
            }
        }
    }

    private bool TryOpen(CancellationToken ct, out Stream stream)
    {
        Debug.Assert(_sync.IsHeldByCurrentThread);

        try
        {
            // This flag combination also makes .NET revalidate the file identity on Unix.
            stream = _fs.OpenFileStream(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.None, FileOptions.DeleteOnClose);
        }
        catch (IOException ex) when (IsContention(ex))
        {
            // Another process holds the lock!
            stream = null;
            return false;
        }

        try
        {
            int pid = Environment.ProcessId;
            byte[] bytes = Encoding.UTF8.GetBytes($"{pid}\n");
            stream.Position = 0;
            stream.SetLength(0); // remove any previous content
            stream.Write(bytes); // write our PID
            stream.Flush(); // ensure the data is written to disk

            // One last check to ensure we have not been disposed or cancelled while writing to the lock file.
            ObjectDisposedException.ThrowIf(_disposed, this);
            ct.ThrowIfCancellationRequested();

            // Finally we have acquired the lock and written our PID to the file!
            // We can return true to indicate success.
            return true;
        }
        catch (Exception)
        {
            // Something bad happened while writing to the lock file that we have open.
            // Perhaps the disk is full, or the file system is read-only.
            // We cannot hold the lock in this state, so dispose of stream and surface
            // the exception to the caller.
            stream?.Dispose();
            stream = null;
            throw;
        }
    }

    private void ReleaseCore()
    {
        if (_stream is null)
        {
            // Nothing to do - we never acquired the lock (or has already been released).
            return;
        }

        try
        {
            // Disposing the stream will close the file handle, release the lock, and
            // automatically delete the file since we opened it with DeleteOnClose.
            _stream.Dispose();
        }
        finally
        {
            _stream = null;

            // Let others pass the gate so they can try to acquire the lock now it's released.
            _gate.Release();
        }
    }

    private static bool IsContention(IOException ex)
    {
        if (OperatingSystem.IsWindows())
            return Interop.Windows.Native.Win32Error.FromHResult(ex.HResult) ==
                   Interop.Windows.Native.Win32Error.SharingViolation;

        if (OperatingSystem.IsMacOS())
            return ex.HResult == Interop.MacOS.Native.Errno.EWOULDBLOCK;

        if (OperatingSystem.IsLinux())
            return ex.HResult == Interop.Linux.Native.Errno.EWOULDBLOCK;

        throw new PlatformNotSupportedException();
    }
}
