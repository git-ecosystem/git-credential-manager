using System;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Microsoft.Git.CredentialManager.Interop.Windows.Native
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

    [Flags]
    public enum CredentialEnumerateFlags
    {
        None = 0,
        AllCredentials = 0x1
    }

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
    }
}
