using System;
using Microsoft.Git.CredentialManager.Interop.Linux.Native;
using Microsoft.Git.CredentialManager.Interop.Posix;
using Microsoft.Git.CredentialManager.Interop.Posix.Native;

namespace Microsoft.Git.CredentialManager.Interop.Linux
{
    public class LinuxTerminal : PosixTerminal
    {
        public LinuxTerminal(ITrace trace)
            : base(trace) { }

        protected override IDisposable CreateTtyContext(int fd, bool echo)
        {
            return new TtyContext(Trace, fd, echo);
        }

        private class TtyContext : IDisposable
        {
            private readonly ITrace _trace;
            private readonly int _fd;

            private termios_Linux _originalTerm;
            private bool _isDisposed;

            public TtyContext(ITrace trace, int fd, bool echo)
            {
                EnsureArgument.NotNull(trace, nameof(trace));
                EnsureArgument.PositiveOrZero(fd, nameof(fd));

                _trace = trace;
                _fd = fd;

                int error = 0;

                // Capture current terminal settings so we can restore them later
                if ((error = Termios_Linux.tcgetattr(_fd, out termios_Linux t)) != 0)
                {
                    throw new InteropException("Failed to get initial terminal settings", error);
                }

                _originalTerm = t;

                // Set desired echo state
                _trace.WriteLine($"Setting terminal echo state to '{echo}'");
                if (echo)
                    t.c_lflag |= LocalFlags.ECHO;
                else
                    t.c_lflag &= ~LocalFlags.ECHO;

                if ((error = Termios_Linux.tcsetattr(_fd, SetActionFlags.TCSAFLUSH, ref t)) != 0)
                {
                    throw new InteropException("Failed to set terminal settings", error);
                }
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                int error = 0;

                // Restore original terminal settings
                if ((error = Termios_Linux.tcsetattr(_fd, SetActionFlags.TCSAFLUSH, ref _originalTerm)) != 0)
                {
                    _trace.WriteLine($"Failed to get restore terminal settings (error: {error:x}");
                }

                _isDisposed = true;
            }
        }
    }
}
