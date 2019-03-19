// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Microsoft.Git.CredentialManager.SecureStorage
{
    internal static partial class NativeMethods
    {
        // https://docs.microsoft.com/en-us/windows/desktop/api/wincred/
        public static class Windows
        {
            private const string Advapi32 = "advapi32.dll";

            // https://docs.microsoft.com/en-gb/windows/desktop/Debug/system-error-codes
            public const int OK = 0;
            public const int ERROR_NO_SUCH_LOGON_SESSION = 0x520;
            public const int ERROR_NOT_FOUND = 0x490;
            public const int ERROR_BAD_USERNAME = 0x89A;
            public const int ERROR_INVALID_FLAGS = 0x3EC;
            public const int ERROR_INVALID_PARAMETER = 0x57;

            public static int GetLastError(bool success)
            {
                if (success)
                {
                    return OK;
                }

                return Marshal.GetLastWin32Error();
            }

            public static void ThrowIfError(int error, string defaultErrorMessage = null)
            {
                switch (error)
                {
                    case OK:
                        return;
                    default:
                        // The Win32Exception constructor will automatically get the human-readable
                        // message for the error code.
                        throw new Exception(defaultErrorMessage, new Win32Exception(error));
                }
            }

            public enum CredentialType
            {
                Generic = 1,
                DomainPassword = 2,
                DomainCertificate = 3,
                DomainVisiblePassword = 4,
            }

            public enum CredentialPersist
            {
                Session = 1,
                LocalMachine = 2,
                Enterprise = 3,
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct Win32Credential
            {
                public int Flags;
                public CredentialType Type;
                [MarshalAs(UnmanagedType.LPWStr)] public string TargetName;
                [MarshalAs(UnmanagedType.LPWStr)] public string Comment;
                public FILETIME LastWritten;
                public int CredentialBlobSize;
                public IntPtr CredentialBlob;
                public CredentialPersist Persist;
                public int AttributeCount;
                public IntPtr CredAttribute;
                [MarshalAs(UnmanagedType.LPWStr)] public string TargetAlias;
                [MarshalAs(UnmanagedType.LPWStr)] public string UserName;
            }

            [DllImport(Advapi32, EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool CredRead(
                string target,
                CredentialType type,
                int reserved,
                out IntPtr credential);

            [DllImport(Advapi32, EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool CredWrite(
                ref Win32Credential credential,
                int flags);

            [DllImport(Advapi32, EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool CredDelete(
                string target,
                CredentialType type,
                int flags);

            [DllImport(Advapi32, EntryPoint = "CredFree", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern void CredFree(
                IntPtr credential);
        }
    }
}
