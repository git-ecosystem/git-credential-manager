using System;
using System.Runtime.InteropServices;
using System.Text;
using GitCredentialManager.Interop.Windows.Native;

namespace GitCredentialManager.Interop.Windows
{
    public class WindowsSystemPrompts : ISystemPrompts
    {
        private IntPtr _parentHwd = IntPtr.Zero;

        public object ParentWindowId
        {
            get => _parentHwd.ToString();
            set => _parentHwd = ConvertUtils.TryToInt32(value, out int ptr) ? new IntPtr(ptr) : IntPtr.Zero;
        }

        public bool ShowCredentialPrompt(string resource, string userName, out ICredential credential)
        {
            EnsureArgument.NotNullOrWhiteSpace(resource, nameof(resource));

            string message = $"Enter credentials for '{resource}'";

            var credUiInfo = new CredUi.CredentialUiInfo
            {
                BannerArt = IntPtr.Zero,
                CaptionText = "Git Credential Manager", // TODO: make this a parameter?
                Parent = _parentHwd,
                MessageText = message,
                Size = Marshal.SizeOf(typeof(CredUi.CredentialUiInfo))
            };

            var packFlags = CredUi.CredentialPackFlags.None;
            var uiFlags = CredUi.CredentialUiWindowsFlags.Generic;
            if (!string.IsNullOrEmpty(userName))
            {
                // If we are given a username, pre-populate the dialog with the given value
                uiFlags |= CredUi.CredentialUiWindowsFlags.InCredOnly;
            }

            IntPtr inBufferPtr = IntPtr.Zero;
            uint inBufferSize;

            try
            {
                CreateCredentialInfoBuffer(userName, packFlags, out inBufferSize, out inBufferPtr);

                return DisplayCredentialPrompt(ref credUiInfo, ref packFlags, inBufferPtr, inBufferSize, false, uiFlags, out credential);
            }
            finally
            {
                if (inBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inBufferPtr);
                }
            }
        }

        private static void CreateCredentialInfoBuffer(string userName, CredUi.CredentialPackFlags flags, out uint inBufferSize, out IntPtr inBufferPtr)
        {
            // Windows Credential API calls require at least an empty string; not null
            userName = userName ?? string.Empty;

            int desiredBufSize = 0;

            // Execute with a null packed credentials pointer to determine the required buffer size.
            // This method always returns false when determining the buffer size so we only fail if the size is not strictly positive.
            CredUi.CredPackAuthenticationBuffer(flags, userName, string.Empty, IntPtr.Zero, ref desiredBufSize);
            Win32Error.ThrowIfError(desiredBufSize > 0, "Unable to determine credential buffer size.");

            // Create a buffer of the desired size and pass the pointer and size back to the caller
            inBufferSize = (uint) desiredBufSize;
            inBufferPtr = Marshal.AllocHGlobal(desiredBufSize);

            Win32Error.ThrowIfError(
                CredUi.CredPackAuthenticationBuffer(flags, userName, string.Empty, inBufferPtr, ref desiredBufSize),
                "Unable to write to credential buffer."
            );
        }

        private static bool DisplayCredentialPrompt(
            ref CredUi.CredentialUiInfo credUiInfo,
            ref CredUi.CredentialPackFlags packFlags,
            IntPtr inBufferPtr,
            uint inBufferSize,
            bool saveCredentials,
            CredUi.CredentialUiWindowsFlags uiFlags,
            out ICredential credential)
        {
            uint authPackage = 0;
            IntPtr outBufferPtr = IntPtr.Zero;
            uint outBufferSize;

            try
            {
                // Open a standard Windows authentication dialog to acquire username and password credentials
                int error = CredUi.CredUIPromptForWindowsCredentials(
                    ref credUiInfo,
                    0,
                    ref authPackage,
                    inBufferPtr,
                    inBufferSize,
                    out outBufferPtr,
                    out outBufferSize,
                    ref saveCredentials,
                    uiFlags);

                switch (error)
                {
                    case Win32Error.Cancelled:
                        credential = null;
                        return false;
                    default:
                        Win32Error.ThrowIfError(error, "Failed to show credential prompt.");
                        break;
                }

                int maxUserLength = 512;
                int maxPassLength = 512;
                int maxDomainLength = 256;
                var usernameBuffer = new StringBuilder(maxUserLength);
                var domainBuffer = new StringBuilder(maxDomainLength);
                var passwordBuffer = new StringBuilder(maxPassLength);

                // Unpack the result
                Win32Error.ThrowIfError(
                    CredUi.CredUnPackAuthenticationBuffer(
                        packFlags,
                        outBufferPtr,
                        outBufferSize,
                        usernameBuffer,
                        ref maxUserLength,
                        domainBuffer,
                        ref maxDomainLength,
                        passwordBuffer,
                        ref maxPassLength),
                    "Failed to unpack credential buffer."
                );

                // Return the plaintext credential strings to the caller
                string userName = usernameBuffer.ToString();
                string password = passwordBuffer.ToString();

                credential = new GitCredential(userName, password);
                return true;
            }
            finally
            {
                if (outBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outBufferPtr);
                }
            }
        }
    }
}
