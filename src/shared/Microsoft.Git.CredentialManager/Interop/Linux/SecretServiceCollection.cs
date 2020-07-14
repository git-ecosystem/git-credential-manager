// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;
using System.Text;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.Gobject;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.Glib;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.Libsecret;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.Libsecret.SecretSchemaAttributeType;
using static Microsoft.Git.CredentialManager.Interop.Linux.Native.Libsecret.SecretSchemaFlags;

namespace Microsoft.Git.CredentialManager.Interop.Linux
{
    public class SecretServiceCollection : ICredentialStore
    {
        private const string SchemaName = "com.microsoft.GitCredentialManager";
        private const string KeyAttributeName = "key";
        private const string UserAttributeName = "user";
        private const string PlainTextContentType = "plain/text";

        #region Constructors

        /// <summary>
        /// Open the default secret collection for the current user.
        /// </summary>
        /// <returns>Default secret collection.</returns>
        public static SecretServiceCollection Open()
        {
            return new SecretServiceCollection();
        }

        private SecretServiceCollection()
        {
            PlatformUtils.EnsureLinux();
        }

        #endregion

        #region ICredentialStore

        public unsafe ICredential Get(string key)
        {
            GHashTable* queryAttrs = null;
            GHashTable* secretAttrs = null;
            IntPtr userKeyPtr = IntPtr.Zero;
            IntPtr passwordPtr = IntPtr.Zero;
            GList* results = null;
            GError* error = null;

            try
            {
                SecretService* service = GetSecretService();

                // Build search query
                queryAttrs = g_hash_table_new_full(
                    g_str_hash, g_str_equal,
                    Marshal.FreeHGlobal, Marshal.FreeHGlobal);
                IntPtr keyKeyPtr = Marshal.StringToHGlobalAnsi(KeyAttributeName);
                IntPtr keyValuePtr = Marshal.StringToHGlobalAnsi(key);
                g_hash_table_insert(queryAttrs, keyKeyPtr, keyValuePtr);

                SecretSchema schema = GetSchema();

                // Execute search query and return one result
                results = secret_service_search_sync(
                    service,
                    ref schema,
                    queryAttrs,
                    SecretSearchFlags.SECRET_SEARCH_UNLOCK,
                    IntPtr.Zero,
                    out error);

                if (error != null)
                {
                    int code = error->code;
                    string message = Marshal.PtrToStringAuto(error->message)!;
                    throw new InteropException("Failed to search for credentials", code, new Exception(message));
                }

                if (results != null && results->data != null)
                {
                    SecretItem* item = (SecretItem*) results->data;

                    // Although we've unlocked the collection during the search call,
                    // an item can also be individually locked within a collection.
                    // If the item is locked we should try and unlock it.
                    if (secret_item_get_locked(item))
                    {
                        var toUnlockList = new GList
                        {
                            data = (IntPtr) item,
                            next = IntPtr.Zero,
                            prev = IntPtr.Zero
                        };

                        int numUnlocked = secret_service_unlock_sync(
                            service,
                            &toUnlockList,
                            IntPtr.Zero,
                            out _,
                            out error
                        );

                        if (numUnlocked != 1)
                        {
                            throw new InteropException("Failed to unlock item", numUnlocked);
                        }
                    }

                    // Extract the user attribute
                    secretAttrs = secret_item_get_attributes(item);
                    userKeyPtr = Marshal.StringToHGlobalAnsi(UserAttributeName);
                    IntPtr userValuePtr = g_hash_table_lookup(secretAttrs, userKeyPtr);
                    string userName = Marshal.PtrToStringAuto(userValuePtr);

                    // Load the secret value
                    secret_item_load_secret_sync(item, IntPtr.Zero, out error);
                    SecretValue* value = secret_item_get_secret(item);
                    if (value == null)
                    {
                        throw new InteropException("Failed to load secret", -1);
                    }

                    // Extract the secret/password
                    passwordPtr = secret_value_unref_to_password(value, out int passwordLength);
                    string password = Marshal.PtrToStringAuto(passwordPtr, passwordLength);

                    return new GitCredential(userName, password);
                }

                return null;
            }
            finally
            {
                if (queryAttrs != null) g_hash_table_destroy(queryAttrs);
                if (secretAttrs != null) g_hash_table_unref(secretAttrs);
                if (userKeyPtr != IntPtr.Zero) Marshal.FreeHGlobal(userKeyPtr);
                if (passwordPtr != IntPtr.Zero) secret_password_free(passwordPtr);
                if (error != null) g_error_free(error);
                if (results != null) g_list_free_full(results, g_object_unref);
            }
        }

