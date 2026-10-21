using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Concurrency;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests.Concurrency;

public class FileLockTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
    private static readonly string LockPath = Path.Combine(Path.GetTempPath(), "gcm-test.lock");

    [Fact]
    public void FileLock_ConstructorAndUntakenDispose_DoNotTouchTheFile()
    {
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);

        Assert.Throws<ArgumentException>(() => new FileLock(fs.Object, "relative.lock"));
        var mutex = new FileLock(fs.Object, LockPath);
        mutex.Release();
        mutex.Dispose();
        mutex.Dispose();

        fs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FileLock_AcquireReleaseAndReacquire_WritesPidAndDeletesFile()
    {
        var fs = new TestFileSystem();
        using ILock mutex = new FileLock(fs, LockPath);
        string expected = Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + "\n";

        await mutex.AcquireAsync();
        Assert.True(mutex.IsHeld);
        Assert.Equal(expected, Encoding.UTF8.GetString(fs.Files[LockPath]));

        mutex.Release();
        mutex.Release();
        Assert.False(mutex.IsHeld);
        Assert.False(fs.Files.ContainsKey(LockPath));

        Assert.True(mutex.TryAcquire());
        Assert.Equal(expected, Encoding.UTF8.GetString(fs.Files[LockPath]));
        mutex.Dispose();
        Assert.False(mutex.IsHeld);
        Assert.False(fs.Files.ContainsKey(LockPath));
    }

    [Fact]
    public async Task FileLock_SameInstanceWaits_AndCanReleaseOnAnotherThread()
    {
        using ILock mutex = new FileLock(new TestFileSystem(), LockPath);
        await mutex.AcquireAsync();

        Assert.False(mutex.TryAcquire());
        Task waiting = mutex.AcquireAsync();
        Assert.False(waiting.IsCompleted);

        await Task.Run(mutex.Release);
        await waiting.WaitAsync(TestTimeout);
        Assert.True(mutex.IsHeld);
    }

    [Fact]
    public async Task FileLock_ConcurrentCalls_DoNotOverlapOwnership()
    {
        using ILock mutex = new FileLock(new TestFileSystem(), LockPath);
        int owners = 0;

        Task[] workers = Enumerable.Range(0, 8).Select(async _ =>
        {
            for (int i = 0; i < 10; i++)
            {
                await mutex.AcquireAsync();
                try
                {
                    Assert.Equal(1, Interlocked.Increment(ref owners));
                    await Task.Yield();
                }
                finally
                {
                    Interlocked.Decrement(ref owners);
                    mutex.Release();
                }
            }
        }).ToArray();

        await Task.WhenAll(workers).WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task FileLock_TimeoutIncludesTheLocalQueue()
    {
        using ILock mutex = new FileLock(new TestFileSystem(), LockPath);
        await mutex.AcquireAsync();

        TimeSpan timeout = TimeSpan.FromMilliseconds(50);
        Assert.False(await mutex.TryAcquireAsync(timeout).WaitAsync(TestTimeout));
        Assert.True(mutex.IsHeld);

        mutex.Release();
        Assert.True(mutex.TryAcquire());
    }

    [Fact]
    public async Task FileLock_CancelQueuedAcquisition_DoesNotReleaseTheOwner()
    {
        using ILock mutex = new FileLock(new TestFileSystem(), LockPath);
        using var cancellation = new CancellationTokenSource();
        await mutex.AcquireAsync();

        Task waiting = mutex.AcquireAsync(cancellation.Token);
        cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TestTimeout));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.True(mutex.IsHeld);

        mutex.Release();
        Assert.True(mutex.TryAcquire());
    }

    [Fact]
    public async Task FileLock_RetriesOnlySharingContention()
    {
        var backing = new TestFileSystem();
        int attempts = 0;
        Mock<IFileSystem> fs = MockFileSystem(() =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                throw SharingViolation();
            }

            return Open(backing);
        });
        using ILock mutex = new FileLock(fs.Object, LockPath);

        await mutex.AcquireAsync(TestTimeout);
        Assert.Equal(2, attempts);
        Assert.True(mutex.IsHeld);
    }

    [Fact]
    public async Task FileLock_InterprocessTimeout_DoesNotRetainTheAcquisitionGate()
    {
        bool busy = true;
        var backing = new TestFileSystem();
        Mock<IFileSystem> fs = MockFileSystem(() => busy ? throw SharingViolation() : Open(backing));
        using ILock mutex = new FileLock(fs.Object, LockPath);
        TimeSpan timeout = TimeSpan.FromMilliseconds(50);

        var stopwatch = Stopwatch.StartNew();
        Assert.False(await mutex.TryAcquireAsync(timeout).WaitAsync(TestTimeout));
        Assert.True(stopwatch.Elapsed >= timeout);
        Assert.False(mutex.IsHeld);

        busy = false;
        Assert.True(mutex.TryAcquire());
    }

    [Fact]
    public async Task FileLock_OpenFailures_AreSurfacedAndAllowRetry()
    {
        Exception[] failures =
        {
            new IOException("Disk failure."),
            new UnauthorizedAccessException("Access denied."),
            new DirectoryNotFoundException("Missing parent."),
        };
        foreach (Exception failure in failures)
        {
            bool fail = true;
            var backing = new TestFileSystem();
            Mock<IFileSystem> fs = MockFileSystem(() => fail ? throw failure : Open(backing));
            using ILock mutex = new FileLock(fs.Object, LockPath);

            Assert.Same(failure, await Record.ExceptionAsync(() => mutex.AcquireAsync(TestTimeout)));
            Assert.False(mutex.IsHeld);
            fs.Verify(x => x.OpenFileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.None, FileOptions.DeleteOnClose), Times.Once);

            fail = false;
            Assert.True(mutex.TryAcquire());
        }
    }

    [Fact]
    public async Task FileLock_CancelInterprocessWait_AllowsLaterAcquisition()
    {
        bool busy = true;
        var backing = new TestFileSystem();
        Mock<IFileSystem> fs = MockFileSystem(() => busy ? throw SharingViolation() : Open(backing));
        using ILock mutex = new FileLock(fs.Object, LockPath);
        using var cancellation = new CancellationTokenSource();

        Task waiting = mutex.AcquireAsync(cancellation.Token);
        cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TestTimeout));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.False(mutex.IsHeld);

        busy = false;
        Assert.True(mutex.TryAcquire());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FileLock_Dispose_EndsHeldOrInterprocessAndQueuedWaits(bool held)
    {
        var backing = new TestFileSystem();
        Mock<IFileSystem> fs = MockFileSystem(() => held ? Open(backing) : throw SharingViolation());
        ILock mutex = new FileLock(fs.Object, LockPath);
        if (held)
        {
            await mutex.AcquireAsync();
        }

        Task first = mutex.AcquireAsync();
        Task second = mutex.AcquireAsync();

        mutex.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => first.WaitAsync(TestTimeout));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => second.WaitAsync(TestTimeout));
        Assert.False(mutex.IsHeld);
        Assert.Throws<ObjectDisposedException>(mutex.Release);
        Assert.Throws<ObjectDisposedException>(() => mutex.TryAcquire());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => mutex.AcquireAsync());
    }

    [Fact]
    public async Task FileLock_PreCanceledAcquisition_DoesNotTouchTheFile()
    {
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        using ILock mutex = new FileLock(fs.Object, LockPath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mutex.AcquireAsync(cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.False(mutex.IsHeld);
        fs.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("2147483647\n")]
    [InlineData("unrecognized data")]
    [InlineData("GCM_FILE_LOCK 1\n123\n")]
    [InlineData("GCM_FILE_LOCK 2\n123\n1\n1\n0123456789abcdef0123456789abcdef\n")]
    public async Task FileLock_UnlockedLeftover_IsReplacedWithPid(string contents)
    {
        var fs = new TestFileSystem();
        fs.Files[LockPath] = Encoding.UTF8.GetBytes(contents);
        using ILock mutex = new FileLock(fs, LockPath);

        await mutex.AcquireAsync();
        Assert.True(mutex.IsHeld);
        Assert.Equal(Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + "\n",
            Encoding.UTF8.GetString(fs.Files[LockPath]));
    }

    [Fact]
    public async Task FileLock_UnlockedFileWithLivePid_CanBeReclaimed()
    {
        var fs = new TestFileSystem();
        using ILock first = new FileLock(fs, LockPath);
        await first.AcquireAsync();
        byte[] abandonedContents = fs.Files[LockPath].ToArray();
        first.Release();
        fs.Files[LockPath] = abandonedContents;

        using ILock second = new FileLock(fs, LockPath);
        Assert.True(second.TryAcquire());
        Assert.Equal(abandonedContents, fs.Files[LockPath]);
    }

    [Fact]
    public async Task FileLock_PreviousContents_AreNotRead()
    {
        var backing = new TestFileSystem();
        using var stream = new UnreadableFileStream(backing);
        stream.Write(new byte[1024]);
        stream.Position = 0;
        Mock<IFileSystem> fs = MockFileSystem(() => stream);
        using ILock mutex = new FileLock(fs.Object, LockPath);

        await mutex.AcquireAsync();
        Assert.True(mutex.IsHeld);
        Assert.Equal(Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + "\n",
            Encoding.UTF8.GetString(backing.Files[LockPath]));
    }

    [Fact]
    public async Task FileLock_CancelDuringInitialization_DoesNotPublishOwnership()
    {
        var backing = new TestFileSystem();
        using var cancellation = new CancellationTokenSource();
        bool cancel = true;
        Mock<IFileSystem> fs = MockFileSystem(() => new CallbackFileStream(backing, () =>
        {
            if (cancel)
            {
                cancellation.Cancel();
            }
        }));
        using ILock mutex = new FileLock(fs.Object, LockPath);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mutex.AcquireAsync(cancellation.Token));
        Assert.False(mutex.IsHeld);
        Assert.False(backing.Files.ContainsKey(LockPath));
        cancel = false;
        Assert.True(mutex.TryAcquire());
    }

    [Fact]
    public async Task FileLock_PidFlushFailure_IsNotMistakenForOpenContention()
    {
        var backing = new TestFileSystem();
        var error = SharingViolation();
        bool fail = true;
        Mock<IFileSystem> fs = MockFileSystem(() => new CallbackFileStream(backing, () =>
        {
            if (fail)
            {
                throw error;
            }
        }));
        using ILock mutex = new FileLock(fs.Object, LockPath);

        Assert.Same(error, await Record.ExceptionAsync(() => mutex.AcquireAsync(TestTimeout)));
        Assert.False(mutex.IsHeld);
        Assert.False(backing.Files.ContainsKey(LockPath));
        fail = false;
        Assert.True(mutex.TryAcquire());
    }

    [Fact]
    public async Task FileLock_DisposeDuringInitialization_DoesNotPublishOwnership()
    {
        var backing = new TestFileSystem();
        ILock mutex = null;
        Mock<IFileSystem> fs = MockFileSystem(() => new CallbackFileStream(backing, () => mutex.Dispose()));
        mutex = new FileLock(fs.Object, LockPath);

        await Assert.ThrowsAsync<ObjectDisposedException>(() => mutex.AcquireAsync());
        Assert.False(mutex.IsHeld);
        Assert.False(backing.Files.ContainsKey(LockPath));
    }

    [Fact]
    public async Task FileLock_LongTimeout_CanBeCanceled()
    {
        using ILock mutex = new FileLock(new TestFileSystem(), LockPath);
        using var cancellation = new CancellationTokenSource();
        await mutex.AcquireAsync();

        Task waiting = mutex.TryAcquireAsync(TimeSpan.FromMilliseconds(uint.MaxValue - 1d), cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TestTimeout));
    }

    [Fact]
    public async Task FileLock_InvalidTimeout_DoesNotAffectTheOwnerOrLaterAcquisitions()
    {
        using ILock mutex = new FileLock(new TestFileSystem(), LockPath);
        await mutex.AcquireAsync();
        TimeSpan[] invalidTimeouts =
        {
            TimeSpan.FromTicks(-1),
            TimeSpan.FromMilliseconds(uint.MaxValue - 1d) + TimeSpan.FromTicks(1),
            TimeSpan.MaxValue,
        };

        foreach (TimeSpan timeout in invalidTimeouts)
        {
            var error = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                mutex.TryAcquireAsync(timeout));
            Assert.Equal("timeout", error.ParamName);
            Assert.True(mutex.IsHeld);
        }

        mutex.Release();
        Assert.True(mutex.TryAcquire());
    }

    [Fact]
    public async Task FileLock_LocalFilesystem_MultipleInstancesExcludeAndDelete()
    {
        string directory = Directory.CreateTempSubdirectory("gcm-lock-tests-").FullName;
        string path = Path.Combine(directory, "mutex");
        var fs = new Mock<FileSystem> { CallBase = true }.Object;
        int owners = 0;
        try
        {
            using ILock owner = new FileLock(fs, path);
            using ILock contender = new FileLock(fs, path);
            await owner.AcquireAsync();
            Assert.False(contender.TryAcquire());
            Assert.True(File.Exists(path));
            owner.Release();

            Task[] workers = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
            {
                using ILock mutex = new FileLock(fs, path);
                for (int i = 0; i < 10; i++)
                {
                    await mutex.AcquireAsync();
                    try
                    {
                        Assert.Equal(1, Interlocked.Increment(ref owners));
                        await Task.Yield();
                    }
                    finally
                    {
                        Interlocked.Decrement(ref owners);
                        mutex.Release();
                    }
                }
            })).ToArray();
            await Task.WhenAll(workers).WaitAsync(TestTimeout);
            Assert.False(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(directory);
        }
    }

    private static Mock<IFileSystem> MockFileSystem(Func<Stream> open)
    {
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        fs.Setup(x => x.OpenFileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
            FileShare.None, FileOptions.DeleteOnClose)).Returns(open);
        return fs;
    }

    private static Stream Open(IFileSystem fs) =>
        fs.OpenFileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
            FileShare.None, FileOptions.DeleteOnClose);

    private static IOException SharingViolation() =>
        new("The file is locked.", OperatingSystem.IsWindows() ? unchecked((int)0x80070020) :
            OperatingSystem.IsMacOS() ? 35 : 11);

    private sealed class UnreadableFileStream(TestFileSystem fs)
        : TestFileStream(fs, LockPath, deleteOnClose: true)
    {
        public override int Read(Span<byte> buffer) => throw new InvalidOperationException("Lock contents must not be read.");

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("Lock contents must not be read.");
    }

    private sealed class CallbackFileStream(TestFileSystem fs, Action flushed)
        : TestFileStream(fs, LockPath, deleteOnClose: true)
    {
        public override void Flush()
        {
            base.Flush();
            flushed();
        }
    }
}
