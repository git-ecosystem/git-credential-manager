using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static GitCredentialManager.Interop.Linux.Native.Gobject;
using static GitCredentialManager.Interop.Linux.Native.Glib;
using static GitCredentialManager.Interop.Linux.Native.Libsecret;
using static GitCredentialManager.Interop.Linux.Native.Libsecret.SecretSchemaAttributeType;
using static GitCredentialManager.Interop.Linux.Native.Libsecret.SecretSchemaFlags;

namespace GitCredentialManager.Interop.Linux
{
    public class SecretServiceCollection : ICredentialStore
    {
        private const string SchemaName = "com.microsoft.GitCredentialManager";
        private const string ServiceAttributeName = "service";
        private const string AccountAttributeName = "account";
        private const string PlainTextContentType = "plain/text";

        private readonly string _namespace;

        #region Constructors

        /// <summary>
        /// Open the default secret collection for the current user.
        /// </summary>
        /// <param name="namespace">Optional namespace to scope credential operations.</param>
        /// <returns>Default secret collection.</returns>
        public SecretServiceCollection(string @namespace)
        {
            PlatformUtils.EnsureLinux();
            _namespace = @namespace;
        }

        #endregion

        #region ICredentialStore

        public IList<string> GetAccounts(string service)
        {
            return Enumerate(service, null).Select(x => x.Account).Distinct().ToList();
        }

        public ICredential Get(string service, string account)
        {
            return Enumerate(service, account).FirstOrDefault();
        }

