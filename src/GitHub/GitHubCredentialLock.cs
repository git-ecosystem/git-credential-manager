using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;

namespace GitHub;

internal sealed class GitHubCredentialLock : IDisposable
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
    private readonly Stream _stream;
    private readonly SemaphoreSlim _gate;

    private GitHubCredentialLock(Stream stream, SemaphoreSlim gate)
    {
        _stream = stream;
        _gate = gate;
    }

    public static async Task<GitHubCredentialLock> AcquireAsync(IFileSystem fileSystem, string service)
    {
        string directory = Path.Combine(fileSystem.UserDataDirectoryPath, "github", "locks");
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(service)));
        string path = Path.Combine(directory, $"{key}.lock");
        SemaphoreSlim gate = Gates.GetOrAdd(path, _ => new SemaphoreSlim(1));
        if (!await gate.WaitAsync(Timeout))
        {
            throw new TimeoutException("Timed out waiting for another GitHub credential operation. Try again.");
        }

        try
        {
            fileSystem.CreateDirectory(directory);
            var elapsed = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    Stream stream = fileSystem.OpenFileStream(
                        path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    return new GitHubCredentialLock(stream, gate);
                }
                catch (IOException exception) when (IsSharingViolation(exception))
                {
                    if (elapsed.Elapsed >= Timeout)
                    {
                        throw new TimeoutException(
                            "Timed out waiting for another GitHub credential operation. Try again.", exception);
                    }
                    await Task.Delay(100);
                }
            }
        }
        catch
        {
            gate.Release();
            throw;
        }
    }

    private static bool IsSharingViolation(IOException exception)
    {
        int error = exception.HResult & 0xFFFF;
        // Unix reports EWOULDBLOCK, not the Windows sharing/lock violation codes.
        return PlatformUtils.IsWindows()
            ? error is 32 or 33
            : error == (PlatformUtils.IsMacOS() ? 35 : 11);
    }

    public void Dispose()
    {
        try
        {
            // Keep the lock file: deleting it can let another process lock a different inode.
            _stream.Dispose();
        }
        finally
        {
            _gate.Release();
        }
    }
}
