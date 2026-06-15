using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Interop.Windows.Native;
using GitCredentialManager.Tty;
using Microsoft.Win32.SafeHandles;
using Spectre.Console;
using NativeFileAccess = GitCredentialManager.Interop.Windows.Native.FileAccess;
using NativeFileShare = GitCredentialManager.Interop.Windows.Native.FileShare;
using NativeFileAttributes = GitCredentialManager.Interop.Windows.Native.FileAttributes;

namespace GitCredentialManager.Interop.Windows;

/// <summary>
/// Spectre.Console input adapter that reads from the Windows console
/// (<c>CONIN$</c>) in raw mode, bypassing the helper's stdin (which is
/// reserved for the Git credential protocol).
/// </summary>
/// <remarks>
/// <para>
/// The Windows console exposes pre-parsed <see cref="INPUT_RECORD"/>
/// events through <c>ReadConsoleInput</c> — no ANSI escape parsing needed.
/// Each <c>KEY_EVENT</c> with <c>bKeyDown == true</c> translates directly into
/// a <see cref="ConsoleKeyInfo"/> via <see cref="ToConsoleKeyInfo"/>.
/// </para>
/// <para>
/// The constructor opens <c>CONIN$</c> and records the original console mode,
/// but leaves the console in its normal (cooked) mode. Raw mode (clears
/// <c>LineInput</c>, <c>EchoInput</c>, and <c>ProcessedInput</c>) is entered
/// only for the duration of each keystroke read and restored immediately
/// afterwards.
/// </para>
/// <para>
/// During a read, <c>ProcessedInput</c> is off, so Ctrl+C arrives as an
/// ordinary key event that <see cref="ReadKey"/> detects and turns into an
/// <see cref="InterruptedException"/> (handled at the top level, which exits
/// 130). Reading the keystroke ourselves — rather than relying on
/// <c>CTRL_C_EVENT</c> — is what makes the prompt case work even when GCM is
/// not the console's foreground process (for example under <c>dotnet run</c>),
/// mirroring the POSIX adapter's 0x03-byte handling. Construction throws
/// <see cref="IOException"/> when <c>CONIN$</c> cannot be opened.
/// </para>
/// </remarks>
public sealed class WindowsAnsiConsoleInput : DisposableObject, IAnsiConsoleInput, IRawModeSessionInput
{
    private const string ConsoleInName = "CONIN$";

    private readonly object _modeLock = new();
    private readonly SafeFileHandle _handle;
    private readonly ConsoleMode _originalMode;

    // True while the console is in raw mode. Raw mode is reference-counted:
    // every keystroke read and every interactive session takes a hold (see
    // BeginRawModeSession), and the console is returned to cooked mode only once
    // the last hold is released. Holding raw mode across a whole prompt rather
    // than re-entering it per keystroke stops the console echoing fast-typed
    // characters between reads. Guarded by _modeLock.
    private bool _inRawMode;
    private int _rawDepth;

    public WindowsAnsiConsoleInput()
    {
        PlatformUtils.EnsureWindows();

        _handle = Kernel32.CreateFile(
            fileName: ConsoleInName,
            desiredAccess: NativeFileAccess.GenericRead | NativeFileAccess.GenericWrite,
            shareMode: NativeFileShare.Read | NativeFileShare.Write,
            securityAttributes: IntPtr.Zero,
            creationDisposition: FileCreationDisposition.OpenExisting,
            flagsAndAttributes: NativeFileAttributes.Normal,
            templateFile: IntPtr.Zero);

        if (_handle.IsInvalid)
        {
            _handle.Dispose();
            throw new IOException($"Failed to open {ConsoleInName} for reading.");
        }

        if (!Kernel32.GetConsoleMode(_handle, out _originalMode))
        {
            _handle.Dispose();
            throw new IOException($"Failed to read initial console mode on {ConsoleInName}.");
        }

        // The console is left in its normal (cooked) mode except during an
        // active keystroke read (see ReadKey). That is what keeps Ctrl+C working
        // when GCM is doing other work — MSAL polling, a GUI, the network — and
        // not just while a Spectre prompt happens to be reading: with
        // ProcessedInput on, Ctrl+C raises CTRL_C_EVENT and the runtime's
        // default handler terminates the process.
    }

    // Switch the console into raw mode (no echo, no line input, no system Ctrl+C
    // handling) for the duration of a read. Caller must hold _modeLock.
    private void EnterRawMode()
    {
        ConsoleMode rawMode = _originalMode
            & ~(ConsoleMode.LineInput | ConsoleMode.EchoInput | ConsoleMode.ProcessedInput);

        if (!Kernel32.SetConsoleMode(_handle, rawMode))
        {
            throw new IOException($"Failed to enter raw console mode on {ConsoleInName}.");
        }

        _inRawMode = true;
    }

