using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Interop.Posix.Native;
using GitCredentialManager.Tty;
using Spectre.Console;

namespace GitCredentialManager.Interop.Posix;

/// <summary>
/// Spectre.Console input adapter that reads from <c>/dev/tty</c>, bypassing the
/// helper's stdin (which is reserved for the Git credential protocol).
/// </summary>
/// <remarks>
/// <para>
/// The constructor opens the TTY device once and keeps the fd alive for the
/// object lifetime, but leaves the terminal in its normal (cooked) mode. Raw
/// mode (echo off, line-input off, <em>signals off</em>) is entered only while
/// a keystroke read is in progress — or, for the duration of an interactive
/// prompt, held continuously via <see cref="IRawModeSessionInput"/> so fast
/// typing is not echoed between reads — and restored to cooked immediately
/// afterwards.
/// </para>
/// <para>
/// During a read, <c>ISIG</c> is off, so Ctrl+C is delivered as a raw
/// <c>0x03</c> byte that the adapter reads directly and turns into an
/// <see cref="InterruptedException"/> (handled at the top level, which exits
/// 130). Reading the byte ourselves — rather than relying on the terminal's
/// <c>SIGINT</c> — is what makes the prompt case work even when GCM is not the
/// controlling terminal's foreground process group (for example under
/// <c>dotnet run</c>, where the driver would deliver <c>SIGINT</c> to the
/// launcher instead).
/// </para>
/// <para>
/// <see cref="PosixSignalRegistration"/> handlers restore the terminal if the
/// process is killed by a signal (<c>SIGINT</c> / <c>SIGTERM</c> /
/// <c>SIGQUIT</c> / <c>SIGHUP</c>) <em>while</em> a read is holding raw mode,
/// since the runtime does not unwind our <c>finally</c> on a fatal signal.
/// <c>SIGINT</c> is included even though a Ctrl+C <em>keypress</em> during a
/// read cannot raise it (<c>ISIG</c> is off, so Ctrl+C arrives as the
/// <c>0x03</c> byte handled above): an <em>external</em> <c>SIGINT</c> — from
/// <c>kill -INT</c>, or one sent to the whole foreground process group — can
/// still be delivered mid-read, and without this handler it would terminate
/// the process leaving the user's terminal stuck in raw mode. Outside a read
/// the handler is a no-op and the runtime's default disposition terminates as
/// usual.
/// </para>
/// <para>
/// Construction throws <see cref="System.IO.IOException"/> when the device
/// is unavailable.
/// </para>
/// <para>
/// Raw mode requires platform-specific termios manipulation (the struct
/// layout differs between macOS and Linux). The platform-specific subclass
/// provides the <see cref="EnterRawMode"/> hook.
/// </para>
/// </remarks>
public abstract class PosixAnsiConsoleInput : DisposableObject, IAnsiConsoleInput, IRawModeSessionInput
{
    private const string TtyDeviceName = "/dev/tty";
    private const int EscapeDisambiguationTimeoutMs = 50;

    // ETX (Ctrl+C). Read directly rather than relying on the terminal driver to
    // raise SIGINT, so the interrupt is seen regardless of process-group status.
    private const int CtrlC = 0x03;

    // VT escape to make the cursor visible again. Spectre hides it during a
    // prompt and shows it on normal completion; on abort we re-show it ourselves
    // so the user's terminal isn't left with an invisible cursor.
    private static readonly byte[] ShowCursorAndNewline = { 0x1B, (byte)'[', (byte)'?', (byte)'2', (byte)'5', (byte)'h', (byte)'\n' };

    private readonly object _rawModeLock = new();
    private readonly PosixFileDescriptor _fd;
    private readonly PosixSignalRegistration[] _signalRegistrations;

    // Raw mode is reference-counted: every keystroke read and every interactive
    // session (see BeginRawModeSession) takes a hold, and the terminal is only
    // returned to cooked mode once the last hold is released. Holding raw mode
    // across a whole prompt — rather than re-entering it per keystroke — stops
    // the driver echoing fast-typed characters in the cooked window that would
    // otherwise open between reads. Guarded by _rawModeLock.
    private int _rawDepth;
    private IDisposable _rawMode;

    protected PosixAnsiConsoleInput()
    {
        PlatformUtils.EnsurePosix();

        _fd = new PosixFileDescriptor(TtyDeviceName, OpenFlags.O_RDWR);
        if (_fd.IsInvalid)
        {
            _fd.Dispose();
            throw new System.IO.IOException($"Failed to open {TtyDeviceName} for reading.");
        }

        // The terminal is left in its normal (cooked) mode except during an
        // active keystroke read (see ReadKey). That is what keeps Ctrl+C working
        // when GCM is doing other work — MSAL polling, a GUI, the network — and
        // not just while a Spectre prompt happens to be reading: in cooked mode
        // the terminal driver raises SIGINT and the runtime's default
        // disposition terminates the process.
        //
        // These handlers exist to restore the terminal if the process is killed
        // by a signal *while* a read is holding raw mode; the runtime does not
        // unwind our finally on a fatal signal, so without them the user's
        // terminal would be left in raw mode. SIGINT is included: although a
        // Ctrl+C keypress during a read cannot raise it (ISIG is off — Ctrl+C
        // arrives as a 0x03 byte we turn into an InterruptedException), an
        // external SIGINT (kill -INT, or one delivered to the foreground
        // process group) can still arrive mid-read. The handler restores only
        // when a read is active and otherwise no-ops, letting the runtime's
        // default disposition terminate as usual.
        _signalRegistrations = new[]
        {
            PosixSignalRegistration.Create(PosixSignal.SIGINT, OnFatalSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnFatalSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGQUIT, OnFatalSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGHUP, OnFatalSignal),
        };
    }

