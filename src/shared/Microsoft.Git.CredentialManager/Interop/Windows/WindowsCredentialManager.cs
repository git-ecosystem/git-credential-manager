// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Git.CredentialManager.Interop.Windows.Native;

namespace Microsoft.Git.CredentialManager.Interop.Windows
{
    public class WindowsCredentialManager : ICredentialStore
    {
        private const string TargetNameLegacyGenericPrefix = "LegacyGeneric:target=";

        private readonly string _namespace;

        #region Constructors

        /// <summary>
        /// Open the Windows Credential Manager vault for the current user.
        /// </summary>
        /// <param name="namespace">Optional namespace to scope credential operations.</param>
        /// <returns>Current user's Credential Manager vault.</returns>
        public static WindowsCredentialManager Open(string @namespace = null)
        {
            return new WindowsCredentialManager(@namespace);
        }

        private WindowsCredentialManager(string @namespace)
        {
            PlatformUtils.EnsureWindows();
            _namespace = @namespace;
        }

        #endregion

        #region ICredentialStore

        public ICredential Get(string service, string account)
        {
            return Enumerate(service, account).FirstOrDefault();
        }

        public void AddOrUpdate(string service, string account, string secret)
        {
            EnsureArgument.NotNullOrWhiteSpace(service, nameof(service));

            IntPtr existingCredPtr = IntPtr.Zero;
            IntPtr credBlob = IntPtr.Zero;

            try
            {
                // Determine if we need to update an existing credential, which might have
                // a target name that does not include the account name.
                //
                // We first check for the presence of a credential with an account-less
                // target name.
                //
                //  - If such credential exists and *has the same account* then we will
                //    update that entry.
                //  - If such credential exists and does *not* have the same account then
                //    we must create a new entry with the account in the target name.
                //  - If no such credential exists then we create a new entry with the
                //    account-less target name.
                //
                string targetName = CreateTargetName(service, account: null);
                if (Advapi32.CredRead(targetName, CredentialType.Generic, 0, out existingCredPtr))
                {
                    var existingCred = Marshal.PtrToStructure<Win32Credential>(existingCredPtr);
                    if (!StringComparer.Ordinal.Equals(existingCred.UserName, account))
                    {
                        // Create new entry with the account in the target name
                        targetName = CreateTargetName(service, account);
                    }
                }

                byte[] secretBytes = Encoding.Unicode.GetBytes(secret);
                credBlob = Marshal.AllocHGlobal(secretBytes.Length);
                Marshal.Copy(secretBytes, 0, credBlob, secretBytes.Length);

                var newCred = new Win32Credential
                {
                    Type = CredentialType.Generic,
                    TargetName = targetName,
                    CredentialBlobSize = secretBytes.Length,
                    CredentialBlob = credBlob,
                    Persist = CredentialPersist.LocalMachine,
                    UserName = account,
                };


                int result = Win32Error.GetLastError(
                    Advapi32.CredWrite(ref newCred, 0)
                );

                Win32Error.ThrowIfError(result, "Failed to write item to store.");
            }
            finally
            {
                if (credBlob != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(credBlob);
                }

                if (existingCredPtr != IntPtr.Zero)
                {
                    Advapi32.CredFree(existingCredPtr);
                }
            }
        }

        public bool Remove(string service, string account)
        {
            WindowsCredential credential = Enumerate(service, account).FirstOrDefault();

            if (credential != null)
            {
                int result = Win32Error.GetLastError(
                    Advapi32.CredDelete(credential.TargetName, CredentialType.Generic, 0)
                );

                switch (result)
                {
                    case Win32Error.Success:
                        return true;

                    case Win32Error.NotFound:
                        return false;

                    default:
                        Win32Error.ThrowIfError(result);
                        return false;
                }
            }

            return false;
        }

        #endregion

