using System;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop.Linux.Native
{
    public static class Glib
    {
        private const string LibraryName = "libglib-2.0.so.0";

        public struct GHashTable { /* transparent */ }

        [StructLayout(LayoutKind.Sequential)]
        public struct GList
        {
            public IntPtr data;
            public IntPtr next;
            public IntPtr prev;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GError
        {
            public int domain;
            public int code;
            public IntPtr message;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint GHashFunc(IntPtr key);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool GEqualFunc(IntPtr a, IntPtr b);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GDestroyNotify(IntPtr data);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint g_str_hash(IntPtr key);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool g_str_equal(IntPtr a, IntPtr b);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe GHashTable* g_hash_table_new(GHashFunc hash_func, GEqualFunc key_equal_func);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe GHashTable* g_hash_table_new_full(
            GHashFunc hash_func,
            GEqualFunc key_equal_func,
            GDestroyNotify key_destroy_func,
            GDestroyNotify value_destroy_func);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void g_hash_table_destroy(GHashTable* hash_table);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool g_hash_table_insert(GHashTable* hash_table, IntPtr key, IntPtr value);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr g_hash_table_lookup(GHashTable* hash_table, IntPtr key);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void g_list_free_full(GList* list, GDestroyNotify free_func);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void g_hash_table_unref(GHashTable* hash_table);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void g_error_free(GError* error);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void g_free(IntPtr mem);
    }
}
