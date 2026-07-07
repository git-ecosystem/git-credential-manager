using System;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop.MacOS.Native
{
    public static class LibC
    {
        private const string LibCLib = "libc";

        [DllImport(LibCLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int _NSGetExecutablePath(IntPtr buf, out int bufsize);
    }
}
