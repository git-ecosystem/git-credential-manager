// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;
using System.Text;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.GLib;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.Libsecret;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.Libsecret.SecretSchemaAttributeType;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.Libsecret.SecretSchemaFlags;

namespace Microsoft.Git.CredentialManager.Interop.Linux
{
    public class LibsecretCollection : ICredentialStore
    {
        private const string SchemaName = "com.microsoft.GitCredentialManager";
        private const string KeyAttributeName = "key";
        private const string UserAttributeName = "user";
        private const string PlainTextContentType = "plain/text";

        private static readonly SecretSchema Schema;

        #region Constructors

        /// <summary>
        /// Open the default secret collection for the current user.
        /// </summary>
        /// <returns>Default secret collection.</returns>
        public static LibsecretCollection Open()
        {
            return new LibsecretCollection();
        }

        static LibsecretCollection()
        {
            Schema = new SecretSchema
            {
                name = SchemaName,
                flags = SECRET_SCHEMA_DONT_MATCH_NAME,
                attributes = new SecretSchemaAttribute[32]
            };

            Schema.attributes[0] = new SecretSchemaAttribute
            {
                name = KeyAttributeName,
                type = SECRET_SCHEMA_ATTRIBUTE_STRING
            };

            Schema.attributes[1] = new SecretSchemaAttribute
            {
                name = UserAttributeName,
                type = SECRET_SCHEMA_ATTRIBUTE_STRING
            };
        }

        private LibsecretCollection()
        {
            PlatformUtils.EnsureLinux();
        }

        #endregion

        #region ICredentialStore

        public unsafe ICredential Get(string key)
        {
            GHashTable* queryAttrs = null;
            IntPtr keyAttrKey = IntPtr.Zero;
            IntPtr keyAttrValue = IntPtr.Zero;
            IntPtr userKey = IntPtr.Zero;

            GError error = null;

            try
            {
                // Build search query
                queryAttrs = g_hash_table_new(g_str_hash, g_str_equal);
                keyAttrKey = Marshal.StringToHGlobalAnsi(KeyAttributeName);
                keyAttrValue = Marshal.StringToHGlobalAnsi(key);

                if (!g_hash_table_insert(queryAttrs, keyAttrKey, keyAttrValue))
                {
                    throw new InteropException("Failed to add key to search query hash table.", -1);
                }

                // Execute search query and return one result (with secrets values pre-loaded)
                IntPtr itemListPtr = secret_service_search_sync(
                    IntPtr.Zero,
                    Schema,
                    queryAttrs,
                    SecretSearchFlags.SECRET_SEARCH_LOAD_SECRETS,
                    IntPtr.Zero,
                    out error);

                // Handle errors
                if (error != null)
                {
                    string msg = Encoding.UTF8.GetString(error.message);
                    int code = error.code;
                    g_error_free(error);
                    throw new InteropException("Failed to search for credentials.", code, new Exception(msg));
                }

                if (itemListPtr != IntPtr.Zero)
                {
                    GList itemList = Marshal.PtrToStructure<GList>(itemListPtr);
                    if (itemList != null && itemList.data != IntPtr.Zero)
                    {
                        IntPtr itemPtr = itemList.data;

                        // Extract the user attribute
                        GHashTable* secretAttrs = secret_item_get_attributes(itemPtr);
                        userKey = Marshal.StringToHGlobalAnsi(UserAttributeName);
                        IntPtr userValue = g_hash_table_lookup(secretAttrs, userKey);
                        string userName = Marshal.PtrToStringAnsi(userValue);

                        // Extract the secret/password
                        SecretValue* secretValue = secret_item_get_secret(itemPtr);
                        IntPtr passwordPtr = secret_value_get(secretValue, out int passwordLength);
                        byte[] passwordBytes = new byte[passwordLength];
                        Marshal.Copy(passwordPtr, passwordBytes, 0, passwordBytes.Length);
                        string password = Encoding.UTF8.GetString(passwordBytes);

                        return new GitCredential(userName, password);
                    }
                }

                return null;
            }
            finally
            {
                if (queryAttrs != null) g_hash_table_destroy(queryAttrs);
                if (keyAttrKey != IntPtr.Zero) Marshal.FreeHGlobal(keyAttrKey);
                if (keyAttrValue != IntPtr.Zero) Marshal.FreeHGlobal(keyAttrValue);
                if (userKey != IntPtr.Zero) Marshal.FreeHGlobal(userKey);
                if (error != null) g_error_free(error);
            }
        }

