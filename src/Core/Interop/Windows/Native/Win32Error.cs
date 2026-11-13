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
        /// <exception cref="InteropException">Throw if <paramref name="error"/> is not <see cref="Success"/>.</exception>
        public static void ThrowIfError(int error, string defaultErrorMessage = "Unknown error.")
        {
            switch (error)
            {
                case Success:
                    return;
                default:
                    // The Win32Exception constructor will automatically get the human-readable
                    // message for the error code.
                    throw new InteropException(defaultErrorMessage, new Win32Exception(error));
            }
        }

        /// <summary>
        /// Convert a Windows HRESULT to a Win32 error code, if possible.
        /// </summary>
        /// <param name="hr">The HRESULT to convert.</param>
        /// <returns>The corresponding Win32 error code, or null if the conversion is not possible.</returns>
        /// <remarks>
        /// <para>
        /// HRESULTs are 32-bit values with the following layout:
        /// <list type="bullet">
        /// <item><description>Bit 31: Severity (0 = Success, 1 = Failure)</description></item>
        /// <item><description>Bit 30: Reserved</description></item>
        /// <item><description>Bit 29: Customer (0 = Microsoft-defined, 1 = customer/third-party defined)</description></item>
        /// <item><description>Bits 28-27: Reserved</description></item>
        /// <item><description>Bits 26-16: Facility (the system service responsible for the error)</description></item>
        /// <item><description>Bits 15-0: Code (the specific error code)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The Win32 error codes are represented in HRESULTs with a facility code of 7 (FACILITY_WIN32)
        /// and a severity of 11 (SEVERITY_ERROR). The lower 16 bits of the HRESULT contain the original
        /// Win32 error code.
        /// </para>
        /// </remarks>
        public static int? FromHResult(int hr) =>
            (hr & unchecked((int)0xFFFF0000)) == unchecked((int)0x80070000)
                ? hr & 0xFFFF
                : null;
    }
}
