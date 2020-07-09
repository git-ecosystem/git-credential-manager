// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.GLib;

namespace Microsoft.Git.CredentialManager.Interop.Linux.Native
{
    public static class Libsecret
    {
        private const string LibsecretLib = "libsecret-1.so.0";

        #region Schema

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

        #endregion

        #region Password

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool secret_password_storev_sync(
            in SecretSchema schema,
            ref GHashTable attributes,
            in byte[] collection,
            in byte[] label,
            in byte[] password,
            IntPtr cancellable,
            out GError error);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte[] secret_password_lookupv_sync(
            ref SecretSchema schema,
            ref GHashTable attributes,
            IntPtr cancellable,
            out GError error);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte[] secret_password_lookupv_nonpageable_sync(
            in SecretSchema schema,
            ref GHashTable attributes,
            IntPtr cancellable,
            ref GError error);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool secret_password_clearv_sync(
            in SecretSchema schema,
            ref GHashTable attributes,
            IntPtr cancellable,
            ref GError error);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void secret_password_free(
            byte[] password);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void secret_password_wipe(
            byte[] password);

        #endregion

        #region Service

        public struct SecretService { /* transparent type */ }

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

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern SecretService secret_service_get_sync(
            SecretServiceFlags flags,
            IntPtr cancellable,
            out GError error);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr secret_service_search_sync(
            IntPtr service,
            in SecretSchema schema,
            GHashTable* attributes,
            SecretSearchFlags flags,
            IntPtr cancellable,
            out GError error);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool secret_service_store_sync(
            IntPtr service,
            in SecretSchema schema,
            GHashTable *attributes,
            string collection,
            string label,
            SecretValue *value,
            IntPtr cancellable,
            out GError error);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool secret_service_clear_sync(
            IntPtr service,
            in SecretSchema schema,
            GHashTable *attributes,
            IntPtr cancellable,
            out GError error);

        #endregion

        #region Item

        public struct SecretItem { /* transparent type */ }

        public struct SecretValue { /* transparent type */ }

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern string secret_item_get_label(IntPtr self);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe GHashTable* secret_item_get_attributes(IntPtr item);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe SecretValue* secret_item_get_secret(IntPtr item);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe SecretValue* secret_value_new(
            byte[] secret,
            int length,
            string content_type);

        [DllImport(LibsecretLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr secret_value_get(SecretValue* value, out int length);

        #endregion
    }
}
