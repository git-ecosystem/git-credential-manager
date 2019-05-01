// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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
        internal static extern void CredFree(
            IntPtr credential);
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
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Comment;
        public FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CredentialPersist Persist;
        public int AttributeCount;
        public IntPtr CredAttribute;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string UserName;
    }
}
