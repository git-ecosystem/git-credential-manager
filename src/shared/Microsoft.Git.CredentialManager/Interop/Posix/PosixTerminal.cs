// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Text;
using Microsoft.Git.CredentialManager.Interop.Posix.Native;

namespace Microsoft.Git.CredentialManager.Interop.Posix
{
    /// <summary>
    /// Represents a thin wrapper around the POSIX TTY device (/dev/tty).
    /// </summary>
    public class PosixTerminal : ITerminal
    {
        private const string TtyDeviceName = "/dev/tty";
        private const byte DeleteChar = 127;

        private readonly ITrace _trace;

        public PosixTerminal(ITrace trace)
        {
            PlatformUtils.EnsurePosix();
            EnsureArgument.NotNull(trace, nameof(trace));

            _trace = trace;
        }

        public void WriteLine(string format, params object[] args)
        {
            using (var fd = new PosixFileDescriptor(TtyDeviceName, OpenFlags.O_RDWR))
            {
                if (fd.IsInvalid)
                {
                    _trace.WriteLine("Not a TTY, abandoning write line.");
                    return;
                }

                fd.Write(string.Format(format, args));
                fd.Write("\n");
            }
        }

        public string Prompt(string prompt)
        {
            return Prompt(prompt, echo: true);
        }

        public string PromptSecret(string prompt)
        {
            return Prompt(prompt, echo: false);
        }

        private string Prompt(string prompt, bool echo)
        {
            using (var fd = new PosixFileDescriptor(TtyDeviceName, OpenFlags.O_RDWR))
            {
                if (fd.IsInvalid)
                {
                    _trace.WriteLine("Not a TTY, abandoning prompt.");
                    return null;
                }

                fd.Write($"{prompt}: ");

                var sb = new StringBuilder();

                using (new TtyContext(_trace, fd, echo))
                {
                    var readBuf = new byte[1];
                    bool eol = false;
                    while (!eol)
                    {
                        int nr;
                        // Read one byte at a time
                        if ((nr = fd.Read(readBuf, 1)) != 1)
                        {
                            // Either we reached end of file or an error occured.
                            // We don't care which so let's just trace and terminate further reading.
                            _trace.WriteLine($"Exiting POSIX terminal prompt read-loop unexpectedly (nr={nr})");
                            eol = true;
                            break;
                        }

                        int c = readBuf[0];
                        switch (c)
                        {
                            case 3: // CTRL + C
                                // Since `read` is a blocking call we must manually raise the SIGINT signal
                                // when the user types CTRL+C into the terminal window.
                                int pid = Unistd.getpid();
                                _trace.WriteLine($"Intercepted SIGINT during terminal prompt read-loop - sending SIGINT to self (pid={pid})");
                                Signal.kill(pid, Signal.SIGINT);
                                break;

                            case '\n':
                                eol = true;
                                // Only need to echo the newline to move the terminal cursor down when
                                // echo is disabled. When echo is enabled the newline is written for us.
                                if (!echo)
                                {
                                    fd.Write("\n");
                                }
                                break;

                            case '\b':
                            case DeleteChar:
                                if (sb.Length > 0)
                                {
                                    sb.Remove(sb.Length - 1, 1);
                                    fd.Write("\b \b");
                                }
                                break;

                            default:
                                sb.Append((char) c);
                                break;
                        }
                    }
                    return sb.ToString();
                }
            }
        }

        private class TtyContext : IDisposable
        {
            private readonly ITrace _trace;
            private readonly int _fd;

            private termios _originalTerm;
            private bool _isDisposed;

            public TtyContext(ITrace trace, int fd, bool echo)
            {
                EnsureArgument.NotNull(trace, nameof(trace));
                EnsureArgument.PositiveOrZero(fd, nameof(fd));

                _trace = trace;
                _fd = fd;

                int error = 0;

                // Capture current terminal settings so we can restore them later
                if ((error = Termios.tcgetattr(_fd, out termios t)) != 0)
                {
                    throw new InteropException("Failed to get initial terminal settings", error);
                }

                _originalTerm = t;

                // Set desired echo state
                _trace.WriteLine($"Setting terminal echo state to '{echo}'");
                t.c_lflag &= echo
                    ? LocalFlags.ECHO
                    : ~LocalFlags.ECHO;

                if ((error = Termios.tcsetattr(_fd, SetActionFlags.TCSAFLUSH, ref t)) != 0)
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
                if ((error = Termios.tcsetattr(_fd, SetActionFlags.TCSAFLUSH, ref _originalTerm)) != 0)
                {
                    _trace.WriteLine($"Failed to get restore terminal settings (error: {error:x}");
                }

                _isDisposed = true;
            }
        }
    }
}
