using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GitCredentialManager.Interop.Posix.Native
{
    public static class Stdio
    {
        public const int EOF = -1;

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr fgets(StringBuilder sb, int size, IntPtr stream);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int fputc(int c, IntPtr stream);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int fprintf(IntPtr stream, string format, string message);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int fgetc(IntPtr stream);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int fputs(string str, IntPtr stream);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void setbuf(IntPtr stream, int size);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr fdopen(int fd, string mode);
    }
}