    // Restore the original (cooked) console mode after a read. Idempotent.
    // Caller must hold _modeLock.
    private void RestoreMode()
    {
        if (!_inRawMode)
        {
            return;
        }

        if (!_handle.IsInvalid && !_handle.IsClosed)
        {
            Kernel32.SetConsoleMode(_handle, _originalMode);
        }

        _inRawMode = false;
    }

    private void AcquireRawMode()
    {
        lock (_modeLock)
        {
            if (_rawDepth++ == 0)
            {
                EnterRawMode();
            }
        }
    }

    private void ReleaseRawMode()
    {
        lock (_modeLock)
        {
            if (_rawDepth > 0 && --_rawDepth == 0)
            {
                RestoreMode();
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
        private WindowsAnsiConsoleInput _owner;
        public RawModeSessionScope(WindowsAnsiConsoleInput owner) => _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.ReleaseRawMode();
    }

    public bool IsKeyAvailable()
    {
        if (!Kernel32.GetNumberOfConsoleInputEvents(_handle, out uint count)) return false;
        return count > 0;
    }

    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        // Take a raw-mode hold for this read. Outside a prompt this enters raw
        // mode for the single read and restores it afterwards; inside an
        // interactive session the session already holds raw mode, so this just
        // nests and the console stays raw between reads.
        AcquireRawMode();
        try
        {
            var buffer = new INPUT_RECORD[1];
            while (true)
            {
                if (!Kernel32.ReadConsoleInput(_handle, buffer, 1, out uint read) || read == 0)
                {
                    return null;
                }

                INPUT_RECORD record = buffer[0];
                if (record.EventType != Kernel32.KEY_EVENT) continue;
                if (!record.KeyEvent.bKeyDown) continue;

                if (IsCtrlC(record.KeyEvent))
                {
                    // Ctrl+C: restore the console and abort the process. Does not return.
                    AbortOnInterrupt();
                }

                return ToConsoleKeyInfo(record.KeyEvent);
            }
        }
        finally
        {
            ReleaseRawMode();
        }
    }

    private static bool IsCtrlC(KEY_EVENT_RECORD ev)
    {
        bool ctrl = ev.dwControlKeyState.HasFlag(ControlKeyState.LeftCtrlPressed)
                 || ev.dwControlKeyState.HasFlag(ControlKeyState.RightCtrlPressed);
        // VK 'C' is 0x43; UnicodeChar is 0x03 (ETX) when Ctrl+C is pressed.
        return ctrl && (ev.wVirtualKeyCode == 0x43 || ev.UnicodeChar == '\u0003');
    }

    private void AbortOnInterrupt()
    {
        // Ctrl+C: restore the console mode, then throw so the operation unwinds
        // cleanly. The exception propagates up through Spectre's prompt and is
        // handled at the top level (Application.OnException), which exits with
        // code 130.
        lock (_modeLock)
        {
            // Forced restore on abort: drop all holds so a later release is a no-op.
            _rawDepth = 0;
            RestoreMode();
        }

        throw new InterruptedException();
    }

    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        // Spectre.Console may cancel pending prompts (eg. on Ctrl+C handler).
        // Run the blocking call on a thread-pool thread; if cancelled the
        // stranded thread parks on the console handle until the next event
        // arrives — acceptable for a one-shot credential-helper invocation.
        return Task.Run<ConsoleKeyInfo?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ReadKey(intercept);
        }, cancellationToken);
    }

    /// <summary>
    /// Translate a <see cref="KEY_EVENT_RECORD"/> into a
    /// <see cref="ConsoleKeyInfo"/>. Exposed as a static for unit testing.
    /// </summary>
    internal static ConsoleKeyInfo ToConsoleKeyInfo(KEY_EVENT_RECORD ev)
    {
        var ctrlState = ev.dwControlKeyState;
        bool shift = ctrlState.HasFlag(ControlKeyState.ShiftPressed);
        bool ctrl = ctrlState.HasFlag(ControlKeyState.LeftCtrlPressed)
                 || ctrlState.HasFlag(ControlKeyState.RightCtrlPressed);
        bool alt = ctrlState.HasFlag(ControlKeyState.LeftAltPressed)
                || ctrlState.HasFlag(ControlKeyState.RightAltPressed);

        var key = (ConsoleKey)ev.wVirtualKeyCode;
        return new ConsoleKeyInfo(ev.UnicodeChar, key, shift, alt, ctrl);
    }

    protected override void ReleaseManagedResources()
    {
        lock (_modeLock)
        {
            RestoreMode();
        }
        _handle.Dispose();
        base.ReleaseManagedResources();
    }
}
