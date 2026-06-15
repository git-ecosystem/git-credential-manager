using System;
using System.IO;
using System.Text;
using GitCredentialManager.Interop.Windows.Native;
using Microsoft.Win32.SafeHandles;
using Spectre.Console;
using NativeFileAccess = GitCredentialManager.Interop.Windows.Native.FileAccess;
using NativeFileShare = GitCredentialManager.Interop.Windows.Native.FileShare;
using NativeFileAttributes = GitCredentialManager.Interop.Windows.Native.FileAttributes;

namespace GitCredentialManager.Interop.Windows;

/// <summary>
/// Spectre.Console output adapter that writes to the Windows console
/// (<c>CONOUT$</c>), bypassing the helper's stdout (which is reserved for
/// the Git credential protocol).
/// </summary>
/// <remarks>
/// <para>
/// The constructor opens the console device once and keeps the writer
/// alive for the object lifetime. Construction throws <see cref="IOException"/>
/// when the device is unavailable (typically a headless invocation).
/// </para>
/// <para>
/// Enables <c>ENABLE_VIRTUAL_TERMINAL_PROCESSING</c> on the console
/// handle so Spectre's ANSI/VT escape sequences render natively
/// (Windows 10+). On older Windows where the flag isn't supported the
/// SetConsoleMode call quietly fails; Spectre's profile detection will
/// fall back to its legacy backend.
/// </para>
/// </remarks>
public sealed class WindowsAnsiConsoleOutput : IAnsiConsoleOutput, IDisposable
{
    private const string ConsoleOutName = "CONOUT$";

    private readonly SafeFileHandle _handle;
    private readonly StreamWriter _writer;

    public WindowsAnsiConsoleOutput()
    {
        PlatformUtils.EnsureWindows();

        _handle = Kernel32.CreateFile(
            fileName: ConsoleOutName,
            desiredAccess: NativeFileAccess.GenericRead | NativeFileAccess.GenericWrite,
            shareMode: NativeFileShare.Read | NativeFileShare.Write,
            securityAttributes: IntPtr.Zero,
            creationDisposition: FileCreationDisposition.OpenExisting,
            flagsAndAttributes: NativeFileAttributes.Normal,
            templateFile: IntPtr.Zero);

        if (_handle.IsInvalid)
        {
            _handle.Dispose();
            throw new IOException($"Failed to open {ConsoleOutName} for writing.");
        }

        // Try and enable Virtual Terminal Processing
        if (Kernel32.GetConsoleMode(_handle, out ConsoleMode mode))
        {
            // Ignore the return value here - older Windows versions may not support VT processing
            // and Spectre will detect this and provide a graceful fallback experience.
            Kernel32.SetConsoleMode(_handle, mode | ConsoleMode.EnableVirtualTerminalProcessing);
        }

        var stream = new FileStream(_handle, System.IO.FileAccess.Write);
        _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
        };
    }

    public TextWriter Writer => _writer;

    public bool IsTerminal => true;

    public int Width => SafeWindowWidth();

    public int Height => SafeWindowHeight();

    public void SetEncoding(Encoding encoding)
    {
        // Encoding is fixed to UTF-8 at construction; Spectre's late updates are ignored.
    }

    public void Dispose()
    {
        _writer.Dispose();
        _handle.Dispose();
    }

    private static int SafeWindowWidth()
    {
        try { return Console.WindowWidth > 0 ? Console.WindowWidth : 80; }
        catch { return 80; }
    }

    private static int SafeWindowHeight()
    {
        try { return Console.WindowHeight > 0 ? Console.WindowHeight : 24; }
        catch { return 24; }
    }
}
