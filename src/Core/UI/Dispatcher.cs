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
        /// Execute synchronous work on the thread associated with this dispatcher and
        /// return a task that completes when the work is done.
        /// </summary>
        /// <param name="work">Work to be run.</param>
        public Task InvokeAsync(Action<CancellationToken> work)
        {
            var job = new DispatcherJob<object>(ct => { work(ct); return null; });
            _queue.AddJob(job);
            return job.Completion;
        }

        /// <inheritdoc cref="InvokeAsync(Action{CancellationToken})"/>
        public Task<TResult> InvokeAsync<TResult>(Func<CancellationToken, TResult> work)
        {
            var job = new DispatcherJob<TResult>(work);
            _queue.AddJob(job);
            return job.Completion;
        }

        /// <summary>
        /// Execute asynchronous work on the thread associated with this dispatcher.
        /// </summary>
        /// <param name="work">Work to be run.</param>
        /// <returns>A task that completes when the work completes, not when it first yields.</returns>
        public Task InvokeAsync(Func<CancellationToken, Task> work)
        {
            var job = new AsyncDispatcherJob(work);
            _queue.AddJob(job);
            return job.Completion;
        }

        /// <inheritdoc cref="InvokeAsync(Func{CancellationToken, Task})"/>
        public Task<TResult> InvokeAsync<TResult>(Func<CancellationToken, Task<TResult>> work)
        {
            var job = new AsyncDispatcherJob<TResult>(work);
            _queue.AddJob(job);
            return job.Completion;
        }

        private interface IDispatcherJob
        {
            Task Completion { get; }

            void Execute(CancellationToken ct);

            void Fail(Exception ex);
        }

        private abstract class DispatcherJob : IDispatcherJob
        {
            public abstract Task Completion { get; }

            public void Execute(CancellationToken ct)
            {
                try
                {
                    ExecuteCore(ct);
                }
                catch (Exception ex)
                {
                    // Marshal the failure back to the caller rather than letting it escape
                    // on to whichever loop is currently pumping the dispatcher thread.
                    Fail(ex);
                }
            }

            public abstract void Fail(Exception ex);

            protected abstract void ExecuteCore(CancellationToken ct);
        }

        private sealed class DispatcherJob<TResult> : DispatcherJob
        {
            private readonly Func<CancellationToken, TResult> _work;
            private readonly TaskCompletionSource<TResult> _tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public override Task<TResult> Completion => _tcs.Task;

            public DispatcherJob(Func<CancellationToken, TResult> work)
            {
                _work = work;
            }

            protected override void ExecuteCore(CancellationToken ct) => _tcs.TrySetResult(_work(ct));

            public override void Fail(Exception ex) => _tcs.TrySetException(ex);
        }

        private sealed class AsyncDispatcherJob : DispatcherJob
        {
            private readonly Func<CancellationToken, Task> _work;
            private readonly TaskCompletionSource _tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public override Task Completion => _tcs.Task;

            public AsyncDispatcherJob(Func<CancellationToken, Task> work)
            {
                _work = work;
            }

            protected override void ExecuteCore(CancellationToken ct) => _ = CompleteAsync(_work(ct));

            public override void Fail(Exception ex) => _tcs.TrySetException(ex);

            private async Task CompleteAsync(Task task)
            {
                if (task is null)
                {
                    // Preserve Unwrap's treatment of a missing inner task.
                    _tcs.TrySetCanceled();
                    return;
                }

                // Observe completion without throwing, then forward the original outcome intact.
                await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                _tcs.TrySetFromTask(task);
            }
        }

        private sealed class AsyncDispatcherJob<TResult> : DispatcherJob
        {
            private readonly Func<CancellationToken, Task<TResult>> _work;
            private readonly TaskCompletionSource<TResult> _tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public override Task<TResult> Completion => _tcs.Task;

            public AsyncDispatcherJob(Func<CancellationToken, Task<TResult>> work)
            {
                _work = work;
            }

            protected override void ExecuteCore(CancellationToken ct) => _ = CompleteAsync(_work(ct));

            public override void Fail(Exception ex) => _tcs.TrySetException(ex);

            private async Task CompleteAsync(Task<TResult> task)
            {
                if (task is null)
                {
                    _tcs.TrySetCanceled();
                    return;
                }

                // SuppressThrowing is only supported by the non-generic Task awaiter.
                await ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                _tcs.TrySetFromTask(task);
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
                    while (TryTake(out IDispatcherJob job))
                    {
                        job.Execute(_cts.Token);
                    }
                }
                finally
                {
                    lock (_queue)
                    {
                        _state = State.Stopped;
                    }
                }
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
