// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Git.CredentialManager.Interop.Posix.Native
{
    public static class Unistd
    {
        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int read(int fd, byte[] buf, int count);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int write(int fd, byte[] buf, int size);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int close(int fd);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int getpid();

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int getppid();

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int geteuid();
    }
}