        private unsafe IEnumerable<ICredential> Enumerate(string service, string account)
        {
            GHashTable* queryAttrs = null;
            GList* results = null;
            GError* error = null;

            try
            {
                SecretService* secService = GetSecretService();

                queryAttrs = CreateSearchQuery(service, account);

                SecretSchema schema = GetSchema();

                // Execute search query and return all results
                results = secret_service_search_sync(
                    secService,
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

                var credentials = new List<ICredential>();

                GList* itemPtr = results;
                while (itemPtr != null && itemPtr->data != IntPtr.Zero)
                {
                    SecretItem* item = (SecretItem*) itemPtr->data;

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
                            secService,
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

                    credentials.Add(CreateCredentialFromItem(item));

                    itemPtr = (GList*)itemPtr->next;
                }

                return credentials;
            }
            finally
            {
                if (queryAttrs != null) g_hash_table_destroy(queryAttrs);
                if (error != null) g_error_free(error);
                if (results != null) g_list_free_full(results, g_object_unref);
            }
        }

        public unsafe void AddOrUpdate(string service, string account, string secret)
        {
            GHashTable* attributes = null;
            SecretValue* secretValue = null;
            GError *error = null;

            // If there is an existing credential that matches the same account and password
            // then don't bother writing out anything because they're the same!
            ICredential existingCred = Get(service, account);
            if (existingCred != null &&
                StringComparer.Ordinal.Equals(existingCred.Account, account) &&
                StringComparer.Ordinal.Equals(existingCred.Password, secret))
            {
                return;
            }

            try
            {
                SecretService* secService = GetSecretService();

                // Create attributes for the key and user
                attributes = g_hash_table_new_full(g_str_hash, g_str_equal,
                    Marshal.FreeHGlobal, Marshal.FreeHGlobal);

                string fullServiceName = CreateServiceName(service);
                IntPtr serviceKeyPtr = Marshal.StringToHGlobalAnsi(ServiceAttributeName);
                IntPtr serviceValuePtr = Marshal.StringToHGlobalAnsi(fullServiceName);
                g_hash_table_insert(attributes, serviceKeyPtr, serviceValuePtr);

                if (!string.IsNullOrWhiteSpace(account))
                {
                    IntPtr accountKeyPtr = Marshal.StringToHGlobalAnsi(AccountAttributeName);
                    IntPtr accountValuePtr = Marshal.StringToHGlobalAnsi(account);
                    g_hash_table_insert(attributes, accountKeyPtr, accountValuePtr);
                }

                // Create the secret value object from the secret string
                byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
                secretValue = secret_value_new(secretBytes, secretBytes.Length, PlainTextContentType);

                SecretSchema schema = GetSchema();

                // Store the secret with the associated attributes
                bool result = secret_service_store_sync(
                    secService,
                    ref schema,
                    attributes,
                    null,
                    fullServiceName, // Use full service name as label
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

        public unsafe bool Remove(string service, string account)
        {
            GHashTable* attributes = null;
            GError* error = null;

            try
            {
                SecretService* secService = GetSecretService();

                // Create search query
                attributes = CreateSearchQuery(service, account);

                SecretSchema schema = GetSchema();

                // Erase the secret with the specified key
                bool result = secret_service_clear_sync(
                    secService,
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

        private unsafe GHashTable* CreateSearchQuery(string service, string account)
        {
            // Build search query
            GHashTable* queryAttrs = g_hash_table_new_full(
                g_str_hash, g_str_equal,
                Marshal.FreeHGlobal, Marshal.FreeHGlobal);

            // If we've be given a service then filter on the service attribute
            if (!string.IsNullOrWhiteSpace(service))
            {
                string fullServiceName = CreateServiceName(service);
                IntPtr keyPtr = Marshal.StringToHGlobalAnsi(ServiceAttributeName);
                IntPtr valuePtr = Marshal.StringToHGlobalAnsi(fullServiceName);
                g_hash_table_insert(queryAttrs, keyPtr, valuePtr);
            }

            // If we've be given a username then filter on the account attribute
            if (!string.IsNullOrWhiteSpace(account))
            {
                IntPtr keyPtr = Marshal.StringToHGlobalAnsi(AccountAttributeName);
                IntPtr valuePtr = Marshal.StringToHGlobalAnsi(account);
                g_hash_table_insert(queryAttrs, keyPtr, valuePtr);
            }

            return queryAttrs;
        }

        private static unsafe ICredential CreateCredentialFromItem(SecretItem* item)
        {
            GHashTable* secretAttrs = null;
            IntPtr serviceKeyPtr = IntPtr.Zero;
            IntPtr accountKeyPtr = IntPtr.Zero;
            SecretValue* value = null;
            IntPtr passwordPtr = IntPtr.Zero;
            GError* error = null;

            try
            {
                secretAttrs = secret_item_get_attributes(item);

                // Extract the service attribute
                serviceKeyPtr = Marshal.StringToHGlobalAnsi(ServiceAttributeName);
                IntPtr serviceValuePtr = g_hash_table_lookup(secretAttrs, serviceKeyPtr);
                string service = Marshal.PtrToStringAuto(serviceValuePtr);

                // Extract the account attribute
                accountKeyPtr = Marshal.StringToHGlobalAnsi(AccountAttributeName);
                IntPtr accountValuePtr = g_hash_table_lookup(secretAttrs, accountKeyPtr);
                string account = Marshal.PtrToStringAuto(accountValuePtr);

                // Load the secret value
                secret_item_load_secret_sync(item, IntPtr.Zero, out error);
                value = secret_item_get_secret(item);
                if (value == null)
                {
                    throw new InteropException("Failed to load secret", -1);
                }

                // Extract the secret/password
                passwordPtr = secret_value_get(value, out int passwordLength);
                string password = Marshal.PtrToStringAuto(passwordPtr, passwordLength);

                return new SecretServiceCredential(service, account, password);
            }
            finally
            {
                if (secretAttrs != null) g_hash_table_unref(secretAttrs);
                if (accountKeyPtr != IntPtr.Zero) Marshal.FreeHGlobal(accountKeyPtr);
                if (serviceKeyPtr != IntPtr.Zero) Marshal.FreeHGlobal(serviceKeyPtr);
                if (value != null) secret_value_unref(value);
                if (error != null) g_error_free(error);
            }
        }

        private string CreateServiceName(string service)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(_namespace))
            {
                sb.AppendFormat("{0}:", _namespace);
            }

            sb.Append(service);
            return sb.ToString();
        }

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
                name = ServiceAttributeName,
                type = SECRET_SCHEMA_ATTRIBUTE_STRING
            };

            schema.attributes[1] = new SecretSchemaAttribute
            {
                name = AccountAttributeName,
                type = SECRET_SCHEMA_ATTRIBUTE_STRING
            };

            return schema;
        }
    }
}
