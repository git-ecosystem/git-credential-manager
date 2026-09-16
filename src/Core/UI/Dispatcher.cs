using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager.UI
{
    /// <summary>
    /// Owns the process entry thread (the "main thread") and runs work posted to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some platform APIs must be used from the process entry thread: macOS requires UI
    /// controls to be created there, and the macOS MSAL broker requires a running
    /// NSApplication. Both of those need a platform main loop, which is expensive to
    /// start and most GCM invocations never need.
    /// </para>
    /// <para>
    /// The dispatcher therefore parks the main thread cheaply until the first job is
    /// posted, and only then starts the main loop just-in-time and hands the thread over
    /// to it for the remaining lifetime of the process. Because the hand-over completes
    /// before any job can be dispatched, work posted here is guaranteed to run with the
    /// main loop - and therefore NSApplication - already running.
    /// </para>
    /// </remarks>
    public class Dispatcher
    {
        private readonly DispatcherJobQueue _queue;
        private readonly Thread _thread;

        public static Dispatcher MainThread { get; private set; }

        /// <summary>
        /// Initialize the dispatcher associated to the current thread. See <see cref="Thread.CurrentThread"/>.
        /// </summary>
        public static void Initialize() => Initialize(new AvaloniaMainLoop());

        internal static void Initialize(IMainLoop mainLoop)
        {
            MainThread = new Dispatcher(Thread.CurrentThread, mainLoop);
        }

        private Dispatcher(Thread thread, IMainLoop mainLoop)
        {
            _thread = thread;
            _queue = new DispatcherJobQueue(mainLoop);
        }

        public void Run()
        {
            // Should only run the dispatcher job queue from the thread that
            // created the dispatcher.
            VerifyAccess();
            _queue.Run();
        }

        /// <summary>
        /// Stop the dispatcher and release the main thread, causing <see cref="Run"/> to return.
        /// </summary>
        /// <remarks>
        /// Work that has been posted but has not yet completed is abandoned, and the tasks
        /// returned for it never complete. Callers must therefore only shut down once all
        /// work they care about has finished. This is why the application thread shuts the
        /// dispatcher down after running to completion, rather than the other way around:
        /// waiting for outstanding work instead would hang whenever a window is still open
        /// with nobody left to close it.
        /// </remarks>
        public void Shutdown()
        {
            // Can shutdown the dispatcher from any thread.
            _queue.Shutdown();
        }

        public bool CheckAccess() => Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId;

        /// <summary>
        /// Ensure the calling thread is the thread associated with this dispatcher.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The calling thread does not have access this dispatcher.
        /// </exception>
        public void VerifyAccess()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException("Not running on the dispatcher thread.");
            }
        }

        /// <summary>
        /// Post work to be run on the thread associated with this dispatcher.
        /// </summary>
        /// <param name="work">Work to be run.</param>
        public void Post(Action<CancellationToken> work)
        {
            Task _ = InvokeAsync(work);
        }

        /// <summary>
        /// Execute work to be run on the thread associated with this dispatcher and wait
        /// synchronously until the work is complete.
        /// </summary>
        /// <param name="work">Work to be run.</param>
        public Task InvokeAsync(Action<CancellationToken> work)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.AddJob(new DispatcherJob(work, tcs));
            return tcs.Task;
        }

        public Task<TResult> InvokeAsync<TResult>(Func<CancellationToken, TResult> work)
        {
            var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.AddJob(new DispatcherJob<TResult>(work, tcs));
            return tcs.Task;
        }

        /// <summary>
        /// Execute asynchronous work on the thread associated with this dispatcher.
        /// </summary>
        /// <param name="work">Work to be run.</param>
        /// <returns>A task that completes when the work completes, not when it first yields.</returns>
        /// <remarks>
        /// The work starts on the dispatcher thread, and because the main loop installs a
        /// synchronization context its continuations resume there too, unless the work
        /// opts out with <see cref="Task.ConfigureAwait(bool)"/>.
        /// </remarks>
        public Task InvokeAsync(Func<CancellationToken, Task> work)
        {
            var tcs = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.AddJob(new DispatcherJob<Task>(work, tcs));
            return tcs.Task.Unwrap();
        }

        /// <inheritdoc cref="InvokeAsync(Func{CancellationToken, Task})"/>
        public Task<TResult> InvokeAsync<TResult>(Func<CancellationToken, Task<TResult>> work)
        {
            var tcs = new TaskCompletionSource<Task<TResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.AddJob(new DispatcherJob<Task<TResult>>(work, tcs));
            return tcs.Task.Unwrap();
        }

        private interface IDispatcherJob
        {
            void Execute(CancellationToken ct);

            void Fail(Exception ex);
        }

        private class DispatcherJob : IDispatcherJob
        {
            private readonly Action<CancellationToken> _work;
            private readonly TaskCompletionSource<object> _tcs;

            public DispatcherJob(Action<CancellationToken> work, TaskCompletionSource<object> tcs)
            {
                _work = work;
                _tcs = tcs;
            }

            public void Execute(CancellationToken ct)
            {
                try
                {
                    _work(ct);
                    _tcs?.TrySetResult(null);
                }
                catch (Exception ex) when (_tcs is not null)
                {
                    // Marshal the failure back to the caller rather than letting it escape
                    // on to whichever loop is currently pumping the dispatcher thread.
                    _tcs.TrySetException(ex);
                }
            }

            public void Fail(Exception ex) => _tcs?.TrySetException(ex);
        }

        private class DispatcherJob<TResult> : IDispatcherJob
        {
            private readonly Func<CancellationToken, TResult> _work;
            private readonly TaskCompletionSource<TResult> _tcs;

            public DispatcherJob(Func<CancellationToken, TResult> work, TaskCompletionSource<TResult> tcs)
            {
                _work = work;
                _tcs = tcs;
            }

            public void Execute(CancellationToken ct)
            {
                try
                {
                    TResult result = _work(ct);
                    _tcs?.TrySetResult(result);
                }
                catch (Exception ex) when (_tcs is not null)
                {
                    _tcs.TrySetException(ex);
                }
            }

            public void Fail(Exception ex) => _tcs?.TrySetException(ex);
        }

        private class DispatcherJobQueue
        {
            private readonly Queue<IDispatcherJob> _queue = new();
            private readonly HashSet<IDispatcherJob> _outstandingJobs = new();
            private readonly CancellationTokenSource _cts = new();
            private readonly IMainLoop _mainLoop;

            private enum State
            {
                NotStarted,
                Started,
                Stopping,
                Stopped,
            }

            private State _state = State.NotStarted;

            private bool _isMainLoopRunning;
            private Exception _mainLoopFault;

            public DispatcherJobQueue(IMainLoop mainLoop)
            {
                _mainLoop = mainLoop;
            }

            public void Run()
            {
                lock (_queue)
                {
                    switch (_state)
                    {
                        case State.Started:
                            throw new InvalidOperationException("Dispatcher has already started.");
                        case State.Stopping:
                        case State.Stopped:
                            // Shut down before we got here, so there is nothing left to run.
                            return;
                    }

                    _state = State.Started;
                }

                // Park cheaply until the main thread is actually needed. An invocation that
                // never shows UI and never talks to the macOS broker must not pay to start
                // the main loop.
                if (!WaitForWork())
                {
                    // We were shut down before any work arrived.
                    return;
                }

                RunMainLoop();
            }

            public void Shutdown()
            {
                lock (_queue)
                {
                    switch (_state)
                    {
                        case State.Stopping:
                            throw new InvalidOperationException("Dispatcher is already shutting down.");
                        case State.Stopped:
                            throw new InvalidOperationException("Dispatcher has already shut down.");
                    }

                    // Shutting down before Run() has been reached is legitimate: the
                    // application thread can finish before the main thread gets there.
                    // Run() sees this and returns without starting anything.
                    _state = State.Stopping;
                    Monitor.PulseAll(_queue);
                }

                // Cancel outside of the queue lock: this runs the main loop's own
                // cancellation callbacks, which take its locks.
                _cts.Cancel();
            }

            public void AddJob(IDispatcherJob job)
            {
                bool post = false;
                Exception fault;

                lock (_queue)
                {
                    switch (_state)
                    {
                        case State.Stopping:
                            throw new InvalidOperationException("Dispatcher is shutting down.");
                        case State.Stopped:
                            throw new InvalidOperationException("Dispatcher has shut down.");
                    }

                    fault = _mainLoopFault;
                    if (fault is null)
                    {
                        _outstandingJobs.Add(job);
                        if (_isMainLoopRunning)
                        {
                            // Our own loop no longer pumps the thread, so queuing here would
                            // strand the job; hand it to the main loop instead.
                            post = true;
                        }
                        else
                        {
                            _queue.Enqueue(job);
                            Monitor.Pulse(_queue);
                        }
                    }
                }

                // Complete outside of the queue lock; the main loop takes its own locks.
                if (fault is not null)
                {
                    job.Fail(fault);
                }
                else if (post)
                {
                    try
                    {
                        PostToMainLoop(job);
                    }
                    catch (Exception ex)
                    {
                        FailAllJobs(ex);
                    }
                }
            }

            /// <summary>
            /// Start the platform main loop and hand the dispatcher thread over to it.
            /// Runs on the dispatcher thread and does not return until shutdown.
            /// </summary>
            private void RunMainLoop()
            {
                try
                {
                    // Initialize the main loop on this thread. Once this returns its own
                    // dispatcher exists and accepts posted work, even though the main loop
                    // is not running yet.
                    _mainLoop.Initialize();

                    IDispatcherJob[] pending;
                    lock (_queue)
                    {
                        // Hand over and drain in a single atomic step so that no job can be
                        // enqueued into a queue that will never be pumped again. Outstanding
                        // jobs stay tracked until their callbacks finish.
                        _isMainLoopRunning = true;
                        pending = _queue.ToArray();
                        _queue.Clear();
                    }

                    // Queue<T>.ToArray returns items in dequeue order, so FIFO is preserved.
                    // These cannot run until the main loop below is pumping, which is exactly
                    // the guarantee callers rely on: on macOS the MSAL broker requires a
                    // running NSApplication, which only exists from that point onwards.
                    foreach (IDispatcherJob job in pending)
                    {
                        PostToMainLoop(job);
                    }

                    // Owns the dispatcher thread until shutdown.
                    _mainLoop.Run(_cts.Token);
                }
                catch (Exception ex)
                {
                    // The main loop is unusable, so no main thread work can ever run.
                    // Fail outstanding and future jobs so callers see the error instead of
                    // hanging, then keep this thread parked: the application thread still
                    // needs to unwind and shut us down so the process exits cleanly.
                    FailAllJobs(ex);
                    WaitForShutdown();
                }
            }

            private void PostToMainLoop(IDispatcherJob job) => _mainLoop.Post(() =>
            {
                lock (_queue)
                {
                    // A posting failure can invalidate callbacks already in the platform queue.
                    if (_mainLoopFault is not null)
                    {
                        return;
                    }
                }

                try
                {
                    job.Execute(_cts.Token);
                }
                finally
                {
                    lock (_queue)
                    {
                        _outstandingJobs.Remove(job);
                    }
                }
            });

            private void FailAllJobs(Exception ex)
            {
                IDispatcherJob[] pending;
                lock (_queue)
                {
                    if (_mainLoopFault is not null)
                    {
                        return;
                    }

                    _mainLoopFault = ex;
                    _isMainLoopRunning = false;
                    pending = new IDispatcherJob[_outstandingJobs.Count];
                    _outstandingJobs.CopyTo(pending);
                    _outstandingJobs.Clear();
                    _queue.Clear();
                }

                foreach (IDispatcherJob job in pending)
                {
                    job.Fail(ex);
                }
            }

            /// <summary>
            /// Block until a job is queued, or until shutdown.
            /// </summary>
            /// <returns>True if there is work to do, false if the dispatcher is shutting down.</returns>
            private bool WaitForWork()
            {
                lock (_queue)
                {
                    while (_queue.Count == 0)
                    {
                        // Only check for stopping state when the queue is empty so that any
                        // remaining jobs still get to run. AddJob rejects new jobs once we
                        // are stopping.
                        if (_state is State.Stopping or State.Stopped)
                        {
                            return false;
                        }

                        Monitor.Wait(_queue);
                    }

                    return true;
                }
            }

            private void WaitForShutdown()
            {
                lock (_queue)
                {
                    while (_state is not (State.Stopping or State.Stopped))
                    {
                        Monitor.Wait(_queue);
                    }
                }
            }
        }
    }
}