        public unsafe void AddOrUpdate(string key, ICredential credential)
        {
            GHashTable* attributes = null;
            SecretValue* secretValue = null;
            GError *error = null;

            try
            {
                SecretService* service = GetSecretService();

                // Create attributes for the key and user
                attributes = g_hash_table_new_full(g_str_hash, g_str_equal,
                    Marshal.FreeHGlobal, Marshal.FreeHGlobal);

                IntPtr keyKeyPtr = Marshal.StringToHGlobalAnsi(KeyAttributeName);
                IntPtr keyValuePtr = Marshal.StringToHGlobalAnsi(key);
                g_hash_table_insert(attributes, keyKeyPtr, keyValuePtr);

                IntPtr userKeyPtr = Marshal.StringToHGlobalAnsi(UserAttributeName);
                IntPtr userValuePtr = Marshal.StringToHGlobalAnsi(credential.UserName);
                g_hash_table_insert(attributes, userKeyPtr, userValuePtr);

                // Create the secret value from the password
                byte[] passwordBytes = Encoding.UTF8.GetBytes(credential.Password);
                secretValue = secret_value_new(passwordBytes, passwordBytes.Length, PlainTextContentType);

                SecretSchema schema = GetSchema();

                // Store the secret with the associated attributes
                bool result = secret_service_store_sync(
                    service,
                    ref schema,
                    attributes,
                    null,
                    key, // Use the key also as the label
                    secretValue,
                    IntPtr.Zero,
                    out error);

                if (error != null)
                {
                    int code = error->code;
                    string message = Marshal.PtrToStringAuto(error->message)!;
                    throw new InteropException("Failed to store credentials", code, new Exception(message));
                }

                if (!result)
                {
                    throw new InteropException("Failed to store credentials", -1);
                }
            }
            finally
            {
                if (attributes != null) g_hash_table_destroy(attributes);
                if (secretValue != null) secret_value_unref(secretValue);
                if (error != null) g_error_free(error);
            }
        }

        public unsafe bool Remove(string key)
        {
            GHashTable* attributes = null;
            GError* error = null;

            try
            {
                SecretService* service = GetSecretService();

                // Create attributes for the key
                attributes = g_hash_table_new_full(g_str_hash, g_str_equal,
                    Marshal.FreeHGlobal, Marshal.FreeHGlobal);
                IntPtr keyKeyPtr = Marshal.StringToHGlobalAnsi(KeyAttributeName);
                IntPtr keyValuePtr = Marshal.StringToHGlobalAnsi(key);
                g_hash_table_insert(attributes, keyKeyPtr, keyValuePtr);

                SecretSchema schema = GetSchema();

                // Erase the secret with the specified key
                bool result = secret_service_clear_sync(
                    service,
                    ref schema,
                    attributes,
                    IntPtr.Zero,
                    out error);

                if (error != null)
                {
                    int code = error->code;
                    string message = Marshal.PtrToStringAuto(error->message)!;
                    throw new InteropException("Failed to erase credentials", code, new Exception(message));
                }

                return result;
            }
            finally
            {
                if (attributes != null) g_hash_table_destroy(attributes);
                if (error != null) g_error_free(error);
            }
        }

        #endregion

        private static unsafe SecretService* GetSecretService()
        {
            // Get a handle to the default secret service, open a session,
            // and load all collections
            SecretService* service = secret_service_get_sync(
                SecretServiceFlags.SECRET_SERVICE_OPEN_SESSION | SecretServiceFlags.SECRET_SERVICE_LOAD_COLLECTIONS,
                IntPtr.Zero, out GError* error);

            if (error != null)
            {
                int code = error->code;
                string message = Marshal.PtrToStringAuto(error->message)!;
                g_error_free(error);
                throw new InteropException("Failed to open secret service session", code, new Exception(message));
            }

            return service;
        }

        private static SecretSchema GetSchema()
        {
            var schema = new SecretSchema
            {
                name = SchemaName,
                flags = SECRET_SCHEMA_DONT_MATCH_NAME,
                attributes = new SecretSchemaAttribute[32]
            };

            schema.attributes[0] = new SecretSchemaAttribute
            {
                name = KeyAttributeName,
                type = SECRET_SCHEMA_ATTRIBUTE_STRING
            };

            schema.attributes[1] = new SecretSchemaAttribute
            {
                name = UserAttributeName,
                type = SECRET_SCHEMA_ATTRIBUTE_STRING
            };

            return schema;
        }
    }
}
