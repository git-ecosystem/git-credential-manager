using System;
using GitCredentialManager.Interop.Linux.Native;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Posix.Native;

namespace GitCredentialManager.Interop.Linux;

/// <summary>
/// Linux-specific <see cref="PosixAnsiConsoleInput"/> — supplies the raw-mode
/// termios manipulation against Linux's <see cref="termios_Linux"/> struct.
/// </summary>
public sealed class LinuxAnsiConsoleInput : PosixAnsiConsoleInput
{
    public LinuxAnsiConsoleInput() { }

    protected override IDisposable EnterRawMode(PosixFileDescriptor fd)
    {
        return new RawModeContext(fd);
    }

    private sealed class RawModeContext : IDisposable
    {
        private readonly int _fd;
        private termios_Linux _original;
        private bool _isDisposed;

        public RawModeContext(int fd)
        {
            _fd = fd;

            if (Termios_Linux.tcgetattr(_fd, out termios_Linux t) != 0)
            {
                throw new System.IO.IOException("Failed to read initial terminal settings.");
            }

            _original = t;

            // Raw mode: disable echo, line buffering, and signal interpretation.
            // With ISIG off the terminal does not generate SIGINT, so Ctrl+C
            // arrives as a 0x03 byte that the base adapter reads and acts on
            // directly. This is robust to GCM not being the terminal's
            // foreground process group.
            //
            // TCSANOW (apply immediately) rather than TCSAFLUSH: this context
            // is entered and disposed around each keystroke read, so the
            // terminal is only raw while we are actively reading. Flushing on
            // every transition would discard typeahead still queued in the tty
            // buffer (e.g. the trailing Enter of a pasted "<down><enter>"),
            // hanging the next read.
            t.c_lflag &= ~(LocalFlags.ECHO | LocalFlags.ICANON | LocalFlags.ISIG);

            if (Termios_Linux.tcsetattr(_fd, SetActionFlags.TCSANOW, ref t) != 0)
            {
                throw new System.IO.IOException("Failed to enter raw terminal mode.");
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Termios_Linux.tcsetattr(_fd, SetActionFlags.TCSANOW, ref _original);
            _isDisposed = true;
        }
    }
}
