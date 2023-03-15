using System;
using System.Runtime.InteropServices;
using System.Text;
using GitCredentialManager.Interop.Windows.Native;
using Microsoft.Win32.SafeHandles;

namespace GitCredentialManager.Interop.Windows
{
    /// <summary>
    /// Represents a thin wrapper around the Windows console device.
    /// </summary>
    public class WindowsTerminal : ITerminal
    {
        // ReadConsole 32768 fail, 32767 OK @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
        private const int BufferReadSize = 16 * 1024;
        private const string ConsoleInName = "CONIN$";
        private const string ConsoleOutName = "CONOUT$";

        private readonly ITrace _trace;
        private readonly ITrace2 _trace2;

        public WindowsTerminal(ITrace trace, ITrace2 trace2)
        {
            PlatformUtils.EnsureWindows();

            _trace = trace;
            _trace2 = trace2;
        }

        public void WriteLine(string format, params object[] args)
        {
            var fileAccessFlags = FileAccess.GenericRead
                                | FileAccess.GenericWrite;
            var fileAttributes = FileAttributes.Normal;
            var fileCreationDisposition = FileCreationDisposition.OpenExisting;
            var fileShareFlags = FileShare.Read
                               | FileShare.Write;

            using (SafeFileHandle stdout = Kernel32.CreateFile(fileName: ConsoleOutName,
                                                          desiredAccess: fileAccessFlags,
                                                              shareMode: fileShareFlags,
                                                     securityAttributes: IntPtr.Zero,
                                                    creationDisposition: fileCreationDisposition,
                                                     flagsAndAttributes: fileAttributes,
                                                           templateFile: IntPtr.Zero))
            {
                if (stdout.IsInvalid)
                {
                    _trace.WriteLine("Not a TTY, abandoning write line.");
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendFormat(format, args);
                sb.AppendLine();

                if (!Kernel32.WriteConsole(buffer: sb,
                              consoleOutputHandle: stdout,
                             numberOfCharsToWrite: (uint) sb.Length,
                             numberOfCharsWritten: out uint written,
                                         reserved: IntPtr.Zero))
                {
                    Win32Error.ThrowIfError(Marshal.GetLastWin32Error(), trace2: _trace2);
                }
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
            var fileAccessFlags = FileAccess.GenericRead
                                | FileAccess.GenericWrite;
            var fileAttributes = FileAttributes.Normal;
            var fileCreationDisposition = FileCreationDisposition.OpenExisting;
            var fileShareFlags = FileShare.Read
                               | FileShare.Write;

            using (SafeFileHandle stdout = Kernel32.CreateFile(fileName: ConsoleOutName,
                                                          desiredAccess: fileAccessFlags,
                                                              shareMode: fileShareFlags,
                                                     securityAttributes: IntPtr.Zero,
                                                    creationDisposition: fileCreationDisposition,
                                                     flagsAndAttributes: fileAttributes,
                                                           templateFile: IntPtr.Zero))
            using (SafeFileHandle stdin  = Kernel32.CreateFile(fileName: ConsoleInName,
                                                          desiredAccess: fileAccessFlags,
                                                              shareMode: fileShareFlags,
                                                     securityAttributes: IntPtr.Zero,
                                                    creationDisposition: fileCreationDisposition,
                                                     flagsAndAttributes: fileAttributes,
                                                           templateFile: IntPtr.Zero))
            {
                string input;
                var sb = new StringBuilder(BufferReadSize);
                uint read = 0;
                uint written = 0;

                if (stdin.IsInvalid || stdout.IsInvalid)
                {
                    _trace.WriteLine("Not a TTY, abandoning prompt.");
                    return null;
                }

                // Prompt the user
                sb.Append($"{prompt}: ");
                if (!Kernel32.WriteConsole(buffer: sb,
                              consoleOutputHandle: stdout,
                             numberOfCharsToWrite: (uint) sb.Length,
                             numberOfCharsWritten: out written,
                                         reserved: IntPtr.Zero))
                {
                    Win32Error.ThrowIfError(Marshal.GetLastWin32Error(), "Failed to write prompt text", _trace2);
                }

                sb.Clear();

                // Read input from the user
                using (new TtyContext(_trace, _trace2, stdin, echo))
                {
                    if (!Kernel32.ReadConsole(buffer: sb,
                                  consoleInputHandle: stdin,
                                 numberOfCharsToRead: BufferReadSize,
                                   numberOfCharsRead: out read,
                                            reserved: IntPtr.Zero))
                    {
                        Win32Error.ThrowIfError(Marshal.GetLastWin32Error(),
                            "Unable to read prompt input from standard input", _trace2);
                    }

                    // Record input from the user into local storage, stripping any EOL chars
                    input = sb.ToString(0, (int) read);
                    input = input.Trim('\n', '\r').Trim('\n');

                    sb.Clear();
                }

                // Write the final newline to stdout manually if we had disabled echo
                if (!echo)
                {
                    sb.Append(Environment.NewLine);
                    if (!Kernel32.WriteConsole(buffer: sb,
                                  consoleOutputHandle: stdout,
                                 numberOfCharsToWrite: (uint) sb.Length,
                                 numberOfCharsWritten: out written,
                                             reserved: IntPtr.Zero))
                    {
                        Win32Error.ThrowIfError(Marshal.GetLastWin32Error(),
                            "Failed to write final newline in secret prompting", _trace2);
                    }
                }

                return input;
            }
        }

        private class TtyContext : IDisposable
        {
            private readonly ITrace _trace;
            private readonly ITrace2 _trace2;
            private readonly SafeFileHandle _stream;

            private ConsoleMode _originalMode;
            private bool _isDisposed;

            public TtyContext(ITrace trace, ITrace2 trace2, SafeFileHandle stream, bool echo)
            {
                EnsureArgument.NotNull(stream, nameof(stream));

                _trace = trace;
                _trace2 = trace2;
                _stream = stream;

                // Capture current console mode so we can restore it later
                ConsoleMode consoleMode;
                if (!Kernel32.GetConsoleMode(consoleMode: out consoleMode, consoleHandle: stream))
                {
                    Win32Error.ThrowIfError(Marshal.GetLastWin32Error(), "Failed to get initial console mode", trace2);
                }

                _originalMode = consoleMode;

                // Set desired echo state
                _trace.WriteLine($"Setting console echo state to '{echo}'");
                if (!echo)
                {
                    ConsoleMode newConsoleMode = consoleMode ^ ConsoleMode.EchoInput;
                    if (!Kernel32.SetConsoleMode(consoleMode: newConsoleMode, consoleHandle: _stream))
                    {
                        Win32Error.ThrowIfError(
                            Marshal.GetLastWin32Error(), "Failed to set console mode", trace2);
                    }
                }
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                // Restore original console mode
                if (!Kernel32.SetConsoleMode(consoleMode: _originalMode, consoleHandle: _stream))
                {
                    _trace.WriteLine("Failed to restore console mode");
                }

                _isDisposed = true;
            }
        }
    }
}
