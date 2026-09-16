using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager.UI
{
    public class Dispatcher
    {
        private readonly DispatcherJobQueue _queue = new();
        private readonly Thread _thread;

        public static Dispatcher MainThread { get; private set; }

        /// <summary>
        /// Initialize the dispatcher associated to the current thread. See <see cref="Thread.CurrentThread"/>.
        /// </summary>
        public static void Initialize()
        {
            MainThread = new Dispatcher(Thread.CurrentThread);
        }

        private Dispatcher(Thread thread)
        {
            _thread = thread;
        }

        public void Run()
        {
            // Should only run the dispatcher job queue from the thread that
            // created the dispatcher.
            VerifyAccess();
            _queue.Run();
        }

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
            _queue.AddJob(new DispatcherJob<object>(ct => { work(ct); return null; }, tcs));
            return tcs.Task;
        }

        /// <inheritdoc cref="InvokeAsync(Action{CancellationToken})"/>
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
                    // Marshal the failure back to the caller rather than letting it escape
                    // on to whichever loop is currently pumping the dispatcher thread.
                    _tcs.TrySetException(ex);
                }
            }
        }

        private class DispatcherJobQueue
        {
            private readonly Queue<IDispatcherJob> _queue = new();
            private readonly CancellationTokenSource _cts = new();

            private enum State
            {
                NotStarted,
                Started,
                Stopping,
                Stopped,
            }

            private State _state = State.NotStarted;

            public void Run()
            {
                lock (_queue)
                {
                    switch (_state)
                    {
                        case State.Started:
                            throw new InvalidOperationException("Dispatcher has already started.");
                        case State.Stopping:
                            throw new InvalidOperationException("Dispatcher is shutting down.");
                        case State.Stopped:
                            throw new InvalidOperationException("Dispatcher has shut down.");
                    }

                    _state = State.Started;
                }

                while (TryTake(out IDispatcherJob job))
                {
                    job.Execute(_cts.Token);
                }
            }

            public void Shutdown()
            {
                lock (_queue)
                {
                    switch (_state)
                    {
                        case State.NotStarted:
                            throw new InvalidOperationException("Dispatcher is not running.");
                        case State.Stopping:
                            throw new InvalidOperationException("Dispatcher is already shutting down.");
                        case State.Stopped:
                            throw new InvalidOperationException("Dispatcher has already shut down.");
                    }
                    _state = State.Stopping;
                    _cts.Cancel();
                    Monitor.Pulse(_queue);
                }
            }

            public void AddJob(IDispatcherJob job)
            {
                lock (_queue)
                {
                    switch (_state)
                    {
                        case State.Stopping:
                            throw new InvalidOperationException("Dispatcher is shutting down.");
                        case State.Stopped:
                            throw new InvalidOperationException("Dispatcher has shut down.");
                    }

                    _queue.Enqueue(job);
                    Monitor.Pulse(_queue);
                }
            }

            private bool TryTake(out IDispatcherJob job)
            {
                lock (_queue)
                {
                    while (_queue.Count == 0)
                    {
                        // Only check for stopping state when the queue is empty
                        // to allow remaining jobs to drain. We check for the stopping
                        // state in AddJob to ensure no more jobs can be added.
                        if (_state == State.Stopping)
                        {
                            job = null;
                            return false;
                        }

                        Monitor.Wait(_queue);
                    }

                    job = _queue.Dequeue();
                    return true;
                }
            }
        }
    }
}
