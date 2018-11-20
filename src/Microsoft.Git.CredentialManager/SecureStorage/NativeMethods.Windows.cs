using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Microsoft.Git.CredentialManager.SecureStorage
{
    internal static partial class NativeMethods
    {
        // https://docs.microsoft.com/en-us/windows/desktop/api/wincred/
        public static class Windows
        {
            private const string Advapi32 = "advapi32.dll";

            private const int ERROR_NO_SUCH_LOGON_SESSION = 0;
            private const int ERROR_NOT_FOUND = 0x490;

            public static void ThrowOnError(bool success, string defaultErrorMessage = null)
            {
                int error = Marshal.GetLastWin32Error();
                if (!success)
                {
                    switch (error)
                    {
                        case ERROR_NO_SUCH_LOGON_SESSION:
                            throw new InvalidOperationException(
                                "The logon session does not exist or there is no credential set associated with this logon session.",
                                new Win32Exception(error)
                            );
                        case ERROR_NOT_FOUND:
                            throw new KeyNotFoundException("The item cannot be found.", new Win32Exception(error));
                        default:
                            throw new Win32Exception(error, defaultErrorMessage);
                    }
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
