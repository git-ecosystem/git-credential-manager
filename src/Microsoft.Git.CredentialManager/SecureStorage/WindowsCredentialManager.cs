using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static Microsoft.Git.CredentialManager.SecureStorage.NativeMethods.Windows;

namespace Microsoft.Git.CredentialManager.SecureStorage
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
                ThrowOnError(
                    CredRead(key, CredentialType.Generic, 0, out credPtr),
                    "Failed to read item from store."
                );

                Win32Credential credential = Marshal.PtrToStructure<Win32Credential>(credPtr);

                var userName = credential.UserName;

                byte[] passwordBytes = NativeMethods.ToByteArray(credential.CredentialBlob, credential.CredentialBlobSize);
                var password = Encoding.Unicode.GetString(passwordBytes);

                return new Credential(userName, password);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            finally
            {
                if (credPtr != IntPtr.Zero)
                {
                    CredFree(credPtr);
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
                CredentialBlob = Marshal.AllocCoTaskMem(passwordBytes.Length),
                CredentialBlobSize = passwordBytes.Length,
                Persist = CredentialPersist.LocalMachine,
                AttributeCount = 0,
                UserName = credential.UserName,
            };

            try
            {
                Marshal.Copy(passwordBytes, 0, w32Credential.CredentialBlob, passwordBytes.Length);

                ThrowOnError(
                    CredWrite(ref w32Credential, 0),
                    "Failed to write item to store."
                );
            }
            finally
            {
                if (w32Credential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(w32Credential.CredentialBlob);
                }
            }
        }

        public bool Remove(string key)
        {
            try
            {
                ThrowOnError(CredDelete(key, CredentialType.Generic, 0));
                return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        #endregion
    }
}
