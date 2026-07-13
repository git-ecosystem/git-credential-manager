using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitCredentialManager.Tty;
using Spectre.Console;
using Xunit;

namespace GitCredentialManager.Tests.Tty;

public class RawModeExclusivityModeTests
{
    [Fact]
    public void Run_BeginsRawModeSessionInsideExclusivity_AndDisposesAfterFunc()
    {
        var log = new List<string>();
        var mode = new RawModeExclusivityMode(new RecordingExclusivityMode(log), new RecordingSessionInput(log));

        int result = mode.Run(() => { log.Add("func"); return 42; });

        Assert.Equal(42, result);
        // The raw-mode session must be held for the whole func and entered only
        // once exclusivity has been acquired.
        Assert.Equal(new[] { "inner.Run", "session.begin", "func", "session.dispose" }, log);
    }

    [Fact]
    public async Task RunAsync_BeginsRawModeSessionInsideExclusivity_AndDisposesAfterFunc()
    {
        var log = new List<string>();
        var mode = new RawModeExclusivityMode(new RecordingExclusivityMode(log), new RecordingSessionInput(log));

        int result = await mode.RunAsync(async () => { log.Add("func"); await Task.Yield(); return 7; });

        Assert.Equal(7, result);
        Assert.Equal(new[] { "inner.RunAsync", "session.begin", "func", "session.dispose" }, log);
    }

    [Fact]
    public void Run_DisposesRawModeSession_WhenFuncThrows()
    {
        var log = new List<string>();
        var mode = new RawModeExclusivityMode(new RecordingExclusivityMode(log), new RecordingSessionInput(log));

        Assert.Throws<InvalidOperationException>(() => mode.Run<int>(() => throw new InvalidOperationException()));
        Assert.Equal(new[] { "inner.Run", "session.begin", "session.dispose" }, log);
    }

    private sealed class RecordingExclusivityMode : IExclusivityMode
    {
        private readonly List<string> _log;
        public RecordingExclusivityMode(List<string> log) => _log = log;
        public T Run<T>(Func<T> func) { _log.Add("inner.Run"); return func(); }
        public async Task<T> RunAsync<T>(Func<Task<T>> func) { _log.Add("inner.RunAsync"); return await func(); }
    }

    private sealed class RecordingSessionInput : IRawModeSessionInput
    {
        private readonly List<string> _log;
        public RecordingSessionInput(List<string> log) => _log = log;
        public IDisposable BeginRawModeSession() { _log.Add("session.begin"); return new Scope(_log); }

        private sealed class Scope : IDisposable
        {
            private readonly List<string> _log;
            public Scope(List<string> log) => _log = log;
            public void Dispose() => _log.Add("session.dispose");
        }
    }
}
