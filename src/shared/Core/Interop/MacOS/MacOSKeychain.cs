using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GitCredentialManager.Interop.MacOS.Native;
using static GitCredentialManager.Interop.MacOS.Native.CoreFoundation;
using static GitCredentialManager.Interop.MacOS.Native.SecurityFramework;

namespace GitCredentialManager.Interop.MacOS
{
    public class MacOSKeychain : ICredentialStore
    {
        private readonly string _namespace;

        #region Constructors

        /// <summary>
        /// Open the default keychain (current user's login keychain).
        /// </summary>
        /// <param name="namespace">Optional namespace to scope credential operations.</param>
        /// <returns>Default keychain.</returns>
        public MacOSKeychain(string @namespace = null)
        {
            PlatformUtils.EnsureMacOS();
            _namespace = @namespace;
        }

        #endregion

        #region ICredentialStore

        public IList<string> GetAccounts(string service)
        {
            IntPtr query = IntPtr.Zero;
            IntPtr resultPtr = IntPtr.Zero;
            IntPtr servicePtr = IntPtr.Zero;
            IntPtr accountPtr = IntPtr.Zero;

            try
            {
                query = CFDictionaryCreateMutable(
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero, IntPtr.Zero);

                CFDictionaryAddValue(query, kSecClass, kSecClassGenericPassword);
                CFDictionaryAddValue(query, kSecMatchLimit, kSecMatchLimitAll);
                CFDictionaryAddValue(query, kSecReturnAttributes, kCFBooleanTrue);

                if (!string.IsNullOrWhiteSpace(service))
                {
                    string fullService = CreateServiceName(service);
                    servicePtr = CreateCFStringUtf8(fullService);
                    CFDictionaryAddValue(query, kSecAttrService, servicePtr);
                }

                int searchResult = SecItemCopyMatching(query, out resultPtr);

                switch (searchResult)
                {
                    case OK:
                        int typeId = CFGetTypeID(resultPtr);
                        Debug.Assert(typeId == CFArrayGetTypeID(), "Returned unknown item from account query");
                        if (typeId == CFArrayGetTypeID())
                        {
                            int len = (int)CFArrayGetCount(resultPtr);
                            var accounts = new HashSet<string>(len);
                            for (int i = 0; i < len; i++)
                            {
                                IntPtr dict = CFArrayGetValueAtIndex(resultPtr, i);
                                string account = GetStringAttribute(dict, kSecAttrAccount);
                                accounts.Add(account);
                            }

                            return accounts.ToList();
                        }

                        throw new InteropException($"Unknown keychain search result type CFTypeID: {typeId}.", -1);

                    case ErrorSecItemNotFound:
                        return Array.Empty<string>();

                    default:
                        ThrowIfError(searchResult);
                        return null;
                }
            }
            finally
            {
                if (query != IntPtr.Zero) CFRelease(query);
                if (servicePtr != IntPtr.Zero) CFRelease(servicePtr);
                if (accountPtr != IntPtr.Zero) CFRelease(accountPtr);
                if (resultPtr != IntPtr.Zero) CFRelease(resultPtr);
            }
        }

        public ICredential Get(string service, string account)
        {
            IntPtr query = IntPtr.Zero;
            IntPtr resultPtr = IntPtr.Zero;
            IntPtr servicePtr = IntPtr.Zero;
            IntPtr accountPtr = IntPtr.Zero;

            try
            {
                query = CFDictionaryCreateMutable(
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero, IntPtr.Zero);

                CFDictionaryAddValue(query, kSecClass, kSecClassGenericPassword);
                CFDictionaryAddValue(query, kSecMatchLimit, kSecMatchLimitOne);
                CFDictionaryAddValue(query, kSecReturnData, kCFBooleanTrue);
                CFDictionaryAddValue(query, kSecReturnAttributes, kCFBooleanTrue);

                if (!string.IsNullOrWhiteSpace(service))
                {
                    string fullService = CreateServiceName(service);
                    servicePtr = CreateCFStringUtf8(fullService);
                    CFDictionaryAddValue(query, kSecAttrService, servicePtr);
                }

                if (!string.IsNullOrWhiteSpace(account))
                {
                    accountPtr = CreateCFStringUtf8(account);
                    CFDictionaryAddValue(query, kSecAttrAccount, accountPtr);
                }

                int searchResult = SecItemCopyMatching(query, out resultPtr);

                switch (searchResult)
                {
                    case OK:
                        int typeId = CFGetTypeID(resultPtr);
                        Debug.Assert(typeId != CFArrayGetTypeID(), "Returned more than one keychain item in search");
                        if (typeId == CFDictionaryGetTypeID())
                        {
                            return CreateCredentialFromAttributes(resultPtr);
                        }

                        throw new InteropException($"Unknown keychain search result type CFTypeID: {typeId}.", -1);

                    case ErrorSecItemNotFound:
                        return null;

                    default:
                        ThrowIfError(searchResult);
                        return null;
                }
            }
            finally
            {
                if (query != IntPtr.Zero) CFRelease(query);
                if (servicePtr != IntPtr.Zero) CFRelease(servicePtr);
                if (accountPtr != IntPtr.Zero) CFRelease(accountPtr);
                if (resultPtr != IntPtr.Zero) CFRelease(resultPtr);
            }
        }

