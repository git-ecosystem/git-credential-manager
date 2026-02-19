using System;
using System.Runtime.InteropServices;
using System.Text;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace GitCredentialManager.Interop.Windows.Native
{
    // https://docs.microsoft.com/en-us/windows/desktop/api/wincred/
    public static class Advapi32
    {
        private const string LibraryName = "advapi32.dll";

        [DllImport(LibraryName, EntryPoint = "CredReadW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead(
            string target,
            CredentialType type,
            int reserved,
            out IntPtr credential);

        [DllImport(LibraryName, EntryPoint = "CredWriteW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredWrite(
            ref Win32Credential credential,
            int flags);

        [DllImport(LibraryName, EntryPoint = "CredDeleteW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredDelete(
            string target,
            CredentialType type,
            int flags);

        [DllImport(LibraryName, EntryPoint = "CredFree", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern void CredFree(
            IntPtr credential);

        [DllImport(LibraryName, EntryPoint = "CredEnumerateW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredEnumerate(
            string filter,
            CredentialEnumerateFlags flags,
            out int count,
            out IntPtr credentialsList);

        [DllImport(LibraryName, EntryPoint = "CredGetSessionTypes", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredGetSessionTypes(
            uint maximumPersistCount,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] CredentialPersist[] maximumPersist);

        // Values from wincred.h
        public const uint CRED_TYPE_MAXIMUM = 7;
        public const uint CRED_TYPE_MAXIMUM_EX = CRED_TYPE_MAXIMUM + 1000;
    }

    // Enum values from wincred.h
    public enum CredentialType
    {
        Generic = 1,
        DomainPassword = 2,
        DomainCertificate = 3,
        DomainVisiblePassword = 4,
        GenericCertificate = 5,
        DomainExtended = 6
    }

    public enum CredentialPersist
    {
        None = 0,
        Session = 1,
        LocalMachine = 2,
        Enterprise = 3,
    }

    [Flags]
    public enum CredentialEnumerateFlags
    {
        None = 0,
        AllCredentials = 0x1
    }

    /// <see cref="https://learn.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentialw"/>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Win32Credential
    {
        public int Flags;
        public CredentialType Type;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Comment;
        public FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CredentialPersist Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string UserName;

        public string GetCredentialBlobAsString()
        {
            if (CredentialBlobSize != 0 && CredentialBlob != IntPtr.Zero)
            {
                byte[] passwordBytes = InteropUtils.ToByteArray(CredentialBlob, CredentialBlobSize);
                return Encoding.Unicode.GetString(passwordBytes);
            }

            return null;
        }
    }

    /// <see cref="https://learn.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credential_attributew"/>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Win32CredentialAttribute
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Keyword;
        public int Flags;
        public int ValueSize;
        public IntPtr Value;

        public long? GetValueAsLong()
        {
            if (ValueSize != 0 && Value != IntPtr.Zero)
            {
                byte[] bytes = InteropUtils.ToByteArray(Value, ValueSize);
                return BitConverter.ToInt64(bytes, 0);
            }

            return null;
        }
    }
}
