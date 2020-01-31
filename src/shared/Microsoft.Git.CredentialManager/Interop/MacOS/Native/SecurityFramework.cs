// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager.Interop.MacOS.Native
{
    // https://developer.apple.com/documentation/security/keychain_services/keychain_items
    public static class SecurityFramework
    {
        private const string SecurityFrameworkLib = "/System/Library/Frameworks/Security.framework/Security";

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SessionGetInfo(int session, out int sessionId, out SessionAttributeBits attributes);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainAddGenericPassword(
            IntPtr keychain,
            uint serviceNameLength,
            string serviceName,
            uint accountNameLength,
            string accountName,
            uint passwordLength,
            byte[] passwordData,
            out IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainFindGenericPassword(
            IntPtr keychainOrArray,
            uint serviceNameLength,
            string serviceName,
            uint accountNameLength,
            string accountName,
            out uint passwordLength,
            out IntPtr passwordData,
            out IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemCopyAttributesAndData(
            IntPtr itemRef,
            ref SecKeychainAttributeInfo info,
            IntPtr itemClass, // SecItemClass*
            out IntPtr attrList, // SecKeychainAttributeList*
            out uint dataLength,
            IntPtr data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemModifyAttributesAndData(
            IntPtr itemRef,
            IntPtr attrList, // SecKeychainAttributeList*
            uint length,
            byte[] data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemDelete(
            IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemFreeContent(
            IntPtr attrList, // SecKeychainAttributeList*
            IntPtr data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemFreeAttributesAndData(
            IntPtr attrList, // SecKeychainAttributeList*
            IntPtr data);

        public const int CallerSecuritySession = -1;

        // https://developer.apple.com/documentation/security/1542001-security_framework_result_codes
        public const int OK = 0;
        public const int ErrorSecNoSuchKeychain = -25294;
        public const int ErrorSecInvalidKeychain = -25295;
        public const int ErrorSecAuthFailed = -25293;
        public const int ErrorSecDuplicateItem = -25299;
        public const int ErrorSecItemNotFound = -25300;
        public const int ErrorSecInteractionNotAllowed = -25308;
        public const int ErrorSecInteractionRequired = -25315;
        public const int ErrorSecNoSuchAttr = -25303;

        public static void ThrowIfError(int error, string defaultErrorMessage = "Unknown error.")
        {
            switch (error)
            {
                case OK:
                    return;
                case ErrorSecNoSuchKeychain:
                    throw new InteropException("The keychain does not exist.", error);
                case ErrorSecInvalidKeychain:
                    throw new InteropException("The keychain is not valid.", error);
                case ErrorSecAuthFailed:
                    throw new InteropException("Authorization/Authentication failed.", error);
                case ErrorSecDuplicateItem:
                    throw new InteropException("The item already exists.", error);
                case ErrorSecItemNotFound:
                    throw new InteropException("The item cannot be found.", error);
                case ErrorSecInteractionNotAllowed:
                    throw new InteropException("Interaction with the Security Server is not allowed.", error);
                case ErrorSecInteractionRequired:
                    throw new InteropException("User interaction is required.", error);
                case ErrorSecNoSuchAttr:
                    throw new InteropException("The attribute does not exist.", error);
                default:
                    throw new InteropException(defaultErrorMessage, error);
            }
        }
    }

    [Flags]
    public enum SessionAttributeBits
    {
        SessionIsRoot = 0x0001,
        SessionHasGraphicAccess = 0x0010,
        SessionHasTty = 0x0020,
        SessionIsRemote = 0x1000,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecKeychainAttributeInfo
    {
        public uint Count;
        public IntPtr Tag; // uint* (SecKeychainAttrType*)
        public IntPtr Format; // uint* (CssmDbAttributeFormat*)
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecKeychainAttributeList
    {
        public uint Count;
        public IntPtr Attributes; // SecKeychainAttribute*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecKeychainAttribute
    {
        public SecKeychainAttrType Tag;
        public uint Length;
        public IntPtr Data;
    }

    public enum CssmDbAttributeFormat : uint
    {
        String = 0,
        SInt32 = 1,
        UInt32 = 2,
        BigNum = 3,
        Real = 4,
        TimeDate = 5,
        Blob = 6,
        MultiUInt32 = 7,
        Complex = 8
    };

    public enum SecKeychainAttrType : uint
    {
        // https://developer.apple.com/documentation/security/secitemattr/accountitemattr
        AccountItem = 1633903476,
    }
}
