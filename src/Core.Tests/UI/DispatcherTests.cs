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
    public async Task Dispatcher_InvokeAsync_BeforeRunIsReached_PreservesWorkRequest()
    {
        var mainLoop = new FakeMainLoop();
        using var initialized = new ManualResetEventSlim();
        using var mayRun = new ManualResetEventSlim();
        using var posting = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized, () => mayRun.Wait());
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            Thread postingThread = null;
            bool workRan = false;
            Task<Task> submission = Submit(() =>
            {
                postingThread = Thread.CurrentThread;
                posting.Set();
                return dispatcher.InvokeAsync(_ => { workRan = true; });
            });

            Assert.True(posting.Wait(Timeout));

            // Let the caller block in AddJob before Run can reach its wait, so the
            // work request has to survive being signalled before anybody waits on it.
            Assert.True(SpinWait.SpinUntil(
                () => (postingThread.ThreadState & ThreadState.WaitSleepJoin) != 0, Timeout));
            Assert.False(submission.IsCompleted);
            Assert.False(mainLoop.Initialized);

            mayRun.Set();
            await (await submission.WaitAsync(Timeout)).WaitAsync(Timeout);

            Assert.True(workRan);
            Assert.True(mainLoop.Ran);
        }
        finally
        {
            mayRun.Set();
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Fact]
    public async Task Dispatcher_InvokeAsync_FirstJob_BlocksUntilMainLoopIsInitialized()
    {
        using var mayInitialize = new ManualResetEventSlim();
        var mainLoop = new FakeMainLoop
        {
            BeforeInitialize = () => Assert.True(mayInitialize.Wait(Timeout)),
        };
        using var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        Task<Task> submission = Submit(() => dispatcher.InvokeAsync(_ => { }));

        // Callers hand their own work to the main loop so that it runs with their ambient
        // state, which means they cannot return until the loop is there to take it.
        Task delay = Task.Delay(TimeSpan.FromMilliseconds(250));
        Assert.Same(delay, await Task.WhenAny(submission, delay));

        mayInitialize.Set();
        await (await submission.WaitAsync(Timeout)).WaitAsync(Timeout);

        dispatcher.Shutdown();
        Assert.True(thread.Join(Timeout));
    }

    [Fact]
    public async Task Dispatcher_InvokeAsync_BeforeMainLoopPumps_AcceptsButDoesNotRunWork()
    {
        using var reachedRun = new ManualResetEventSlim();
        using var mayPump = new ManualResetEventSlim();
        var mainLoop = new FakeMainLoop
        {
            BeforeRun = () =>
            {
                reachedRun.Set();
                Assert.True(mayPump.Wait(Timeout));
            },
        };
        using var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            bool workRan = false;
            Task task = await Submit(() => dispatcher.InvokeAsync(_ => { workRan = true; })).WaitAsync(Timeout);
            Assert.True(reachedRun.Wait(Timeout));
            Assert.True(mainLoop.Initialized);
            Assert.False(task.IsCompleted);
            Assert.False(workRan);

            mayPump.Set();
            await task.WaitAsync(Timeout);
            Assert.True(workRan);
        }
        finally
        {
            mayPump.Set();
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Fact]
    public async Task Dispatcher_InvokeAsync_ConcurrentFirstJobs_ReleasesAllCallers()
    {
        using var initializing = new ManualResetEventSlim();
        using var mayInitialize = new ManualResetEventSlim();
        int postCount = 0;
        var mainLoop = new FakeMainLoop
        {
            BeforeInitialize = () =>
            {
                initializing.Set();
                Assert.True(mayInitialize.Wait(Timeout));
            },
            BeforePost = () => Interlocked.Increment(ref postCount),
        };
        using var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            int workCount = 0;
            Task<Task>[] submissions =
            {
                Submit(() => dispatcher.InvokeAsync(_ => { Interlocked.Increment(ref workCount); })),
                Submit(() => dispatcher.InvokeAsync(_ => { Interlocked.Increment(ref workCount); return 42; })),
                Submit(() => dispatcher.InvokeAsync(_ => { Interlocked.Increment(ref workCount); return Task.CompletedTask; })),
                Submit(() => dispatcher.InvokeAsync(_ => { Interlocked.Increment(ref workCount); return Task.FromResult(42); })),
            };

            Assert.True(initializing.Wait(Timeout));
            Assert.All(submissions, submission => Assert.False(submission.IsCompleted));
            Assert.Equal(0, Volatile.Read(ref postCount));

            mayInitialize.Set();
            foreach (Task<Task> submission in submissions)
            {
                await (await submission.WaitAsync(Timeout)).WaitAsync(Timeout);
            }

            Assert.Equal(4, workCount);
            Assert.Equal(4, postCount);

            // The open gate also admits late callers and posts from the dispatcher
            // thread itself, without another signal or a reset.
            Task subsequent = await Submit(() => dispatcher.InvokeAsync(_ =>
                dispatcher.InvokeAsync(_ => { Interlocked.Increment(ref workCount); }))).WaitAsync(Timeout);
            await subsequent.WaitAsync(Timeout);

            Assert.Equal(5, workCount);
            Assert.Equal(6, postCount);
        }
        finally
        {
            mayInitialize.Set();
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Fact]
    public void Dispatcher_InvokeAsync_FromDispatcherThreadBeforeMainLoop_Throws()
    {
        var mainLoop = new FakeMainLoop();
        using var initialized = new ManualResetEventSlim();
        using var posted = new ManualResetEventSlim();
        Exception error = null;

        // Waiting here would be waiting on the only thread that can start the main loop.
        Thread thread = StartDispatcherThread(mainLoop, initialized, () =>
        {
            error = Record.Exception(() => Dispatcher.MainThread.Post(_ => { }));
            posted.Set();
        });

        Assert.True(initialized.Wait(Timeout));
        Assert.True(posted.Wait(Timeout));
        Assert.IsType<InvalidOperationException>(error);

        Dispatcher.MainThread.Shutdown();
        Assert.True(thread.Join(Timeout));
        Assert.False(mainLoop.Initialized);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispatcher_Shutdown_WhileInitializing_ReleasesWaitingCallers(bool failInitialize)
    {
        using var initializing = new ManualResetEventSlim();
        using var mayInitialize = new ManualResetEventSlim();
        int postCount = 0;
        var mainLoop = new FakeMainLoop
        {
            BeforeInitialize = () =>
            {
                initializing.Set();
                Assert.True(mayInitialize.Wait(Timeout));
                if (failInitialize)
                {
                    throw new InvalidOperationException("Initialization failed after shutdown.");
                }
            },
            BeforePost = () => Interlocked.Increment(ref postCount),
        };
        using var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;
        bool shutdownRequested = false;

        try
        {
            Task<Task>[] submissions =
            {
                Submit(() => dispatcher.InvokeAsync(_ => { })),
                Submit(() => dispatcher.InvokeAsync(_ => 42)),
                Submit(() => dispatcher.InvokeAsync(_ => Task.CompletedTask)),
                Submit(() => dispatcher.InvokeAsync(_ => Task.FromResult(42))),
            };

            Assert.True(initializing.Wait(Timeout));
            dispatcher.Shutdown();
            shutdownRequested = true;

            // Shutdown must release callers before Initialize returns, without letting
            // any of their work reach a loop that is not ready to accept it.
            foreach (Task<Task> submission in submissions)
            {
                InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => submission.WaitAsync(Timeout));
                Assert.Equal("Dispatcher is shutting down.", error.Message);
            }

            Assert.False(mainLoop.Initialized);
            Assert.False(mainLoop.Ran);
            Assert.Equal(0, Volatile.Read(ref postCount));

            mayInitialize.Set();
            Assert.True(thread.Join(Timeout));
            Assert.Equal(!failInitialize, mainLoop.RunWasCancelled);

            // A failure after shutdown must not wait for another shutdown signal.
            InvalidOperationException stopped = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Submit(() => dispatcher.InvokeAsync(_ => { })).WaitAsync(Timeout));
            Assert.Equal("Dispatcher has shut down.", stopped.Message);
            Assert.Equal(0, postCount);
        }
        finally
        {
            if (!shutdownRequested)
            {
                dispatcher.Shutdown();
            }

            mayInitialize.Set();
            Assert.True(thread.Join(Timeout));
        }
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
        using var reachedFailure = new ManualResetEventSlim();
        using var mayFail = new ManualResetEventSlim();
        Action fail = () =>
        {
            reachedFailure.Set();
            Assert.True(mayFail.Wait(Timeout));
            throw failure;
        };
        var mainLoop = new FakeMainLoop
        {
            BeforeInitialize = failInitialize ? fail : null,
            BeforeRun = failInitialize ? null : fail,
        };
        using var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            bool workRan = false;

            // Submitting blocks until the main loop is running, so it cannot be done from
            // the thread that has to release it.
            Task<Task>[] submissions =
            {
                Submit(() => dispatcher.InvokeAsync(_ => { workRan = true; })),
                Submit(() => dispatcher.InvokeAsync(_ => { workRan = true; return 42; })),
                Submit(() => dispatcher.InvokeAsync(_ => { workRan = true; return Task.CompletedTask; })),
                Submit(() => dispatcher.InvokeAsync(_ => { workRan = true; return Task.FromResult(42); })),
            };

            // Getting as far as the failure proves the dispatcher thread has seen the
            // work, so at least one caller is waiting on the loop it is about to fail.
            Assert.True(reachedFailure.Wait(Timeout));
            mayFail.Set();

            foreach (Task<Task> submission in submissions)
            {
                Task task = await submission.WaitAsync(Timeout);
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
            mayFail.Set();
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Fact]
    public async Task Dispatcher_InvokeAsync_PostFails_FaultsPostedAndFutureJobs()
    {
        var failure = new InvalidOperationException("Posting to the main loop failed.");
        int postCount = 0;
        var mainLoop = new FakeMainLoop
        {
            // Nothing may run: the jobs have to fault rather than complete.
            PumpPostedWork = false,
            BeforePost = () =>
            {
                if (Interlocked.Increment(ref postCount) == 2)
                {
                    throw failure;
                }
            },
        };
        using var initialized = new ManualResetEventSlim();

        Thread thread = StartDispatcherThread(mainLoop, initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            bool workRan = false;

            // Submit one at a time so the failing post is reliably the second, covering
            // work already posted, the rejected post, and work submitted afterwards.
            Task posted = await Submit(() => dispatcher.InvokeAsync(_ => { workRan = true; })).WaitAsync(Timeout);
            Task rejected = await Submit(() => dispatcher.InvokeAsync(_ => { workRan = true; })).WaitAsync(Timeout);
            Task future = await Submit(() => dispatcher.InvokeAsync(_ => { workRan = true; })).WaitAsync(Timeout);

            foreach (Task task in new[] { posted, rejected, future })
            {
                Assert.Same(failure,
                    await Assert.ThrowsAsync<InvalidOperationException>(() => task.WaitAsync(Timeout)));
            }

            Assert.False(workRan);
            Assert.Equal(2, postCount);
        }
        finally
        {
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

    private static Task<Task> Submit(Func<Task> submission) =>
        Task.Factory.StartNew(
            submission, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

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
            Assert.True(Initialized);
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
