// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager.Interop.MacOS.Native
{
    public static class LibSystem
    {
        private const string LibSystemLib = "/System/Library/Frameworks/System.framework/System";

        [DllImport(LibSystemLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dlopen(string name, int flags);

        [DllImport(LibSystemLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        public static IntPtr GetGlobal(IntPtr handle, string symbol)
        {
            IntPtr ptr = dlsym(handle, symbol);
            return Marshal.PtrToStructure<IntPtr>(ptr);
        }
    }
}
