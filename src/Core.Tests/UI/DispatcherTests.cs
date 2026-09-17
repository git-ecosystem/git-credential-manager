using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI;
using Xunit;

namespace GitCredentialManager.Tests.UI;

public class DispatcherTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void Dispatcher_Shutdown_BeforeRunIsReached_RunReturnsWithoutStartingMainLoop()
    {
        var mainLoop = new FakeMainLoop();
        var initialized = new ManualResetEventSlim();
        var mayRun = new ManualResetEventSlim();

        // Hold the dispatcher thread between Initialize and Run so we can shut down in
        // the window that the application thread can genuinely hit in Program.Main.
        Thread thread = StartDispatcherThread(mainLoop, initialized, () => mayRun.Wait(Timeout));
        Assert.True(initialized.Wait(Timeout));

        Dispatcher.MainThread.Shutdown();
        mayRun.Set();

        Assert.True(thread.Join(Timeout));
        Assert.False(mainLoop.Initialized);
        Assert.False(mainLoop.Ran);
    }

    [Fact]
    public void Dispatcher_Run_AfterRunHasReturned_Throws()
    {
        var mainLoop = new FakeMainLoop();
        var initialized = new ManualResetEventSlim();
        Exception secondRun = null;

        // Run must be called from the dispatcher thread, so the second call has to be
        // made there too rather than from the test thread.
        var thread = new Thread(() =>
        {
            Dispatcher.Initialize(mainLoop);
            initialized.Set();
            Dispatcher.MainThread.Run();
            secondRun = Record.Exception(() => Dispatcher.MainThread.Run());
        })
        {
            IsBackground = true,
            Name = nameof(Dispatcher_Run_AfterRunHasReturned_Throws),
        };
        thread.Start();

        Assert.True(initialized.Wait(Timeout));
        Dispatcher.MainThread.Shutdown();
        Assert.True(thread.Join(Timeout));

        // Running again once the thread has been released is a programming error, and is
        // distinct from the tolerated race where shutdown beats Run to the dispatcher.
        Assert.IsType<InvalidOperationException>(secondRun);
    }

    [Fact]
    public void Dispatcher_Shutdown_NoWorkPosted_NeverStartsMainLoop()
    {
        var mainLoop = new FakeMainLoop();
        var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));

        Dispatcher.MainThread.Shutdown();

        Assert.True(thread.Join(Timeout));
        Assert.False(mainLoop.Initialized);
        Assert.False(mainLoop.Ran);
    }

    [Fact]
    public async Task Dispatcher_InvokeAsync_FirstJob_StartsMainLoopAndRunsWork()
    {
        var mainLoop = new FakeMainLoop();
        var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        Task<int> task = dispatcher.InvokeAsync(_ => 42);

        Assert.Equal(42, await task.WaitAsync(Timeout));
        Assert.True(mainLoop.Initialized);
        Assert.True(mainLoop.Ran);

        dispatcher.Shutdown();
        Assert.True(thread.Join(Timeout));
    }

    [Fact]
    public async Task Dispatcher_Shutdown_MainLoopRunning_StopsItByCancellation()
    {
        var mainLoop = new FakeMainLoop();
        var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        await dispatcher.InvokeAsync(_ => { }).WaitAsync(Timeout);

        dispatcher.Shutdown();

        // Cancelling the token handed to Run is the only shutdown signal the loop gets.
        Assert.True(thread.Join(Timeout));
        Assert.True(mainLoop.RunWasCancelled);
    }

    [Fact]
    public void Dispatcher_Shutdown_WorkStillPending_AbandonsIt()
    {
        var mainLoop = new FakeMainLoop { PumpPostedWork = false };
        var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        Task task = dispatcher.InvokeAsync(_ => { });
        Assert.True(SpinWait.SpinUntil(() => mainLoop.Ran, Timeout));

        dispatcher.Shutdown();
        Assert.True(thread.Join(Timeout));

        // Outstanding work is dropped rather than drained. Draining instead would hang
        // shutdown whenever a window is left open with nobody awaiting it.
        Assert.False(task.IsCompleted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Dispatcher_InvokeAsync_MainLoopFails_FaultsPendingAndFutureJobs(bool failInitialize)
    {
        var failure = new InvalidOperationException("The main loop failed.");
        Action fail = () => throw failure;
        var mainLoop = new FakeMainLoop
        {
            BeforeInitialize = failInitialize ? fail : null,
            BeforeRun = failInitialize ? null : fail,
        };
        using var initialized = new ManualResetEventSlim();
        using var mayRun = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized, () => mayRun.Wait(Timeout));
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            bool workRan = false;
            Task[] tasks =
            {
                dispatcher.InvokeAsync(_ => { workRan = true; }),
                dispatcher.InvokeAsync(_ => { workRan = true; return 42; }),
                dispatcher.InvokeAsync(_ => { workRan = true; return Task.CompletedTask; }),
                dispatcher.InvokeAsync(_ => { workRan = true; return Task.FromResult(42); }),
            };
            mayRun.Set();

            foreach (Task task in tasks)
            {
                Assert.Same(failure,
                    await Assert.ThrowsAsync<InvalidOperationException>(() => task.WaitAsync(Timeout)));
            }

            Task future = dispatcher.InvokeAsync(_ => { workRan = true; });
            Assert.Same(failure,
                await Assert.ThrowsAsync<InvalidOperationException>(() => future.WaitAsync(Timeout)));
            Assert.False(workRan);
        }
        finally
        {
            mayRun.Set();
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Fact]
    public async Task Dispatcher_InvokeAsync_PostFailsDuringHandoff_FaultsAllJobs()
    {
        var failure = new InvalidOperationException("Posting to the main loop failed.");
        int postCount = 0;
        var mainLoop = new FakeMainLoop
        {
            BeforePost = () =>
            {
                if (Interlocked.Increment(ref postCount) == 2)
                {
                    throw failure;
                }
            },
        };
        using var initialized = new ManualResetEventSlim();
        using var mayRun = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized, () => mayRun.Wait(Timeout));
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            bool workRan = false;
            Task[] tasks =
            {
                dispatcher.InvokeAsync(_ => { workRan = true; }),
                dispatcher.InvokeAsync(_ => { workRan = true; }),
                dispatcher.InvokeAsync(_ => { workRan = true; }),
            };
            mayRun.Set();

            // Cover work already posted, the rejected post, and work not yet posted.
            foreach (Task task in tasks)
            {
                Assert.Same(failure,
                    await Assert.ThrowsAsync<InvalidOperationException>(() => task.WaitAsync(Timeout)));
            }

            Task future = dispatcher.InvokeAsync(_ => { workRan = true; });
            Assert.Same(failure,
                await Assert.ThrowsAsync<InvalidOperationException>(() => future.WaitAsync(Timeout)));
            Assert.False(workRan);
            Assert.False(mainLoop.Ran);
            Assert.Equal(2, postCount);
        }
        finally
        {
            mayRun.Set();
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Fact]
    public async Task Dispatcher_InvokeAsync_PostFailsWhileRunning_SkipsFaultedCallbacks()
    {
        var failure = new InvalidOperationException("Posting to the running main loop failed.");
        using var initialized = new ManualResetEventSlim();
        using var running = new ManualResetEventSlim();
        using var mayPump = new ManualResetEventSlim();
        using var callbackPumped = new ManualResetEventSlim();
        int postCount = 0;
        var mainLoop = new FakeMainLoop
        {
            BeforePost = () =>
            {
                if (Interlocked.Increment(ref postCount) == 2)
                {
                    throw failure;
                }
            },
            BeforeRun = () =>
            {
                running.Set();
                Assert.True(mayPump.Wait(Timeout));
            },
            AfterWork = () => callbackPumped.Set(),
        };

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            bool workRan = false;
            Task pending = dispatcher.InvokeAsync(_ => { workRan = true; });
            Assert.True(running.Wait(Timeout));

            // Bound the submission itself: a posting caller must not wait for shutdown
            // or throw synchronously instead of returning the faulted task.
            Task<Task> submission = Task.Factory.StartNew(
                () => dispatcher.InvokeAsync(_ => { workRan = true; }),
                CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            Task rejected = await submission.WaitAsync(Timeout);

            Assert.Same(failure,
                await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(Timeout)));
            Assert.Same(failure,
                await Assert.ThrowsAsync<InvalidOperationException>(() => rejected.WaitAsync(Timeout)));

            Task future = dispatcher.InvokeAsync(_ => { workRan = true; });
            Assert.Same(failure,
                await Assert.ThrowsAsync<InvalidOperationException>(() => future.WaitAsync(Timeout)));

            mayPump.Set();
            Assert.True(callbackPumped.Wait(Timeout));
            Assert.False(workRan);
        }
        finally
        {
            mayPump.Set();
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Fact]
    public async Task Dispatcher_InvokeAsync_ConcurrentPostsFail_PreservesFirstFailure()
    {
        var firstFailure = new InvalidOperationException("The first post failed.");
        var secondFailure = new InvalidOperationException("The second post failed.");
        using var initialized = new ManualResetEventSlim();
        using var running = new ManualResetEventSlim();
        using var firstPosting = new ManualResetEventSlim();
        using var secondPosting = new ManualResetEventSlim();
        using var mayFailFirst = new ManualResetEventSlim();
        using var mayFailSecond = new ManualResetEventSlim();
        int postCount = 0;
        var mainLoop = new FakeMainLoop
        {
            PumpPostedWork = false,
            BeforeRun = () => running.Set(),
            BeforePost = () =>
            {
                switch (Interlocked.Increment(ref postCount))
                {
                    case 2:
                        firstPosting.Set();
                        Assert.True(mayFailFirst.Wait(Timeout));
                        throw firstFailure;
                    case 3:
                        secondPosting.Set();
                        Assert.True(mayFailSecond.Wait(Timeout));
                        throw secondFailure;
                }
            },
        };

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            Task pending = dispatcher.InvokeAsync(_ => { });
            Assert.True(running.Wait(Timeout));

            Task<Task> firstSubmission = Task.Factory.StartNew(
                () => dispatcher.InvokeAsync(_ => { }),
                CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            Assert.True(firstPosting.Wait(Timeout));

            Task<Task> secondSubmission = Task.Factory.StartNew(
                () => dispatcher.InvokeAsync(_ => { }),
                CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            Assert.True(secondPosting.Wait(Timeout));

            mayFailFirst.Set();
            Task first = await firstSubmission.WaitAsync(Timeout);
            Assert.Same(firstFailure,
                await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(Timeout)));
            Assert.Same(firstFailure,
                await Assert.ThrowsAsync<InvalidOperationException>(() => first.WaitAsync(Timeout)));

            mayFailSecond.Set();
            Task second = await secondSubmission.WaitAsync(Timeout);
            Assert.Same(firstFailure,
                await Assert.ThrowsAsync<InvalidOperationException>(() => second.WaitAsync(Timeout)));

            Task future = dispatcher.InvokeAsync(_ => { });
            Assert.Same(firstFailure,
                await Assert.ThrowsAsync<InvalidOperationException>(() => future.WaitAsync(Timeout)));
        }
        finally
        {
            mayFailFirst.Set();
            mayFailSecond.Set();
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    private static Thread StartDispatcherThread(
        IMainLoop mainLoop, ManualResetEventSlim initialized, Action beforeRun = null)
    {
        // The dispatcher binds to the thread that initializes it and must be run from
        // that same thread, so both have to happen here rather than in the test.
        var thread = new Thread(() =>
        {
            Dispatcher.Initialize(mainLoop);
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

    private sealed class FakeMainLoop : IMainLoop
    {
        private readonly BlockingCollection<Action> _work = new();

        public Action BeforeInitialize { get; init; }
        public Action BeforePost { get; init; }
        public Action BeforeRun { get; init; }
        public Action AfterWork { get; init; }

        public bool Initialized { get; private set; }
        public bool Ran { get; private set; }
        public bool RunWasCancelled { get; private set; }

        /// <summary>
        /// False to accept posted work but never run it, modelling a loop that is shut
        /// down while work is still outstanding.
        /// </summary>
        public bool PumpPostedWork { get; init; } = true;

        public void Initialize()
        {
            BeforeInitialize?.Invoke();
            Initialized = true;
        }

        public void Post(Action work)
        {
            BeforePost?.Invoke();
            _work.Add(work);
        }

        public void Run(CancellationToken ct)
        {
            Ran = true;
            BeforeRun?.Invoke();
            try
            {
                if (PumpPostedWork)
                {
                    foreach (Action work in _work.GetConsumingEnumerable(ct))
                    {
                        work();
                        AfterWork?.Invoke();
                    }
                }
                else
                {
                    ct.WaitHandle.WaitOne();
                    ct.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                RunWasCancelled = true;
            }
        }
    }
}
