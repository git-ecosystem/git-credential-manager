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
    /// to it for the remaining lifetime of the process. Posting blocks until the main
    /// loop can accept work, and that work only runs once the loop is pumping.
    /// </para>
    /// </remarks>
    public class Dispatcher
    {
        // The queue of work to run on the dispatcher thread; the queue owns a loop that pumps work.
        private readonly DispatcherJobQueue _queue;

        // The thread that owns this dispatcher; work posted to the dispatcher must run on this thread.
        private readonly Thread _thread;

        // Work run by this dispatcher is reported against this context rather than
        // against whoever posted it, since this is the thread it executes on.
        private readonly Trace2Context _traceContext;

        public static Dispatcher MainThread { get; private set; }

        /// <summary>
        /// Initialize the dispatcher associated to the current thread. See <see cref="Thread.CurrentThread"/>.
        /// </summary>
        public static void Initialize() => Initialize(new AvaloniaMainLoop());

        internal static void Initialize(IMainLoop mainLoop)
        {
            MainThread = new Dispatcher(Thread.CurrentThread, Trace2.GetCurrentContext(), mainLoop);
        }

        private Dispatcher(Thread thread, Trace2Context traceContext, IMainLoop mainLoop)
        {
            _thread = thread;
            _traceContext = traceContext;
            _queue = new DispatcherJobQueue(this, mainLoop);
        }

        public void Run()
        {
            // Should only run the dispatcher job queue from the thread that created the dispatcher.
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
            // Can shut down the dispatcher from any thread.
            _queue.Shutdown();
        }

        public bool CheckAccess() => Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId;

        /// <summary>
        /// Ensure the calling thread is the thread associated with this dispatcher.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The calling thread does not have access to this dispatcher.
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
        /// <remarks>
        /// The first call to the dispatcher starts the main loop, and blocks until it can
        /// accept posted work.
        /// </remarks>
        public void Post(Action<CancellationToken> work)
        {
            Task _ = InvokeAsync(work);
        }

        /// <summary>
        /// Execute work to be run on the thread associated with this dispatcher and wait
        /// synchronously until the work is complete.
        /// </summary>
        /// <param name="work">Work to be run.</param>
        /// <remarks>
        /// The first call to the dispatcher starts the main loop, and blocks until it can
        /// accept posted work.
        /// </remarks>
        public Task InvokeAsync(Action<CancellationToken> work)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.AddJob(new DispatcherJob<object>(
                ct => { work(ct); return null; }, tcs, ExecutionContext.Capture(), _traceContext));
            return tcs.Task;
        }

        /// <inheritdoc cref="InvokeAsync(Action{CancellationToken})"/>
        public Task<TResult> InvokeAsync<TResult>(Func<CancellationToken, TResult> work)
        {
            var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.AddJob(new DispatcherJob<TResult>(work, tcs, ExecutionContext.Capture(), _traceContext));
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
            _queue.AddJob(new DispatcherJob<Task>(work, tcs, ExecutionContext.Capture(), _traceContext));
            return tcs.Task.Unwrap();
        }

        /// <inheritdoc cref="InvokeAsync(Func{CancellationToken, Task})"/>
        public Task<TResult> InvokeAsync<TResult>(Func<CancellationToken, Task<TResult>> work)
        {
            var tcs = new TaskCompletionSource<Task<TResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.AddJob(new DispatcherJob<Task<TResult>>(work, tcs, ExecutionContext.Capture(), _traceContext));
            return tcs.Task.Unwrap();
        }

        private interface IDispatcherJob
        {
            void Execute(CancellationToken ct);

            void Fail(Exception ex);
        }

        private class DispatcherJob<TResult> : IDispatcherJob
        {
            private readonly Func<CancellationToken, TResult> _work;
            private readonly TaskCompletionSource<TResult> _tcs;
            private readonly ExecutionContext _callerContext;
            private readonly Trace2Context _traceContext;

            public DispatcherJob(
                Func<CancellationToken, TResult> work,
                TaskCompletionSource<TResult> tcs,
                ExecutionContext callerContext,
                Trace2Context traceContext)
            {
                _work = work;
                _tcs = tcs;
                _callerContext = callerContext;
                _traceContext = traceContext;
            }

            public void Execute(CancellationToken ct)
            {
                try
                {
                    TResult result = default;
                    RunWork(() => result = _work(ct));
                    _tcs?.TrySetResult(result);
                }
                catch (Exception ex) when (_tcs is not null)
                {
                    // Marshal the failure back to the caller rather than letting it escape
                    // on to whichever loop is currently pumping the dispatcher thread.
                    _tcs.TrySetException(ex);
                }
            }

            public void Fail(Exception ex) => _tcs?.TrySetException(ex);

            /// <summary>
            /// Run the work as the caller that posted it, but reported as the thread that
            /// is running it.
            /// </summary>
            private void RunWork(Action work)
            {
                // Work must observe the ambient state of whoever posted it, so the caller's
                // execution context is restored around it; anything flowed by AsyncLocal<T>,
                // such as System.Diagnostics.Activity.Current, would otherwise be lost
                // crossing to the dispatcher thread.
                //
                // Trace2 is the exception. It reports which thread work ran on, and this work
                // runs on the dispatcher thread, so its context is applied on top - and must
                // be applied inside the restored context, since restoring replaces the whole
                // AsyncLocal<T> map and would shadow a switch made outside it.

                // Nothing to restore if the caller suppressed flow.
                if (_callerContext is null)
                {
                    RunAs(work);
                    return;
                }

                ExecutionContext.Run(_callerContext, state => RunAs((Action)state), work);
            }

            private void RunAs(Action work)
            {
                Trace2Context previous = Trace2.GetCurrentContext();
                Trace2.SetCurrentContext(_traceContext);
                try
                {
                    if (!ReferenceEquals(previous, _traceContext))
                    {
                        // Switching context loses which logical thread asked for the work,
                        // so record it before we run.
                        Trace2.WriteData("dispatcher", "caller", previous?.ThreadName ?? string.Empty);
                    }

                    work();
                }
                finally
                {
                    Trace2.SetCurrentContext(previous);
                }
            }
        }

        private class DispatcherJobQueue
        {
            private readonly Dispatcher _owner;
            private readonly IMainLoop _mainLoop;
            private readonly Lock _lock = new();
            private readonly HashSet<IDispatcherJob> _outstandingJobs = new();
            private readonly CancellationTokenSource _cts = new();

            private readonly ManualResetEventSlim _workRequested = new(false, spinCount: 0);
            private readonly ManualResetEventSlim _mainLoopReady = new(false);

            private enum State
            {
                NotStarted,
                Started,
                Stopping,
                Stopped,
            }

            private State _state = State.NotStarted;

            private Exception _mainLoopFault;

            public DispatcherJobQueue(Dispatcher owner, IMainLoop mainLoop)
            {
                _owner = owner;
                _mainLoop = mainLoop;
            }

            public void Run()
            {
                lock (_lock)
                {
                    switch (_state)
                    {
                        case State.Started:
                            throw new InvalidOperationException("Dispatcher has already started.");
                        case State.Stopped:
                            throw new InvalidOperationException("Dispatcher has shut down.");
                        case State.Stopping:
                            // Shut down before we got here, so there is nothing left to run.
                            _state = State.Stopped;
                            return;
                    }

                    _state = State.Started;
                }

                try
                {
                    // Park cheaply until the main thread is actually needed. An invocation
                    // that never needs the main thread must not pay to start the main loop.
                    if (!WaitForWork())
                    {
                        // We were shut down before any work arrived!
                        return;
                    }

                    RunMainLoop();
                }
                finally
                {
                    lock (_lock)
                    {
                        _state = State.Stopped;

                        // No waiting caller can have its work accepted now.
                        _mainLoopReady.Set();
                    }
                }
            }

            public void Shutdown()
            {
                lock (_lock)
                {
                    switch (_state)
                    {
                        case State.Stopping:
                            throw new InvalidOperationException("Dispatcher is already shutting down.");
                        case State.Stopped:
                            throw new InvalidOperationException("Dispatcher has already shut down.");
                    }

                    // Shutting down before Run() has been reached is legitimate.
                    // The application thread can finish before the main thread gets there.
                    // Run() sees this and returns without starting anything.
                    _state = State.Stopping;
                    _workRequested.Set();
                    _mainLoopReady.Set();
                }

                // Cancel outside the lock. This runs the main loop's own cancellation
                // callbacks, which take its locks.
                _cts.Cancel();
            }

            public void AddJob(IDispatcherJob job)
            {
                Exception fault;

                lock (_lock)
                {
                    ThrowIfShuttingDown();

                    if (!_mainLoopReady.IsSet && _owner.CheckAccess())
                    {
                        // We would be waiting on the only thread that can release us.
                        throw new InvalidOperationException(
                            "Cannot post work from the dispatcher thread before the main loop is running.");
                    }

                    // Needing the main thread is what starts the main loop.
                    _workRequested.Set();
                }

                // Wait for the main loop to be read to accept work before we post the job.
                _mainLoopReady.Wait();

                lock (_lock)
                {
                    // The 'ready' gate also opens on failure and shutdown, not just success.
                    fault = _mainLoopFault;
                    if (fault is null)
                    {
                        ThrowIfShuttingDown();
                        _outstandingJobs.Add(job);
                    }
                }

                // Complete outside the lock; the main loop takes its own locks.
                if (fault is not null)
                {
                    job.Fail(fault);
                    return;
                }

                try
                {
                    PostToMainLoop(job);
                }
                catch (Exception ex)
                {
                    FailAllJobs(ex);
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

                    // Release the callers waiting to post. Their work still cannot run
                    // until the main loop below is pumping (which we start below), but
                    // after init the main loop can begin accepting and queuing work.
                    _mainLoopReady.Set();

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
                lock (_lock)
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
                    lock (_lock)
                    {
                        _outstandingJobs.Remove(job);
                    }
                }
            });

            private void FailAllJobs(Exception ex)
            {
                IDispatcherJob[] pending;
                lock (_lock)
                {
                    if (_mainLoopFault is not null)
                    {
                        return;
                    }

                    _mainLoopFault = ex;
                    pending = new IDispatcherJob[_outstandingJobs.Count];
                    _outstandingJobs.CopyTo(pending);
                    _outstandingJobs.Clear();

                    // Release anyone waiting to post; they will see the fault instead.
                    _mainLoopReady.Set();
                }

                foreach (IDispatcherJob job in pending)
                {
                    job.Fail(ex);
                }
            }

            private void ThrowIfShuttingDown()
            {
                switch (_state)
                {
                    case State.Stopping:
                        throw new InvalidOperationException("Dispatcher is shutting down.");
                    case State.Stopped:
                        throw new InvalidOperationException("Dispatcher has shut down.");
                }
            }

            /// <summary>
            /// Block until the main thread is needed, or until shutdown.
            /// </summary>
            /// <returns>True if there is work to do, false if the dispatcher is shutting down.</returns>
            private bool WaitForWork()
            {
                _workRequested.Wait();

                lock (_lock)
                {
                    return _state is not (State.Stopping or State.Stopped);
                }
            }

            // Only the failure path materializes the cancellation token's wait handle.
            private void WaitForShutdown() => _cts.Token.WaitHandle.WaitOne();
        }
    }
}
