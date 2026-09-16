using System;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI;
using Xunit;

namespace GitCredentialManager.Tests.UI;

public class DispatcherTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispatcher_InvokeAsync_AsyncWork_DoesNotInlineCallerContinuations(bool returnsValue)
    {
        using var initialized = new ManualResetEventSlim();
        Thread thread = StartDispatcherThread(initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            // Deliberately allow the work's own task to run continuations inline.
            var work = new TaskCompletionSource<int>();
            Task task = returnsValue
                ? dispatcher.InvokeAsync(_ => work.Task)
                : dispatcher.InvokeAsync(_ => (Task)work.Task);
            Assert.Equal(TaskCreationOptions.RunContinuationsAsynchronously,
                task.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously);
            Task<bool> continuation = task.ContinueWith(
                _ => dispatcher.CheckAccess(),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            await dispatcher.InvokeAsync(_ => work.SetResult(42)).WaitAsync(Timeout);

            Assert.False(await continuation.WaitAsync(Timeout));
        }
        finally
        {
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Theory]
    [InlineData(false, false, TaskStatus.RanToCompletion)]
    [InlineData(false, true, TaskStatus.RanToCompletion)]
    [InlineData(true, false, TaskStatus.RanToCompletion)]
    [InlineData(true, true, TaskStatus.RanToCompletion)]
    [InlineData(false, false, TaskStatus.Faulted)]
    [InlineData(false, true, TaskStatus.Faulted)]
    [InlineData(true, false, TaskStatus.Faulted)]
    [InlineData(true, true, TaskStatus.Faulted)]
    [InlineData(false, false, TaskStatus.Canceled)]
    [InlineData(false, true, TaskStatus.Canceled)]
    [InlineData(true, false, TaskStatus.Canceled)]
    [InlineData(true, true, TaskStatus.Canceled)]
    public async Task Dispatcher_InvokeAsync_AsyncWork_PreservesTaskOutcome(
        bool returnsValue, bool alreadyCompleted, TaskStatus outcome)
    {
        using var initialized = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Thread thread = StartDispatcherThread(initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            var work = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            Exception[] failures =
            {
                new InvalidOperationException("First work failure."),
                new ArgumentException("Second work failure."),
            };

            void CompleteWork()
            {
                switch (outcome)
                {
                    case TaskStatus.RanToCompletion:
                        work.SetResult(42);
                        break;
                    case TaskStatus.Faulted:
                        work.SetException(failures);
                        break;
                    case TaskStatus.Canceled:
                        work.SetCanceled(cancellation.Token);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(outcome));
                }
            }

            if (alreadyCompleted)
            {
                CompleteWork();
            }

            Task task = returnsValue
                ? dispatcher.InvokeAsync(_ => work.Task)
                : dispatcher.InvokeAsync(_ => (Task)work.Task);

            if (!alreadyCompleted)
            {
                Assert.False(task.IsCompleted);
                CompleteWork();
            }

            switch (outcome)
            {
                case TaskStatus.RanToCompletion:
                    await task.WaitAsync(Timeout);
                    if (returnsValue)
                    {
                        Assert.Equal(42, await (Task<int>)task);
                    }
                    break;
                case TaskStatus.Faulted:
                    Assert.Same(failures[0],
                        await Assert.ThrowsAsync<InvalidOperationException>(() => task.WaitAsync(Timeout)));
                    Assert.Equal(failures, task.Exception.InnerExceptions);
                    break;
                case TaskStatus.Canceled:
                    OperationCanceledException error =
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(Timeout));
                    Assert.Equal(cancellation.Token, error.CancellationToken);
                    Assert.True(task.IsCanceled);
                    break;
            }
        }
        finally
        {
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispatcher_InvokeAsync_DelegateThrows_FaultsTaskWithoutFailingLoop(bool throwsCancellation)
    {
        using var initialized = new ManualResetEventSlim();
        Thread thread = StartDispatcherThread(initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            Exception failure = throwsCancellation
                ? new OperationCanceledException()
                : new InvalidOperationException("The delegate failed.");
            void ActionWork(CancellationToken ct) => throw failure;
            int ValueWork(CancellationToken ct) => throw failure;
            Task AsyncWork(CancellationToken ct) => throw failure;
            Task<int> AsyncValueWork(CancellationToken ct) => throw failure;

            Task[] tasks =
            {
                dispatcher.InvokeAsync(ActionWork),
                dispatcher.InvokeAsync(ValueWork),
                dispatcher.InvokeAsync(AsyncWork),
                dispatcher.InvokeAsync(AsyncValueWork),
            };

            foreach (Task task in tasks)
            {
                Assert.Same(failure, await Record.ExceptionAsync(() => task.WaitAsync(Timeout)));
                Assert.True(task.IsFaulted);
            }

            Assert.Equal(42, await dispatcher.InvokeAsync(_ => 42).WaitAsync(Timeout));
        }
        finally
        {
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispatcher_InvokeAsync_AsyncWorkReturnsNull_CancelsTask(bool returnsValue)
    {
        using var initialized = new ManualResetEventSlim();
        Thread thread = StartDispatcherThread(initialized);
        Assert.True(initialized.Wait(Timeout));
        Dispatcher dispatcher = Dispatcher.MainThread;

        try
        {
            Task<int> Work(CancellationToken ct) => null;
            Task task = returnsValue
                ? dispatcher.InvokeAsync(Work)
                : dispatcher.InvokeAsync(ct => (Task)Work(ct));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(Timeout));
            Assert.True(task.IsCanceled);
        }
        finally
        {
            dispatcher.Shutdown();
            Assert.True(thread.Join(Timeout));
        }
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
