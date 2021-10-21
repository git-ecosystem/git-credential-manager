using System;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop.Linux.Native
{
    public static class Libsecret
    {
        private const string LibraryName = "libsecret-1.so.0";

        public enum SecretSchemaAttributeType
        {
            SECRET_SCHEMA_ATTRIBUTE_STRING = 0,
            SECRET_SCHEMA_ATTRIBUTE_INTEGER = 1,
            SECRET_SCHEMA_ATTRIBUTE_BOOLEAN = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecretSchemaAttribute
        {
            public string name;
            public SecretSchemaAttributeType type;
        }

        [Flags]
        public enum SecretSchemaFlags
        {
            SECRET_SCHEMA_NONE = 0,
            SECRET_SCHEMA_DONT_MATCH_NAME = 1 << 1,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecretSchema
        {
            public string name;
            public SecretSchemaFlags flags;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public SecretSchemaAttribute[] attributes;

            int reserved;
            IntPtr reserved1;
            IntPtr reserved2;
            IntPtr reserved3;
            IntPtr reserved4;
            IntPtr reserved5;
            IntPtr reserved6;
            IntPtr reserved7;
        }
        public struct SecretService { /* transparent */ }

        [Flags]
        public enum SecretServiceFlags
        {
            SECRET_SERVICE_NONE             = 0,
            SECRET_SERVICE_OPEN_SESSION     = 1 << 1,
            SECRET_SERVICE_LOAD_COLLECTIONS = 1 << 2,
        }

        [Flags]
        public enum SecretSearchFlags
        {
            SECRET_SEARCH_NONE         = 0,
            SECRET_SEARCH_ALL          = 1 << 1,
            SECRET_SEARCH_UNLOCK       = 1 << 2,
            SECRET_SEARCH_LOAD_SECRETS = 1 << 3,
        }

        public struct SecretItem { /* transparent */ }

        public struct SecretValue { /* transparent */ }

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe SecretService* secret_service_get_sync(
            SecretServiceFlags flags,
            IntPtr cancellable,
            out Glib.GError* error);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe Glib.GList* secret_service_search_sync(
            SecretService* service,
            ref SecretSchema schema,
            Glib.GHashTable* attributes,
            SecretSearchFlags flags,
            IntPtr cancellable,
            out Glib.GError* error);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool secret_service_store_sync(
            SecretService* service,
            ref SecretSchema schema,
            Glib.GHashTable *attributes,
            string collection,
            string label,
            SecretValue *value,
            IntPtr cancellable,
            out Glib.GError* error);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool secret_service_clear_sync(
            SecretService* service,
            ref SecretSchema schema,
            Glib.GHashTable *attributes,
            IntPtr cancellable,
            out Glib.GError* error);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern string secret_item_get_label(IntPtr self);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe Glib.GHashTable* secret_item_get_attributes(SecretItem* item);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void secret_item_load_secret_sync(
            SecretItem* self,
            IntPtr cancellable,
            out Glib.GError* error);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int secret_service_unlock_sync(SecretService* service,
            Glib.GList* objects,
            IntPtr cancellable,
            out Glib.GList* unlocked,
            out Glib.GError* error);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool secret_item_get_locked(SecretItem *self);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe SecretValue* secret_item_get_secret(SecretItem* item);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe SecretValue* secret_value_new(
            byte[] secret,
            int length,
            string content_type);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr secret_value_get(SecretValue* value, out int length);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void secret_value_unref(SecretValue* value);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr secret_value_unref_to_password(SecretValue *value, out int length);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void secret_password_free(IntPtr password);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool secret_item_delete_sync(SecretItem* self, IntPtr cancellable, out Glib.GError* error);
    }
}
