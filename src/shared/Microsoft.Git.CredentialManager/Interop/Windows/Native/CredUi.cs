using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GitCredentialManager.Interop.Windows.Native
{
    // https://docs.microsoft.com/en-us/windows/desktop/api/wincred/
    public static class CredUi
    {
        private const string LibraryName = "credui.dll";

        [Flags]
        public enum CredentialPackFlags : uint
        {
            None = 0,
            ProtectedCredentials = 0x1,
            WowBuffer = 0x2,
            GenericCredentials = 0x4,
        }

        [Flags]
        public enum CredentialUiFlags : uint
        {
            None = 0,
            IncorrectPassword = 0x1,
            DoNoPersist = 0x2,
            RequestAdministrator = 0x4,
            ExcludeCertificates = 0x8,
            RequireCertificates = 0x10,
            ShowSaveCheckbox = 0x40,
            AlwaysShowUi = 0x80,
            RequireSmartCard = 0x100,
            PasswordOnlyOk = 0x200,
            ValidateUsername = 0x400,
            CompleteUsername = 0x800,
            Persist = 0x1000,
            ServerCredential = 0x4000,
            ExpectConfirmation = 0x20000,
            GenericCredentials = 0x40000,
            UsernameTargetCredentials = 0x80000,
            KeepUsername = 0x100000,
        }

        public enum CredentialUiResult : uint
        {
            Success = 0,
            Cancelled = 1223,
            NoSuchLogonSession = 1312,
            NotFound = 1168,
            InvalidAccountName = 1315,
            InsufficientBuffer = 122,
            InvalidParameter = 87,
            InvalidFlags = 1004,
        }

        [Flags]
        public enum CredentialUiWindowsFlags : uint
        {
            None = 0,

            /// <summary>
            /// The caller is requesting that the credential provider return the user name and password in plain text.
            /// </summary>
            Generic = 0x0001,

            /// <summary>
            /// The Save check box is displayed in the dialog box.
            /// </summary>
            Checkbox = 0x0002,

            /// <summary>
            /// Only credential providers that support the authentication package specified by the `authPackage` parameter
            /// should be enumerated.
            /// </summary>
            AuthPackageOnly = 0x0010,

            /// <summary>
            /// Only the credentials specified by the `inAuthBuffer` parameter for the authentication package specified
            /// by the `authPackage` parameter should be enumerated.
            /// <para/>
            /// If this flag is set, and the `inAuthBuffer` parameter is `null`, the function fails.
            /// </summary>
            InCredOnly = 0x0020,

            /// <summary>
            /// Credential providers should enumerate only administrators.
            /// <para/>
            /// This value is intended for User Account Control (UAC) purposes only.
            /// <para/>
            /// We recommend that external callers not set this flag.
            /// </summary>
            EnumerateAdmins = 0x0100,

            /// <summary>
            /// Only the incoming credentials for the authentication package specified by the `authPackage` parameter
            /// should be enumerated.
            /// </summary>
            EnumerateCurrentUser = 0x0200,

            /// <summary>
            /// The credential dialog box should be displayed on the secure desktop.
            /// <para/>
            /// This value cannot be combined with <see cref="Generic"/>.
            /// </summary>
            SecurePrompt = 0x1000,

            /// <summary>
            /// The credential dialog box is invoked by the SspiPromptForCredentials function, and the client is prompted
            /// before a prior handshake.
            /// <para/>
            /// If SSPIPFC_NO_CHECKBOX is passed in the `inAuthBuffer` parameter, then the credential provider should
            /// not display the check box.
            /// </summary>
            Preprompting = 0x2000,

            /// <summary>
            /// The credential provider should align the credential BLOB pointed to by the `outAuthBuffer` parameter to
            /// a 32-bit boundary, even if the provider is running on a 64-bit system.
            /// </summary>
            Pack32Wow = 0x10000000,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CredentialUiInfo
        {
            [MarshalAs(UnmanagedType.U4)]
            public int Size;

            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr Parent;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string MessageText;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string CaptionText;

            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr BannerArt;
        }

        [DllImport(LibraryName, EntryPoint = "CredUIPromptForWindowsCredentialsW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int CredUIPromptForWindowsCredentials(
            ref CredentialUiInfo credInfo,
            uint authError,
            ref uint authPackage,
            IntPtr inAuthBuffer,
            uint inAuthBufferSize,
            out IntPtr outAuthBuffer,
            out uint outAuthBufferSize,
            ref bool saveCredentials,
            CredentialUiWindowsFlags flags);

        [DllImport(LibraryName, EntryPoint = "CredPackAuthenticationBufferW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredPackAuthenticationBuffer(
            CredentialPackFlags flags,
            string username,
            string password,
            IntPtr packedCredentials,
            ref int packedCredentialsSize);

        [DllImport(LibraryName, EntryPoint = "CredUnPackAuthenticationBufferW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredUnPackAuthenticationBuffer(
            CredentialPackFlags flags,
            IntPtr authBuffer,
            uint authBufferSize,
            StringBuilder username,
            ref int maxUsernameLen,
            StringBuilder domainName,
            ref int maxDomainNameLen,
            StringBuilder password,
            ref int maxPasswordLen);
    }
}
