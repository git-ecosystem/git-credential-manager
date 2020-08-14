// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager.Interop.Linux.Native
{
    public static class Gobject
    {
        private const string LibraryName = "libgobject-2.0.so.0";

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void g_object_unref(IntPtr @object);
    }
}