        private IEnumerable<WindowsCredential> Enumerate(string service, string account)
        {
            IntPtr credList = IntPtr.Zero;

            try
            {
                int result = Win32Error.GetLastError(
                    Advapi32.CredEnumerate(
                        null,
                        CredentialEnumerateFlags.AllCredentials,
                        out int count,
                        out credList)
                );

                switch (result)
                {
                    case Win32Error.Success:
                        int ptrSize = Marshal.SizeOf<IntPtr>();
                        for (int i = 0; i < count; i++)
                        {
                            IntPtr credPtr = Marshal.ReadIntPtr(credList, i * ptrSize);
                            Win32Credential credential = Marshal.PtrToStructure<Win32Credential>(credPtr);

                            if (!IsMatch(service, account, credential))
                            {
                                continue;
                            }

                            yield return CreateCredentialFromStructure(credential);
                        }
                        break;

                    case Win32Error.NotFound:
                        yield break;

                    default:
                        Win32Error.ThrowIfError(result, "Failed to enumerate credentials.");
                        yield break;
                }
            }
            finally
            {
                if (credList != IntPtr.Zero)
                {
                    Advapi32.CredFree(credList);
                }
            }
        }

        private WindowsCredential CreateCredentialFromStructure(Win32Credential credential)
        {
            string password = null;
            if (credential.CredentialBlobSize != 0 && credential.CredentialBlob != IntPtr.Zero)
            {
                byte[] passwordBytes = InteropUtils.ToByteArray(
                    credential.CredentialBlob,
                    credential.CredentialBlobSize);
                password = Encoding.Unicode.GetString(passwordBytes);
            }

            // Recover the service name from the target name
            string service = credential.TargetName.TrimUntilIndexOf(TargetNameLegacyGenericPrefix);
            if (!string.IsNullOrWhiteSpace(_namespace))
            {
                service = service.TrimUntilIndexOf($"{_namespace}:");
            }

            return new WindowsCredential(service, credential.UserName, password, credential.TargetName);
        }

        private bool IsMatch(string service, string account, Win32Credential credential)
        {
            // Match against the username first
            if (!string.IsNullOrWhiteSpace(account) &&
                !StringComparer.Ordinal.Equals(account, credential.UserName))
            {
                return false;
            }

            // Trim the "LegacyGeneric" prefix Windows adds and any namespace we have been filtered with
            string targetName = credential.TargetName.TrimUntilIndexOf(TargetNameLegacyGenericPrefix);
            if (!string.IsNullOrWhiteSpace(_namespace))
            {
                targetName = targetName.TrimUntilIndexOf($"{_namespace}:");
            }

            // If the target name matches the service name exactly then return 'match'
            if (StringComparer.Ordinal.Equals(service, targetName))
            {
                return true;
            }

            // Try matching the target and service as URIs
            if (Uri.TryCreate(service, UriKind.Absolute, out Uri serviceUri) &&
                Uri.TryCreate(targetName, UriKind.Absolute, out Uri targetUri))
            {
                // Match host name
                if (!StringComparer.OrdinalIgnoreCase.Equals(serviceUri.Host, targetUri.Host))
                {
                    return false;
                }

                // Match port number
                if (!serviceUri.IsDefaultPort && serviceUri.Port == targetUri.Port)
                {
                    return false;
                }

                // Match path
                if (!string.IsNullOrWhiteSpace(serviceUri.AbsolutePath) &&
                    !StringComparer.OrdinalIgnoreCase.Equals(serviceUri.AbsolutePath, targetUri.AbsolutePath))
                {
                    return false;
                }

                // URLs match
                return true;
            }

            // Unable to match
            return false;
        }

        private string CreateTargetName(string service, string account)
        {
            var serviceUri = new Uri(service, UriKind.Absolute);
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(_namespace))
            {
                sb.AppendFormat("{0}:", _namespace);
            }

            if (!string.IsNullOrWhiteSpace(serviceUri.Scheme))
            {
                sb.AppendFormat("{0}://", serviceUri.Scheme);
            }

            if (!string.IsNullOrWhiteSpace(account))
            {
                string escapedAccount = account.Replace('@', '_');
                sb.AppendFormat("{0}@", escapedAccount);
            }

            if (!string.IsNullOrWhiteSpace(serviceUri.Host))
            {
                sb.Append(serviceUri.Host);
            }

            if (!string.IsNullOrWhiteSpace(serviceUri.AbsolutePath.TrimEnd('/')))
            {
                sb.Append(serviceUri.AbsolutePath);
            }

            return sb.ToString();
        }
    }
}
