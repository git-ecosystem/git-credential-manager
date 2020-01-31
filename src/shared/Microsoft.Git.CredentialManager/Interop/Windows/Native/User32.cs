// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager.Interop.Windows.Native
{
    public static class User32
    {
        private const string LibraryName = "user32.dll";

        public const int UOI_FLAGS = 1;
        public const int WSF_VISIBLE = 0x0001;

        [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern IntPtr GetProcessWindowStation();

        [DllImport(LibraryName, EntryPoint = "GetUserObjectInformation", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern unsafe bool GetUserObjectInformation(IntPtr hObj, int nIndex, void* pvBuffer, uint nLength, ref uint lpnLengthNeeded);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USEROBJECTFLAGS
    {
        public int fInherit;
        public int fReserved;
        public int dwFlags;
    }
}
