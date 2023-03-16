using System;
using System.Text;
using GitCredentialManager.Interop.Posix.Native;

namespace GitCredentialManager.Interop.Posix
{
    /// <summary>
    /// Represents a thin wrapper around the POSIX TTY device (/dev/tty).
    /// </summary>
    public abstract class PosixTerminal : ITerminal
    {
        private const string TtyDeviceName = "/dev/tty";
        private const byte DeleteChar = 127;

        protected readonly ITrace Trace;
        protected readonly ITrace2 Trace2;

        public PosixTerminal(ITrace trace, ITrace2 trace2)
        {
            PlatformUtils.EnsurePosix();
            EnsureArgument.NotNull(trace, nameof(trace));

            Trace = trace;
        }

        public void WriteLine(string format, params object[] args)
        {
            using (var fd = new PosixFileDescriptor(TtyDeviceName, OpenFlags.O_RDWR))
            {
                if (fd.IsInvalid)
                {
                    Trace.WriteLine("Not a TTY, abandoning write line.");
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

        protected abstract IDisposable CreateTtyContext(int fd, bool echo);

        private string Prompt(string prompt, bool echo)
        {
            using (var fd = new PosixFileDescriptor(TtyDeviceName, OpenFlags.O_RDWR))
            {
                if (fd.IsInvalid)
                {
                    Trace.WriteLine("Not a TTY, abandoning prompt.");
                    return null;
                }

                fd.Write($"{prompt}: ");

                var sb = new StringBuilder();

                using (CreateTtyContext(fd, echo))
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
                            Trace.WriteLine($"Exiting POSIX terminal prompt read-loop unexpectedly (nr={nr})");
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
                                Trace.WriteLine($"Intercepted SIGINT during terminal prompt read-loop - sending SIGINT to self (pid={pid})");
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
    }
}
