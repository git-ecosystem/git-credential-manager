using System;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Concurrency;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests.Concurrency;

public class ILockTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task LockExtensions_DelegateToTheTimedAcquisitionPrimitive()
    {
        var mutex = new Mock<ILock>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        mutex.Setup(x => x.TryAcquireAsync(Timeout.InfiniteTimeSpan, cancellation.Token)).ReturnsAsync(true);
        mutex.Setup(x => x.TryAcquireAsync(TimeSpan.Zero, default)).ReturnsAsync(false);
        mutex.Setup(x => x.TryAcquireAsync(TestTimeout, cancellation.Token)).ReturnsAsync(false);

        await mutex.Object.AcquireAsync(cancellation.Token);
        Assert.False(mutex.Object.TryAcquire());
        await Assert.ThrowsAsync<TimeoutException>(() => mutex.Object.AcquireAsync(TestTimeout, cancellation.Token));

        mutex.VerifyAll();
    }
}
