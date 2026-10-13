using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GitCredentialManager.Interop.Linux.Native;
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

        internal enum Operation
        {
            ConnectBus,
            OpenService,
            Search,
            Unlock,
            Store,
            Erase,
            Load,
        }

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

        public string Name => "freedesktop.org Secret Service";

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
                    SecretSearchFlags.SECRET_SEARCH_UNLOCK | SecretSearchFlags.SECRET_SEARCH_ALL,
                    IntPtr.Zero,
                    out error);

                if (error != null)
                {
                    throw CreateException(Operation.Search, error);
                }

                var credentials = new List<ICredential>();

                GList* itemPtr = results;
                while (itemPtr != null && itemPtr->data != IntPtr.Zero)
                {
                    SecretItem* item = (SecretItem*) itemPtr->data;

                    // Searches can return locked items even when unlocking was requested.
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
                            IntPtr.Zero,
                            out error
                        );

                        if (error != null || numUnlocked != 1)
                        {
                            throw CreateException(Operation.Unlock, error);
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
                    throw CreateException(Operation.Store, error);
                }

                if (!result)
                {
                    throw CreateException(Operation.Store, null);
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
                    throw CreateException(Operation.Erase, error);
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
                bool loaded = secret_item_load_secret_sync(item, IntPtr.Zero, out error);
                if (error != null || !loaded)
                {
                    throw CreateException(Operation.Load, error);
                }

                value = secret_item_get_secret(item);
                if (value == null)
                {
                    throw CreateException(Operation.Load, null);
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
            IntPtr connection = IntPtr.Zero;
            GError* error = null;

            try
            {
                // Diagnose bus connectivity separately from Secret Service availability.
                connection = Gio.g_bus_get_sync(Gio.GBusType.G_BUS_TYPE_SESSION, IntPtr.Zero, out error);
                if (error != null || connection == IntPtr.Zero)
                {
                    throw CreateException(Operation.ConnectBus, error);
                }

                SecretService* service = secret_service_get_sync(
                    SecretServiceFlags.SECRET_SERVICE_OPEN_SESSION | SecretServiceFlags.SECRET_SERVICE_LOAD_COLLECTIONS,
                    IntPtr.Zero, out error);

                if (error != null || service == null)
                {
                    throw CreateException(Operation.OpenService, error);
                }

                return service;
            }
            finally
            {
                if (error != null) g_error_free(error);
                if (connection != IntPtr.Zero) g_object_unref(connection);
            }
        }

        private static unsafe InteropException CreateException(Operation operation, GError* error)
        {
            if (error == null)
            {
                return CreateException(operation, null, -1, null);
            }

            string domain = Marshal.PtrToStringUTF8(g_quark_to_string(error->domain));
            string message = Marshal.PtrToStringUTF8(error->message);
            return CreateException(operation, domain, error->code, message);
        }

        internal static InteropException CreateException(
            Operation operation, string errorDomain, int errorCode, string errorMessage)
        {
            string message = operation switch
            {
                Operation.ConnectBus => "Failed to connect to the D-Bus session bus.",
                Operation.OpenService => "Failed to open the Secret Service session.",
                Operation.Search => "Failed to search for credentials.",
                Operation.Unlock => "Failed to unlock the secret item.",
                Operation.Store => "Failed to store credentials.",
                Operation.Erase => "Failed to erase credentials.",
                Operation.Load => "Failed to load the secret.",
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
            };

            bool serviceUnavailable = errorDomain == Gio.DBusErrorDomain &&
                (errorCode == (int)Gio.GDBusError.G_DBUS_ERROR_SERVICE_UNKNOWN ||
                 errorCode == (int)Gio.GDBusError.G_DBUS_ERROR_NAME_HAS_NO_OWNER);

            string hint;
            if (errorDomain == Gio.IOErrorDomain && errorCode == (int)Gio.GIOError.G_IO_ERROR_CANCELLED)
            {
                hint = "The operation was cancelled.";
            }
            else if (errorDomain == Libsecret.ErrorDomain && errorCode == (int)SecretError.SECRET_ERROR_IS_LOCKED)
            {
                hint = "The secret item or collection is locked. Unlock it using your Secret Service provider before retrying.";
            }
            else if (operation == Operation.ConnectBus)
            {
                hint = "Ensure a D-Bus session bus is running and accessible. In a headless session, start your shell or workload with 'dbus-run-session'.";
            }
            else if (operation == Operation.OpenService)
            {
                hint = (serviceUnavailable ? "No Secret Service provider is available. " : string.Empty) +
                    "Ensure a Secret Service provider, such as GNOME Keyring or KWallet, is installed and running on the same D-Bus session bus.";
            }
            else if (serviceUnavailable)
            {
                hint = "A required D-Bus service is unavailable. Ensure your Secret Service provider and its prompting components are installed and running.";
            }
            else if (operation == Operation.Unlock)
            {
                hint = "Unlock the item or its collection using your Secret Service provider before retrying. An unlock prompt may have been dismissed or cancelled.";
            }
            else if (operation == Operation.Store && errorMessage == null)
            {
                hint = "The collection could not be created or unlocked. A Secret Service prompt may have been dismissed or cancelled.";
            }
            else if (operation == Operation.Load && errorMessage == null)
            {
                hint = "No secret value was returned. Ensure the item and its collection are unlocked.";
            }
            else
            {
                hint = null;
            }

            if (hint != null)
            {
                message += Environment.NewLine + hint;
            }

            message += Environment.NewLine + $"See {Constants.HelpUrls.GcmCredentialStores} for more information.";

            Exception innerException = errorMessage == null ? null : new Exception($"{errorDomain}: {errorMessage}");
            return new InteropException(message, errorCode, innerException);
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
