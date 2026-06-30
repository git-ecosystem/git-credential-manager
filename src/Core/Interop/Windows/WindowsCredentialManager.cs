using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GitCredentialManager.Interop.Windows.Native;

namespace GitCredentialManager.Interop.Windows
{
    public class WindowsCredentialManager : ICredentialStore
    {
        internal const string TargetNameLegacyGenericPrefix = "LegacyGeneric:target=";

        private readonly string _namespace;

        /// <summary>
        /// Open the Windows Credential Manager vault for the current user.
        /// </summary>
        /// <param name="namespace">Optional namespace to scope credential operations.</param>
        /// <returns>Current user's Credential Manager vault.</returns>
        public WindowsCredentialManager(string @namespace = null)
        {
            PlatformUtils.EnsureWindows();
            _namespace = @namespace;
        }

        public IList<string> GetAccounts(string service)
        {
            return Enumerate(service, null).Select(x => x.UserName).Distinct().ToList();
        }

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
                    else
                    {
                        // No need to write out credential if the account and secret/password are the same
                        string existingSecret = existingCred.GetCredentialBlobAsString();
                        if (StringComparer.Ordinal.Equals(existingSecret, secret))
                        {
                            return;
                        }
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

        /// <summary>
        /// Check if we can persist credentials to for the current process and logon session.
        /// </summary>
        /// <returns>True if persistence is possible, false otherwise.</returns>
        public static bool CanPersist()
        {
            uint count = Advapi32.CRED_TYPE_MAXIMUM;
            var arr = new CredentialPersist[count];

            int result = Win32Error.GetLastError(
                Advapi32.CredGetSessionTypes(count, arr)
            );

            CredentialPersist persist = CredentialPersist.None;
            if (result == Win32Error.Success)
            {
                persist = arr[(int)CredentialType.Generic];
            }

            // If the maximum allowed is anything less than "local machine" then cannot persist credentials.
            return persist >= CredentialPersist.LocalMachine;
        }

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
            string password = credential.GetCredentialBlobAsString();

            // Recover the target name we gave from the internal (raw) target name
            string targetName = credential.TargetName.TrimUntilIndexOf(TargetNameLegacyGenericPrefix);

            // Recover the service name from the target name
            string serviceName = targetName;
            if (!string.IsNullOrWhiteSpace(_namespace))
            {
                serviceName = serviceName.TrimUntilIndexOf($"{_namespace}:");
            }

            // Strip any userinfo component from the service name
            serviceName = RemoveUriUserInfo(serviceName);

            return new WindowsCredential(serviceName, credential.UserName, password, targetName);
        }

        public /* for testing */ static string RemoveUriUserInfo(string url)
        {
            // To remove the userinfo component we must search for the end of the :// scheme
            // delimiter, and the start of the @ userinfo delimiter. We don't want to match
            // any other '@' character however (such as one in the URI path).
            // To ensure this we only consider an '@' character that exists before the first
            // '/' character after the scheme delimiter - that is to say the authority-path
            // separator.
            //
            //                authority
            //              |-----------|
            //     scheme://userinfo@host/path
            //
            int schemeDelimIdx = url.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
            if (schemeDelimIdx > 0)
            {
                int authorityIdx = schemeDelimIdx + Uri.SchemeDelimiter.Length;
                int slashIdx = url.IndexOf("/", authorityIdx, StringComparison.Ordinal);
                int atIdx = url.IndexOf("@", StringComparison.Ordinal);

                // No path component or trailing slash; use end of string
                if (slashIdx < 0)
                {
                    slashIdx = url.Length - 1;
                }

                // Only if the '@' is before the first slash is this the userinfo delimiter
                if (0 < atIdx && atIdx < slashIdx)
                {
                    return url.Substring(0, authorityIdx) + url.Substring(atIdx + 1);
                }
            }

            return url;
        }

        internal /* for testing */ bool IsMatch(string service, string account, Win32Credential credential)
        {
            // Match against the username first
            if (!string.IsNullOrWhiteSpace(account) &&
                !StringComparer.Ordinal.Equals(account, credential.UserName))
            {
                return false;
            }

            // Trim the "LegacyGeneric" prefix Windows adds
            string targetName = credential.TargetName.TrimUntilIndexOf(TargetNameLegacyGenericPrefix);
            
            // Only match credentials with the namespace we have been configured with (if any)
            if (!string.IsNullOrWhiteSpace(_namespace))
            {
                string nsPrefix = $"{_namespace}:";
                if (!targetName.StartsWith(nsPrefix, StringComparison.Ordinal))
                {
                    return false;
                }

                targetName = targetName.Substring(nsPrefix.Length);
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
                // Match scheme/protocol
                if (!StringComparer.OrdinalIgnoreCase.Equals(serviceUri.Scheme, targetUri.Scheme))
                {
                    return false;
                }

                // Match host name
                if (!StringComparer.OrdinalIgnoreCase.Equals(serviceUri.Host, targetUri.Host))
                {
                    return false;
                }

                // Match port number
                if (serviceUri.Port != targetUri.Port)
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

        internal /* for testing */ string CreateTargetName(string service, string account)
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

            if (!serviceUri.IsDefaultPort)
            {
                sb.AppendFormat(":{0}", serviceUri.Port);
            }

            string trimmedPath = serviceUri.AbsolutePath.TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(trimmedPath))
            {
                sb.Append(trimmedPath);
            }

            return sb.ToString();
        }
    }
}
