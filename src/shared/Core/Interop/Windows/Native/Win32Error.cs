using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop.Windows.Native
{
    // https://docs.microsoft.com/en-gb/windows/desktop/Debug/system-error-codes

    /// <summary>
    /// The System Error Codes are very broad.
    /// <para/>
    /// Each one can occur in one of many hundreds of locations in the system.
    /// <para/>
    /// Consequently the descriptions of these codes cannot be very specific.
    /// <para/>
    /// Use of these codes requires some amount of investigation and analysis.
    /// <para/>
    /// You need to note both the programmatic and the run-time context in which these errors occur.
    /// <para/>
    /// Because these codes are defined in WinError.h for anyone to use, sometimes the codes are returned by non-system software.
    /// <para/>
    /// Sometimes the code is returned by a function deep in the stack and far removed from your code that is handling the error.
    /// </summary>
    internal static class Win32Error
    {
        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        public const int Success = 0;

        /// <summary>
        /// The system cannot find the file specified.
        /// </summary>
        public const int FileNotFound = 2;

        /// <summary>
        /// The handle is invalid.
        /// </summary>
        public const int InvalidHandle = 6;

        /// <summary>
        /// Not enough storage is available to process this command.
        /// </summary>
        public const int NotEnoughMemory = 8;

        /// <summary>
        /// A device attached to the system is not functioning.
        /// </summary>
        public const int GenericFailure = 31;

        /// <summary>
        /// The process cannot access the file because it is being used by another process.
        /// </summary>
        public const int SharingViolation = 32;

        /// <summary>
        /// The file exists.
        /// </summary>
        public const int FileExists = 80;

        /// <summary>
        /// The data area passed to a system call is too small.
        /// </summary>
        public const int InsufficientBuffer = 122;

        /// <summary>
        /// Cannot create a file when that file already exists.
        /// </summary>
        public const int AlreadyExists = 183;

        /// <summary>
        /// The implementation is not capable of performing the request.
        /// </summary>
        public const int NotCapable = 775;

        /// <summary>
        /// Element not found.
        /// </summary>
        public const int NotFound = 1168;

        /// <summary>
        /// The operation was canceled by the user.
        /// </summary>
        public const int Cancelled = 1223;

        /// <summary>
        /// A specified logon session does not exist. It may already have been terminated.
        /// </summary>
        public const int NoSuchLogonSession = 1312;

        public static int GetLastError(bool success)
        {
            if (success)
            {
                return Success;
            }

            return Marshal.GetLastWin32Error();
        }

        /// <summary>
        /// Throw an <see cref="InteropException"/> if <paramref name="succeeded"/> is not true.
        /// </summary>
        /// <param name="trace2">The application's TRACE2 tracer.</param>
        /// <param name="succeeded">Windows API return code.</param>
        /// <param name="defaultErrorMessage">Default error message.</param>
        /// <exception cref="InteropException">Throw if <paramref name="succeeded"/> is not true.</exception>
        public static void ThrowIfError(ITrace2 trace2, bool succeeded, string defaultErrorMessage = "Unknown error.")
        {
            ThrowIfError(GetLastError(succeeded), defaultErrorMessage, trace2);
        }

        /// <summary>
        /// Throw an <see cref="InteropException"/> if <paramref name="succeeded"/> is not true.
        /// </summary>
        /// <param name="succeeded">Windows API return code.</param>
        /// <param name="defaultErrorMessage">Default error message.</param>
        /// <exception cref="InteropException">Throw if <paramref name="succeeded"/> is not true.</exception>
        public static void ThrowIfError(bool succeeded, string defaultErrorMessage = "Unknown error.")
        {
            ThrowIfError(GetLastError(succeeded), defaultErrorMessage);
        }

        /// <summary>
        /// Throw an <see cref="InteropException"/> if <paramref name="error"/> is not <see cref="Success"/>.
        /// </summary>
        /// <param name="error">Windows API error code.</param>
        /// <param name="defaultErrorMessage">Default error message.</param>
        /// <param name="trace2">The application's TRACE2 tracer.</param>
        /// <exception cref="InteropException">Throw if <paramref name="error"/> is not <see cref="Success"/>.</exception>
        public static void ThrowIfError(int error, string defaultErrorMessage = "Unknown error.", ITrace2 trace2 = null)
        {
            switch (error)
            {
                case Success:
                    return;
                default:
                    // The Win32Exception constructor will automatically get the human-readable
                    // message for the error code.
                    if (trace2 != null)
                        throw new Trace2InteropException(trace2, defaultErrorMessage, new Win32Exception(error));
                    throw new InteropException(defaultErrorMessage, new Win32Exception(error));
            }
        }
    }
}
