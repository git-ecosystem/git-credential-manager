// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager.Interop.Linux.Native
{
    public static class GLib
    {
        private const string GLibLib = "libglib-2.0.so.0";

        public struct GHashTable { /* transparent type */ }

        [StructLayout(LayoutKind.Sequential)]
        public class GList
        {
            public IntPtr data;
            public IntPtr next;
            public IntPtr prev;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class GError
        {
            public int domain;
            public int code;
            public byte[] message;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint GHashFunc(IntPtr key);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool GEqualFunc(IntPtr a, IntPtr b);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GDestroyNotify(IntPtr data);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint g_str_hash(IntPtr key);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool g_str_equal(IntPtr a, IntPtr b);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe GHashTable* g_hash_table_new(GHashFunc hash_func, GEqualFunc key_equal_func);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe GHashTable* g_hash_table_new_full(
            GHashFunc hash_func,
            GEqualFunc key_equal_func,
            GDestroyNotify key_destroy_func,
            GDestroyNotify value_destroy_func);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void g_hash_table_destroy(GHashTable* hash_table);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool g_hash_table_insert(GHashTable* hash_table, IntPtr key, IntPtr value);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr g_hash_table_lookup(GHashTable* hash_table, IntPtr key);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr g_hash_table_get_keys_as_array(
            GHashTable* hash_table,
            out int length);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void g_error_free(GError error);

        [DllImport(GLibLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void g_free(IntPtr mem);
    }
}
