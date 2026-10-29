using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.Entra;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra;

public class EntraAuthenticationInteractiveLockTests : IDisposable
{
    private const string Authority = "https://login.microsoftonline.com/tenant-1";

    private readonly TestCommandContext _context = new();
    private readonly string _root;

    public EntraAuthenticationInteractiveLockTests()
    {
        // The lock is a real file, so back the test file system with a real directory.
        _root = Path.Combine(Path.GetTempPath(), $"gcmtest-lock-{Guid.NewGuid():N}");
        _context.FileSystem.UserDataDirectoryPath = Path.Combine(_root, ".gcm");
        Directory.CreateDirectory(_context.FileSystem.UserDataDirectoryPath);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private EntraAuthentication CreateAuth(string clientId = "client-1") =>
        new(_context, new PublicClientConfig { ClientId = clientId });

    [Fact]
    public async Task AcquireInteractiveLockAsync_NotHeld_ReturnsLock()
    {
        var auth = CreateAuth();

        using IDisposable handle = await auth.AcquireInteractiveLockAsync(Authority, CancellationToken.None);

        Assert.NotNull(handle);
    }

    [Fact]
    public async Task AcquireInteractiveLockAsync_HeldByOther_WaitsUntilReleased()
    {
        var auth = CreateAuth();
        IDisposable first = await auth.AcquireInteractiveLockAsync(Authority, CancellationToken.None);
        Assert.NotNull(first);

        Task<IDisposable> second = auth.AcquireInteractiveLockAsync(
            Authority, CancellationToken.None, TimeSpan.FromSeconds(30));

        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.False(second.IsCompleted, "The second caller must wait while the first holds the lock.");

        first.Dispose();

        using IDisposable handle = await second.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.NotNull(handle);
    }

    [Fact]
    public async Task AcquireInteractiveLockAsync_HeldByOtherPastTimeout_ReturnsNull()
    {
        var auth = CreateAuth();
        using IDisposable first = await auth.AcquireInteractiveLockAsync(Authority, CancellationToken.None);
        Assert.NotNull(first);

        IDisposable second = await auth.AcquireInteractiveLockAsync(
            Authority, CancellationToken.None, TimeSpan.FromMilliseconds(100));

        // Locking must never block authentication for good: the caller continues without it.
        Assert.Null(second);
    }

    [Fact]
    public async Task AcquireInteractiveLockAsync_DifferentAuthority_DoesNotWait()
    {
        var auth = CreateAuth();
        using IDisposable first = await auth.AcquireInteractiveLockAsync(Authority, CancellationToken.None);
        Assert.NotNull(first);

        using IDisposable second = await auth.AcquireInteractiveLockAsync(
                "https://login.microsoftonline.com/tenant-2", CancellationToken.None, TimeSpan.FromMilliseconds(100));

        Assert.NotNull(second);
    }

    [Fact]
    public async Task AcquireInteractiveLockAsync_DifferentClientId_DoesNotWait()
    {
        using IDisposable first = await CreateAuth("client-1")
            .AcquireInteractiveLockAsync(Authority, CancellationToken.None);
        Assert.NotNull(first);

        using IDisposable second = await CreateAuth("client-2")
            .AcquireInteractiveLockAsync(Authority, CancellationToken.None, TimeSpan.FromMilliseconds(100));

        Assert.NotNull(second);
    }

    [Fact]
    public async Task AcquireInteractiveLockAsync_Canceled_Throws()
    {
        var auth = CreateAuth();
        using IDisposable first = await auth.AcquireInteractiveLockAsync(Authority, CancellationToken.None);
        Assert.NotNull(first);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            auth.AcquireInteractiveLockAsync(Authority, cts.Token, TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task AcquireInteractiveLockAsync_UserDataDirectoryUnusable_ReturnsNull()
    {
        // A file where the directory should be: the lock file can never be created.
        string blocker = Path.Combine(_root, "blocker");
        File.WriteAllText(blocker, string.Empty);
        _context.FileSystem.UserDataDirectoryPath = Path.Combine(blocker, ".gcm");

        IDisposable handle = await CreateAuth().AcquireInteractiveLockAsync(Authority, CancellationToken.None);

        Assert.Null(handle);
    }
}