    /// <summary>
    /// Switch the TTY into raw mode (no echo, no line buffering, no signal
    /// generation) and return a disposable that restores the original termios
    /// state when disposed.
    /// </summary>
    protected abstract IDisposable EnterRawMode(PosixFileDescriptor fd);

    private void AcquireRawMode()
    {
        lock (_rawModeLock)
        {
            if (_rawDepth++ == 0)
            {
                _rawMode = EnterRawMode(_fd);
            }
        }
    }

    private void ReleaseRawMode()
    {
        lock (_rawModeLock)
        {
            if (_rawDepth > 0 && --_rawDepth == 0)
            {
                _rawMode?.Dispose();
                _rawMode = null;
            }
        }
    }

    IDisposable IRawModeSessionInput.BeginRawModeSession()
    {
        AcquireRawMode();
        return new RawModeSessionScope(this);
    }

    private sealed class RawModeSessionScope : IDisposable
    {
        private PosixAnsiConsoleInput _owner;
        public RawModeSessionScope(PosixAnsiConsoleInput owner) => _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.ReleaseRawMode();
    }

    private void OnFatalSignal(PosixSignalContext context)
    {
        RestoreTerminal();
    }

    private void RestoreTerminal()
    {
        lock (_rawModeLock)
        {
            if (_rawMode is null)
            {
                return;
            }

            // Forced restore (a fatal signal, or a Ctrl+C abort): drop any
            // outstanding holds and return the terminal to cooked mode. Pending
            // releases from in-flight reads or sessions then become no-ops.
            _rawMode.Dispose();
            _rawMode = null;
            _rawDepth = 0;
            try
            {
                _fd.Write(ShowCursorAndNewline, ShowCursorAndNewline.Length);
            }
            catch
            {
                // Best-effort: the fd may already be gone during shutdown.
            }
        }
    }

    private void AbortOnInterrupt()
    {
        // Ctrl+C: restore the terminal, then throw so the operation unwinds
        // cleanly. The exception propagates up through Spectre's prompt and is
        // handled at the top level.
        RestoreTerminal();
        throw new InterruptedException();
    }

    public bool IsKeyAvailable()
    {
        // We deliberately use select(2) rather than poll. On macOS, poll(2) does not
        // work on TTY / character devices - it returns immediately with 'POLLNVAL'
        // in 'revents' regardless of whether data is available, which makes a
        // poll-based readiness check useless on /dev/tty.
        // select(2) works correctly on ttys on both macOS and Linux.
        return Select.WaitReadable(_fd, timeoutMs: 0);
    }

    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        // Take a raw-mode hold for this read. Outside a prompt this enters raw
        // mode for the single read and restores it afterwards; inside an
        // interactive session the session already holds raw mode, so this just
        // nests and the terminal stays raw between reads.
        AcquireRawMode();
        try
        {
            return AnsiEscapeParser.ReadKey(ReadOneBlocking, ReadOneWithDisambiguationTimeout);
        }
        finally
        {
            ReleaseRawMode();
        }
    }

    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        // Spectre.Console may cancel pending prompts (eg. on Ctrl+C handler).
        // Run the blocking read on a thread-pool thread and observe cancellation
        // via the token. If cancelled the blocking thread remains parked on the
        // tty fd until the next keystroke arrives — acceptable for a one-shot
        // credential-helper invocation; concurrent prompts are not supported.
        return Task.Run<ConsoleKeyInfo?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ReadKey(intercept);
        }, cancellationToken);
    }

    private int ReadOneBlocking()
    {
        byte[] buf = new byte[1];
        int n = _fd.Read(buf, 1);
        if (n != 1)
        {
            return -1;
        }
        if (buf[0] == CtrlC)
        {
            // Ctrl+C: restore the terminal and abort the process. Does not return.
            AbortOnInterrupt();
        }
        return buf[0];
    }

    private int ReadOneWithDisambiguationTimeout()
    {
        // select(2), not poll(2): macOS poll() returns POLLNVAL immediately on a
        // tty rather than honouring the timeout, which would make a bare Escape
        // (no trailing bytes) fall through to a blocking read that never returns.
        if (!Select.WaitReadable(_fd, EscapeDisambiguationTimeoutMs))
        {
            return -1;
        }
        return ReadOneBlocking();
    }

    protected override void ReleaseManagedResources()
    {
        if (_signalRegistrations is not null)
        {
            foreach (PosixSignalRegistration registration in _signalRegistrations)
            {
                registration?.Dispose();
            }
        }
        lock (_rawModeLock)
        {
            _rawMode?.Dispose();
            _rawMode = null;
            _rawDepth = 0;
        }
        _fd?.Dispose();
        base.ReleaseManagedResources();
    }
}

