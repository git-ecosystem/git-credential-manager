// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Git.CredentialManager.Interop.Windows.Native;

namespace Microsoft.Git.CredentialManager.Interop.Windows
{
    public class WindowsCredentialManager : ICredentialStore
    {
        #region Constructors

        /// <summary>
        /// Open the Windows Credential Manager vault for the current user.
        /// </summary>
        /// <returns>Current user's Credential Manager vault.</returns>
        public static WindowsCredentialManager Open()
        {
            return new WindowsCredentialManager();
        }

        private WindowsCredentialManager()
        {
            PlatformUtils.EnsureWindows();
        }

        #endregion

        #region ICredentialStore

        public ICredential Get(string key)
        {
            IntPtr credPtr = IntPtr.Zero;

            try
            {
                int result = Common.GetLastError(
                    Advapi32.CredRead(key, CredentialType.Generic, 0, out credPtr)
                );

                switch (result)
                {
                    case Common.OK:
                        Win32Credential credential = Marshal.PtrToStructure<Win32Credential>(credPtr);

                        var userName = credential.UserName;

                        byte[] passwordBytes = InteropUtils.ToByteArray(credential.CredentialBlob, credential.CredentialBlobSize);
                        var password = Encoding.Unicode.GetString(passwordBytes);

                        return new GitCredential(userName, password);

                    case Common.ERROR_NOT_FOUND:
                        return null;

                    default:
                        Common.ThrowIfError(result, "Failed to read item from store.");
                        return null;
                }
            }
            finally
            {
                if (credPtr != IntPtr.Zero)
                {
                    Advapi32.CredFree(credPtr);
                }
            }
        }

        public void AddOrUpdate(string key, ICredential credential)
        {
            byte[] passwordBytes = Encoding.Unicode.GetBytes(credential.Password);

            var w32Credential = new Win32Credential
            {
                Type = CredentialType.Generic,
                TargetName = key,
                CredentialBlob = Marshal.AllocHGlobal(passwordBytes.Length),
                CredentialBlobSize = passwordBytes.Length,
                Persist = CredentialPersist.LocalMachine,
                AttributeCount = 0,
                UserName = credential.UserName,
            };

            try
            {
                Marshal.Copy(passwordBytes, 0, w32Credential.CredentialBlob, passwordBytes.Length);

                int result = Common.GetLastError(
                    Advapi32.CredWrite(ref w32Credential, 0)
                );

                Common.ThrowIfError(result, "Failed to write item to store.");
            }
            finally
            {
                if (w32Credential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(w32Credential.CredentialBlob);
                }
            }
        }

        public bool Remove(string key)
        {
            int result = Common.GetLastError(
                Advapi32.CredDelete(key, CredentialType.Generic, 0)
            );

            switch (result)
            {
                case Common.OK:
                    return true;

                case Common.ERROR_NOT_FOUND:
                    return false;

                default:
                    Common.ThrowIfError(result);
                    return false;
            }
        }

        #endregion
    }
}