        public void AddOrUpdate(string service, string account, string secret)
        {
            EnsureArgument.NotNullOrWhiteSpace(service, nameof(service));

            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);

            IntPtr passwordData = IntPtr.Zero;
            IntPtr itemRef = IntPtr.Zero;

            string serviceName = CreateServiceName(service);

            uint serviceNameLength = (uint) serviceName.Length;
            uint accountLength = (uint) (account?.Length ?? 0);

            try
            {
                // Check if an entry already exists in the keychain
                int findResult = SecKeychainFindGenericPassword(
                    IntPtr.Zero, serviceNameLength, serviceName, accountLength, account,
                    out uint passwordDataLength, out passwordData, out itemRef);

                switch (findResult)
                {
                    // Update existing entry only if the password/secret is different
                    case OK when !InteropUtils.AreEqual(secretBytes, passwordData, passwordDataLength):
                        ThrowIfError(
                            SecKeychainItemModifyAttributesAndData(itemRef, IntPtr.Zero, (uint) secretBytes.Length, secretBytes),
                            "Could not update existing item"
                        );
                        break;

                    // Create new entry
                    case ErrorSecItemNotFound:
                        ThrowIfError(
                            SecKeychainAddGenericPassword(IntPtr.Zero, serviceNameLength, serviceName, accountLength,
                                account, (uint) secretBytes.Length, secretBytes, out itemRef),
                            "Could not create new item"
                        );
                        break;

                    default:
                        ThrowIfError(findResult);
                        break;
                }
            }
            finally
            {
                if (passwordData != IntPtr.Zero)
                {
                    SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
                }

                if (itemRef != IntPtr.Zero)
                {
                    CFRelease(itemRef);
                }
            }
        }

        public bool Remove(string service, string account)
        {
            IntPtr query = IntPtr.Zero;
            IntPtr itemRefPtr = IntPtr.Zero;
            IntPtr servicePtr = IntPtr.Zero;
            IntPtr accountPtr = IntPtr.Zero;

            try
            {
                query = CFDictionaryCreateMutable(
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero, IntPtr.Zero);

                CFDictionaryAddValue(query, kSecClass, kSecClassGenericPassword);
                CFDictionaryAddValue(query, kSecMatchLimit, kSecMatchLimitOne);
                CFDictionaryAddValue(query, kSecReturnRef, kCFBooleanTrue);

                if (!string.IsNullOrWhiteSpace(service))
                {
                    string fullService = CreateServiceName(service);
                    servicePtr = CreateCFStringUtf8(fullService);
                    CFDictionaryAddValue(query, kSecAttrService, servicePtr);
                }

                if (!string.IsNullOrWhiteSpace(account))
                {
                    accountPtr = CreateCFStringUtf8(account);
                    CFDictionaryAddValue(query, kSecAttrAccount, accountPtr);
                }

                // Search for the credential to delete and get the SecKeychainItem ref.
                int searchResult = SecItemCopyMatching(query, out itemRefPtr);
                switch (searchResult)
                {
                    case OK:
                        // Delete the item
                        ThrowIfError(
                            SecKeychainItemDelete(itemRefPtr)
                        );
                        return true;

                    case ErrorSecItemNotFound:
                        return false;

                    default:
                        ThrowIfError(searchResult);
                        return false;
                }
            }
            finally
            {
                if (query != IntPtr.Zero) CFRelease(query);
                if (itemRefPtr != IntPtr.Zero) CFRelease(itemRefPtr);
                if (servicePtr != IntPtr.Zero) CFRelease(servicePtr);
                if (accountPtr != IntPtr.Zero) CFRelease(accountPtr);
            }
        }

        #endregion

        private static IntPtr CreateCFStringUtf8(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return CFStringCreateWithBytes(IntPtr.Zero,
                bytes, bytes.Length, CFStringEncoding.kCFStringEncodingUTF8, false);
        }

        private static ICredential CreateCredentialFromAttributes(IntPtr attributes)
        {
            string service = GetStringAttribute(attributes, kSecAttrService);
            string account = GetStringAttribute(attributes, kSecAttrAccount);
            string password = GetStringAttribute(attributes, kSecValueData);
            string label = GetStringAttribute(attributes, kSecAttrLabel);
            return new MacOSKeychainCredential(service, account, password, label);
        }

        private static string GetStringAttribute(IntPtr dict, IntPtr key)
        {
            if (dict == IntPtr.Zero)
            {
                return null;
            }

            IntPtr buffer = IntPtr.Zero;
            try
            {
                if (CFDictionaryGetValueIfPresent(dict, key, out IntPtr value) && value != IntPtr.Zero)
                {
                    if (CFGetTypeID(value) == CFStringGetTypeID())
                    {
                        int stringLength = (int)CFStringGetLength(value);
                        int bufferSize = stringLength + 1;
                        buffer = Marshal.AllocHGlobal(bufferSize);
                        if (CFStringGetCString(value, buffer, bufferSize, CFStringEncoding.kCFStringEncodingUTF8))
                        {
                            return Marshal.PtrToStringAuto(buffer, stringLength);
                        }
                    }

                    if (CFGetTypeID(value) == CFDataGetTypeID())
                    {
                        int length = CFDataGetLength(value);
                        IntPtr ptr = CFDataGetBytePtr(value);
                        return Marshal.PtrToStringAuto(ptr, length);
                    }
                }
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            return null;
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
    }
}