        public unsafe void AddOrUpdate(string key, ICredential credential)
        {
            GHashTable* attributes = null;
            IntPtr userAttrKey = IntPtr.Zero;
            IntPtr userAttrValue = IntPtr.Zero;
            IntPtr keyAttrKey = IntPtr.Zero;
            IntPtr keyAttrValue = IntPtr.Zero;

            GError error = null;

            try
            {
                // Create attributes for the key and user
                attributes = g_hash_table_new(g_str_hash, g_str_equal);

                keyAttrKey = Marshal.StringToHGlobalAnsi(KeyAttributeName);
                keyAttrValue = Marshal.StringToHGlobalAnsi(key);
                if (!g_hash_table_insert(attributes, keyAttrKey, keyAttrValue))
                {
                    throw new InteropException("Failed to add key to attribute hash table.", -1);
                }

                userAttrKey = Marshal.StringToHGlobalAnsi(UserAttributeName);
                userAttrValue = Marshal.StringToHGlobalAnsi(credential.UserName);
                if (!g_hash_table_insert(attributes, userAttrKey, userAttrValue))
                {
                    throw new InteropException("Failed to add user to attribute hash table.", -1);
                }

                // Create the secret value from the password
                byte[] passwordBytes = Encoding.UTF8.GetBytes(credential.Password);
                SecretValue* secretValue = secret_value_new(passwordBytes, passwordBytes.Length, PlainTextContentType);

                // Store the secret with the associated attributes
                bool result = secret_service_store_sync(
                    IntPtr.Zero,
                    Schema,
                    attributes,
                    null,
                    key, // Use the key also as the label
                    secretValue,
                    IntPtr.Zero,
                    out error);

                // Handle errors
                if (error != null)
                {
                    string msg = Encoding.UTF8.GetString(error.message);
                    int code = error.code;
                    g_error_free(error);
                    throw new InteropException("Failed to store credentials.", code, new Exception(msg));
                }

                if (!result)
                {
                    throw new InteropException("Failed to store credentials.", -1);
                }
            }
            finally
            {
                if (attributes != null) g_hash_table_destroy(attributes);
                if (userAttrKey != IntPtr.Zero) Marshal.FreeHGlobal(userAttrKey);
                if (userAttrValue != IntPtr.Zero) Marshal.FreeHGlobal(userAttrValue);
                if (keyAttrKey != IntPtr.Zero) Marshal.FreeHGlobal(keyAttrKey);
                if (keyAttrValue != IntPtr.Zero) Marshal.FreeHGlobal(keyAttrValue);
                if (error != null) g_error_free(error);
            }
        }

        public bool Remove(string key)
        {
            unsafe
            {
                GHashTable* attributes = null;
                IntPtr keyAttrKey = IntPtr.Zero;
                IntPtr keyAttrValue = IntPtr.Zero;

                GError error = null;

                try
                {
                    // Create attributes for the key
                    attributes = g_hash_table_new(g_str_hash, g_str_equal);
                    keyAttrKey = Marshal.StringToHGlobalAnsi(KeyAttributeName);
                    keyAttrValue = Marshal.StringToHGlobalAnsi(key);
                    if (!g_hash_table_insert(attributes, keyAttrKey, keyAttrValue))
                    {
                        throw new InteropException("Failed to add key to delete query hash table.", -1);
                    }

                    // Erase the secret with the specified key
                    bool result = secret_service_clear_sync(
                        IntPtr.Zero,
                        Schema,
                        attributes,
                        IntPtr.Zero,
                        out error);

                    // Handle errors
                    if (error != null)
                    {
                        string msg = Encoding.UTF8.GetString(error.message);
                        int code = error.code;
                        g_error_free(error);
                        throw new InteropException("Failed to erase the credentials.", code, new Exception(msg));
                    }

                    return result;
                }
                finally
                {
                    if (attributes != null) g_hash_table_destroy(attributes);
                    if (keyAttrKey != IntPtr.Zero) Marshal.FreeHGlobal(keyAttrKey);
                    if (keyAttrValue != IntPtr.Zero) Marshal.FreeHGlobal(keyAttrValue);
                    if (error != null) g_error_free(error);
                }
            }
        }

        #endregion
    }
}
