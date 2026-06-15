using System;
using System.IO;
using System.Text;
using GitCredentialManager.Interop.Posix.Native;
using Microsoft.Win32.SafeHandles;
using Spectre.Console;

namespace GitCredentialManager.Interop.Posix;

/// <summary>
/// Spectre.Console output adapter that writes to <c>/dev/tty</c>, bypassing
/// the helper's stdout (which is reserved for the Git credential protocol).
/// </summary>
/// <remarks>
/// The constructor opens the TTY device once and keeps the writer alive for
/// the object lifetime. Construction throws <see cref="IOException"/> when
/// the device is not available (typically a headless invocation).
/// </remarks>
public sealed class PosixAnsiConsoleOutput : IAnsiConsoleOutput, IDisposable
{
    private const string TtyDeviceName = "/dev/tty";

    private readonly int _fd;
    private readonly SafeFileHandle _handle;
    private readonly StreamWriter _writer;

    public PosixAnsiConsoleOutput()
    {
        PlatformUtils.EnsurePosix();

        _fd = Fcntl.open(TtyDeviceName, OpenFlags.O_WRONLY);
        if (_fd == -1)
        {
            throw new IOException($"Failed to open {TtyDeviceName} for writing.");
        }

        _handle = new SafeFileHandle(new IntPtr(_fd), ownsHandle: true);

        // Stream wraps the SafeFileHandle and will close it on disposal.
        var stream = new FileStream(_handle, FileAccess.Write);
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
