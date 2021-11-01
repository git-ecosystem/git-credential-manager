using System;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop.Posix.Native
{
    public static class Fcntl
    {
        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int open(string pathname, OpenFlags flags);
    }

    [Flags]
    public enum OpenFlags
    {
        O_RDONLY    = 0,
        O_WRONLY    = 1,
        O_RDWR      = 2,
        O_CREAT     = 64,
        O_EXCL      = 128,
        O_NOCTTY    = 256,
        O_TRUNC     = 512,
        O_APPEND    = 1024,
        O_NONBLOCK  = 2048,
        O_SYNC      = 4096,
        O_NOFOLLOW  = 131072,
        O_DIRECTORY = 65536,
        O_DIRECT    = 16384,
        O_ASYNC     = 8192,
        O_LARGEFILE = 32768,
        O_CLOEXEC   = 524288,
        O_PATH      = 2097152,
    }
}
