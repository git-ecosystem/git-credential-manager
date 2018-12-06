using System;
using System.Collections.Generic;
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
                ThrowOnError(
                    SecKeychainFindGenericPassword(
                        IntPtr.Zero, (uint) key.Length, key, 0, null,
                        out uint passwordLength, out passwordData, out itemRef)
                );

                // Get and decode the user name from the 'account name' attribute
                byte[] userNameBytes = GetAccountNameAttributeData(itemRef);
                string userName = Encoding.UTF8.GetString(userNameBytes);

                // Decode the password from the raw data
                byte[] passwordBytes = NativeMethods.ToByteArray(passwordData, passwordLength);
                string password = Encoding.UTF8.GetString(passwordBytes);

                return new Credential(userName, password);
            }
            catch (KeyNotFoundException)
            {
                return null;
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
                SecKeychainFindGenericPassword(
                    IntPtr.Zero, (uint) key.Length, key, (uint) credential.UserName.Length, credential.UserName,
                    out uint _, out passwordData, out itemRef);

                if (itemRef != IntPtr.Zero) // Update existing entry
                {
                    ThrowOnError(
                        SecKeychainItemModifyAttributesAndData(itemRef, IntPtr.Zero, (uint) passwordBytes.Length, passwordBytes),
                        "Could not update existing item"
                    );
                }
                else // Create new entry
                {
                    ThrowOnError(
                        SecKeychainAddGenericPassword(IntPtr.Zero, (uint) key.Length, key, (uint) credential.UserName.Length,
                            credential.UserName, (uint) passwordBytes.Length, passwordBytes, out itemRef),
                        "Could not create new item"
                    );
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
                SecKeychainFindGenericPassword(
                    IntPtr.Zero, (uint) key.Length, key, 0, null,
                    out _, out passwordData, out itemRef);

                if (itemRef != IntPtr.Zero)
                {
                    ThrowOnError(
                        SecKeychainItemDelete(itemRef)
                    );

                    return true;
                }

                return false;
            }
            catch (KeyNotFoundException)
            {
                return false;
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
                tagArrayPtr = Marshal.AllocCoTaskMem(sizeof(SecKeychainAttrType));
                Marshal.Copy(new[] {(int) SecKeychainAttrType.AccountItem}, 0, tagArrayPtr, 1);

                formatArrayPtr = Marshal.AllocCoTaskMem(sizeof(CssmDbAttributeFormat));
                Marshal.Copy(new[] {(int) CssmDbAttributeFormat.String}, 0, formatArrayPtr, 1);

                var attributeInfo = new SecKeychainAttributeInfo
                {
                    Count = 1,
                    Tag = tagArrayPtr,
                    Format = formatArrayPtr,
                };

                ThrowOnError(
                    SecKeychainItemCopyAttributesAndData(
                        itemRef, ref attributeInfo,
                        IntPtr.Zero, out attrListPtr, out var _, IntPtr.Zero)
                );

                SecKeychainAttributeList attrList = Marshal.PtrToStructure<SecKeychainAttributeList>(attrListPtr);
                Debug.Assert(attrList.Count == 1, "Only expecting a list structure containing one attribute to be returned");

                byte[] attrListArrayBytes = NativeMethods.ToByteArray(
                    attrList.Attributes, Marshal.SizeOf<SecKeychainAttribute>() * attrList.Count);

                SecKeychainAttribute[] attributes = NativeMethods.ToStructArray<SecKeychainAttribute>(attrListArrayBytes);
                Debug.Assert(attributes.Length == 1, "Only expecting one attribute structure to returned from raw byte conversion");

                return NativeMethods.ToByteArray(attributes[0].Data, attributes[0].Length);
            }
            finally
            {
                if (tagArrayPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(tagArrayPtr);
                }

                if (formatArrayPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(formatArrayPtr);
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
