using System;
using System.Threading;
using GitCredentialManager.UI;
using Xunit;

namespace GitCredentialManager.Tests.UI;

public class DispatcherTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void Dispatcher_Shutdown_BeforeRunIsReached_RunReturns()
    {
        var initialized = new ManualResetEventSlim();
        var mayRun = new ManualResetEventSlim();

        // Hold the dispatcher thread between Initialize and Run so we shut down in the
        // window that the application thread can genuinely hit in Program.Main.
        Thread thread = StartDispatcherThread(initialized, () => mayRun.Wait(Timeout));
        Assert.True(initialized.Wait(Timeout));

        Dispatcher.MainThread.Shutdown();
        mayRun.Set();

        Assert.True(thread.Join(Timeout));
    }

    [Fact]
    public void Dispatcher_Shutdown_NoWorkPosted_RunReturns()
    {
        var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(initialized);
        Assert.True(initialized.Wait(Timeout));

        Dispatcher.MainThread.Shutdown();

        Assert.True(thread.Join(Timeout));
    }

    private static Thread StartDispatcherThread(ManualResetEventSlim initialized, Action beforeRun = null)
    {
        // The dispatcher binds to the thread that initializes it and must be run from
        // that same thread, so both have to happen here rather than in the test.
        var thread = new Thread(() =>
        {
            Dispatcher.Initialize();
            initialized.Set();
            beforeRun?.Invoke();
            Dispatcher.MainThread.Run();
        })
        {
            IsBackground = true,
            Name = nameof(StartDispatcherThread),
        };

        thread.Start();
        return thread;
    }
}
