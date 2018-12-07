using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static Microsoft.Git.CredentialManager.SecureStorage.NativeMethods.MacOS;

namespace Microsoft.Git.CredentialManager.SecureStorage
{
    public class MacOSKeychain : ICredentialStore
    {
        #region Constructors

        /// <summary>
        /// Open the default keychain (current user's login keychain).
        /// </summary>
        /// <returns>Default keychain.</returns>
        public static MacOSKeychain Open()
        {
            return new MacOSKeychain();
        }

        private MacOSKeychain()
        {
            PlatformUtils.EnsureMacOS();
        }

        #endregion

        #region ICredentialStore

        public ICredential Get(string key)
        {
            IntPtr passwordData = IntPtr.Zero;
            IntPtr itemRef = IntPtr.Zero;

            try
            {
                // Find the item (itemRef) and password (passwordData) in the keychain
                int findResult = SecKeychainFindGenericPassword(
                    IntPtr.Zero, (uint) key.Length, key, 0, null,
                    out uint passwordLength, out passwordData, out itemRef);

                switch (findResult)
                {
                    case OK:
                        // Get and decode the user name from the 'account name' attribute
                        byte[] userNameBytes = GetAccountNameAttributeData(itemRef);
                        string userName = Encoding.UTF8.GetString(userNameBytes);

                        // Decode the password from the raw data
                        byte[] passwordBytes = NativeMethods.ToByteArray(passwordData, passwordLength);
                        string password = Encoding.UTF8.GetString(passwordBytes);

                        return new Credential(userName, password);

                    case ErrorSecItemNotFound:
                        return null;

                    default:
                        ThrowIfError(findResult);
                        return null;
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

        public void AddOrUpdate(string key, ICredential credential)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(credential.Password);

            IntPtr passwordData = IntPtr.Zero;
            IntPtr itemRef = IntPtr.Zero;

            try
            {
                // Check if an entry already exists in the keychain
                int findResult = SecKeychainFindGenericPassword(
                    IntPtr.Zero, (uint) key.Length, key, (uint) credential.UserName.Length, credential.UserName,
                    out uint _, out passwordData, out itemRef);

                switch (findResult)
                {
                    // Create new entry
                    case OK:
                        ThrowIfError(
                            SecKeychainItemModifyAttributesAndData(itemRef, IntPtr.Zero, (uint) passwordBytes.Length, passwordBytes),
                            "Could not update existing item"
                        );
                        break;

                    // Update existing entry
                    case ErrorSecItemNotFound:
                        ThrowIfError(
                            SecKeychainAddGenericPassword(IntPtr.Zero, (uint) key.Length, key, (uint) credential.UserName.Length,
                                credential.UserName, (uint) passwordBytes.Length, passwordBytes, out itemRef),
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

        public bool Remove(string key)
        {
            IntPtr passwordData = IntPtr.Zero;
            IntPtr itemRef = IntPtr.Zero;

            try
            {
                int findResult = SecKeychainFindGenericPassword(
                    IntPtr.Zero, (uint) key.Length, key, 0, null,
                    out _, out passwordData, out itemRef);

                switch (findResult)
                {
                    case OK:
                        ThrowIfError(
                            SecKeychainItemDelete(itemRef)
                        );
                        return true;

                    case ErrorSecItemNotFound:
                        return false;

                    default:
                        ThrowIfError(findResult);
                        return false;
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

        #endregion

        #region Private Methods

        private static byte[] GetAccountNameAttributeData(IntPtr itemRef)
        {
            IntPtr tagArrayPtr = IntPtr.Zero;
            IntPtr formatArrayPtr = IntPtr.Zero;
            IntPtr attrListPtr = IntPtr.Zero; // SecKeychainAttributeList

            try
            {
                // Extract the user name by querying for the item's 'account' attribute
                tagArrayPtr = Marshal.AllocHGlobal(sizeof(SecKeychainAttrType));
                Marshal.WriteInt32(tagArrayPtr, (int) SecKeychainAttrType.AccountItem);

                formatArrayPtr = Marshal.AllocHGlobal(sizeof(CssmDbAttributeFormat));
                Marshal.WriteInt32(formatArrayPtr, (int) CssmDbAttributeFormat.String);

                var attributeInfo = new SecKeychainAttributeInfo
                {
                    Count = 1,
                    Tag = tagArrayPtr,
                    Format = formatArrayPtr,
                };

                ThrowIfError(
                    SecKeychainItemCopyAttributesAndData(
                        itemRef, ref attributeInfo,
                        IntPtr.Zero, out attrListPtr, out _, IntPtr.Zero)
                );

                SecKeychainAttributeList attrList = Marshal.PtrToStructure<SecKeychainAttributeList>(attrListPtr);
                Debug.Assert(attrList.Count == 1, "Only expecting a list structure containing one attribute to be returned");

                SecKeychainAttribute attribute = Marshal.PtrToStructure<SecKeychainAttribute>(attrList.Attributes);

                return NativeMethods.ToByteArray(attribute.Data, attribute.Length);
            }
            finally
            {
                if (tagArrayPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tagArrayPtr);
                }

                if (formatArrayPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(formatArrayPtr);
                }

                if (attrListPtr != IntPtr.Zero)
                {
                    SecKeychainItemFreeAttributesAndData(attrListPtr, IntPtr.Zero);
                }
            }
        }

        #endregion
    }
}
