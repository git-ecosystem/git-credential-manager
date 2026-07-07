using System;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop.Windows.Native
{
    public static class Shell32
    {
        private const string LibraryName = "shell32.dll";

        /// <summary>
        /// Parses a Unicode command line string and returns an array of pointers
        /// to the command line arguments, along with a count of such arguments,
        /// in a way that is similar to the standard C run-time argv and argc values.
        /// </summary>
        /// <param name="lpCmdLine">
        /// Pointer to a null-terminated Unicode string that contains the full command line.
        /// If this parameter is an empty string the function returns the path to the current executable file.
        /// </param>
        /// <param name="pNumArgs">
        /// Pointer to an int that receives the number of array elements returned, similar to argc.
        /// </param>
        /// <returns>A pointer to an array of LPWSTR values, similar to argv.</returns>
        [DllImport("Shell32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CommandLineToArgvW(IntPtr lpCmdLine, out int pNumArgs);
    }
}
