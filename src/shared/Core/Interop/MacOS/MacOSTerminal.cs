using System;
using GitCredentialManager.Interop.MacOS.Native;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Posix.Native;

namespace GitCredentialManager.Interop.MacOS
{
    public class MacOSTerminal : PosixTerminal
    {
        public MacOSTerminal(ITrace trace, ITrace2 trace2)
            : base(trace, trace2) { }

        protected override IDisposable CreateTtyContext(int fd, bool echo)
        {
            return new TtyContext(Trace, Trace2, fd, echo);
        }

        private class TtyContext : IDisposable
        {
            private readonly ITrace _trace;
            private readonly int _fd;

            private termios_MacOS _originalTerm;
            private bool _isDisposed;

            public TtyContext(ITrace trace, ITrace2 trace2, int fd, bool echo)
            {
                EnsureArgument.NotNull(trace, nameof(trace));
                EnsureArgument.PositiveOrZero(fd, nameof(fd));

                _trace = trace;
                _fd = fd;

                int error = 0;

                // Capture current terminal settings so we can restore them later
                if ((error = Termios_MacOS.tcgetattr(_fd, out termios_MacOS t)) != 0)
                {
                    throw new Trace2InteropException(trace2, "Failed to get initial terminal settings", error);
                }

                _originalTerm = t;

                // Set desired echo state
                _trace.WriteLine($"Setting terminal echo state to '{echo}'");
                if (echo)
                    t.c_lflag |= LocalFlags.ECHO;
                else
                    t.c_lflag &= ~LocalFlags.ECHO;

                if ((error = Termios_MacOS.tcsetattr(_fd, SetActionFlags.TCSAFLUSH, ref t)) != 0)
                {
                    throw new Trace2InteropException(trace2, "Failed to set terminal settings", error);
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
                if ((error = Termios_MacOS.tcsetattr(_fd, SetActionFlags.TCSAFLUSH, ref _originalTerm)) != 0)
                {
                    _trace.WriteLine($"Failed to get restore terminal settings (error: {error:x}");
                }

                _isDisposed = true;
            }
        }
    }
}
