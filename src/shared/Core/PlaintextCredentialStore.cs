using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace GitCredentialManager
{
    public class PlaintextCredentialStore : ICredentialStore
    {
        public PlaintextCredentialStore(IFileSystem fileSystem, string storeRoot, string @namespace = null)
        {
            EnsureArgument.NotNull(fileSystem, nameof(fileSystem));
            EnsureArgument.NotNullOrWhiteSpace(storeRoot, nameof(storeRoot));

            FileSystem = fileSystem;
            StoreRoot = storeRoot;
            Namespace = @namespace;
        }

        protected IFileSystem FileSystem { get; }
        protected string StoreRoot { get; }
        protected string Namespace { get; }
        protected virtual string CredentialFileExtension => ".credential";

        public IList<string> GetAccounts(string service)
        {
            return Enumerate(service, null).Select(x => x.Account).Distinct().ToList();
        }

        public ICredential Get(string service, string account)
        {
            return Enumerate(service, account).FirstOrDefault();
        }

        public void AddOrUpdate(string service, string account, string secret)
        {
            // Ensure the store root exists and permissions are set
            EnsureStoreRoot();

            FileCredential existingCredential = Enumerate(service, account).FirstOrDefault();

            // No need to update existing credential if nothing has changed
            if (existingCredential != null &&
                StringComparer.Ordinal.Equals(account, existingCredential.Account) &&
                StringComparer.Ordinal.Equals(secret, existingCredential.Password))
            {
                return;
            }

            string serviceSlug = CreateServiceSlug(service);
            string servicePath = Path.Combine(StoreRoot, serviceSlug);

            if (!FileSystem.DirectoryExists(servicePath))
            {
                FileSystem.CreateDirectory(servicePath);
            }

            string fullPath = Path.Combine(servicePath, $"{account}{CredentialFileExtension}");
            var credential = new FileCredential(fullPath, service, account, secret);
            SerializeCredential(credential);
        }

        public bool Remove(string service, string account)
        {
            foreach (FileCredential credential in Enumerate(service, account))
            {
                // Only delete the first match
                FileSystem.DeleteFile(credential.FullPath);
                return true;
            }

            return false;
        }

        protected virtual bool TryDeserializeCredential(string path, out FileCredential credential)
        {
            string text;
            using (var stream = FileSystem.OpenFileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream))
            {
                text = reader.ReadToEnd();
            }

            int line1Idx = text.IndexOf(Environment.NewLine, StringComparison.OrdinalIgnoreCase);
            if (line1Idx > 0)
            {
                // Password is the first line
                // Decrypt password from base64
                string protectedPasswordBase64 = text.Substring(0, line1Idx);
                try
                {
                    byte[] protectedPasswordBytes = Convert.FromBase64String(protectedPasswordBase64);
                    byte[] passwordBytes = ProtectedData.Unprotect(protectedPasswordBytes, null, DataProtectionScope.CurrentUser);
                    string password = Encoding.UTF8.GetString(passwordBytes);

                    // All subsequent lines are metadata/attributes
                    string attrText = text.Substring(line1Idx + Environment.NewLine.Length);
                    using var attrReader = new StringReader(attrText);
                    IDictionary<string, string> attrs = attrReader.ReadDictionary(StringComparer.OrdinalIgnoreCase);

                    // Account is optional
                    attrs.TryGetValue("account", out string account);

                    // Service is required
                    if (attrs.TryGetValue("service", out string service))
                    {
                        credential = new FileCredential(path, service, account, password);
                        return true;
                    }
                }
                catch
                {
                    // If decryption fails, treat as not found/corrupt
                }

                // All subsequent lines are metadata/attributes
                string attrText = text.Substring(line1Idx + Environment.NewLine.Length);
                using var attrReader = new StringReader(attrText);
                IDictionary<string, string> attrs = attrReader.ReadDictionary(StringComparer.OrdinalIgnoreCase);

                // Account is optional
                attrs.TryGetValue("account", out string account);

                // Service is required
                if (attrs.TryGetValue("service", out string service))
                {
                    credential = new FileCredential(path, service, account, password);
                    return true;
                }
            }

            credential = null;
            return false;
        }

        protected virtual void SerializeCredential(FileCredential credential)
        {
            // Ensure the parent directory exists
            string parentDir = Path.GetDirectoryName(credential.FullPath);
            if (!FileSystem.DirectoryExists(parentDir))
            {
                FileSystem.CreateDirectory(parentDir);
            }

            using (var stream = FileSystem.OpenFileStream(credential.FullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                // Encrypt password and encode as base64
                var protectedPasswordBytes = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(credential.Password),
                    null,
                    DataProtectionScope.CurrentUser); // Choose CurrentUser or LocalMachine as appropriate
                var protectedPasswordBase64 = Convert.ToBase64String(protectedPasswordBytes);
                writer.WriteLine(protectedPasswordBase64);
                writer.WriteLine("service={0}", credential.Service);
                writer.WriteLine("account={0}", credential.Account);
                writer.Flush();
            }
        }

        private IEnumerable<FileCredential> Enumerate(string service, string account)
        {
            string serviceSlug = CreateServiceSlug(service);
            string searchPath = Path.Combine(StoreRoot, serviceSlug);
            bool anyAccount = string.IsNullOrWhiteSpace(account);

            if (!FileSystem.DirectoryExists(searchPath))
            {
                yield break;
            }

            IEnumerable<string> allFiles = FileSystem.EnumerateFiles(searchPath, $"*{CredentialFileExtension}");

            foreach (string fullPath in allFiles)
            {
                string accountFile = Path.GetFileNameWithoutExtension(fullPath);
                if (anyAccount || StringComparer.OrdinalIgnoreCase.Equals(account, accountFile))
                {
                    // Validate the credential metadata also matches our search
                    if (TryDeserializeCredential(fullPath, out FileCredential credential) &&
                        StringComparer.OrdinalIgnoreCase.Equals(service, credential.Service) &&
                        (anyAccount || StringComparer.OrdinalIgnoreCase.Equals(account, credential.Account)))
                    {
                        yield return credential;
                    }
                }
            }
        }

        /// <summary>
        /// Ensure the store root directory exists. If it does not, create a new directory with
        /// permissions that only permit the owner to read/write/execute. Permissions on an existing
        /// directory are not modified.
        /// </summary>
        private void EnsureStoreRoot()
        {
            if (FileSystem.DirectoryExists(StoreRoot))
            {
                // Don't touch the permissions on the existing directory
                return;
            }

            FileSystem.CreateDirectory(StoreRoot);

            // We only set file system permissions on POSIX platforms
            if (!PlatformUtils.IsPosix())
            {
                return;
            }

            // Set store root permissions such that only the owner can read/write/execute
            var mode = Interop.Posix.Native.NativeFileMode.S_IRUSR |
                       Interop.Posix.Native.NativeFileMode.S_IWUSR |
                       Interop.Posix.Native.NativeFileMode.S_IXUSR;

            // Ignore the return code.. this is a best effort only
            Interop.Posix.Native.Stat.chmod(StoreRoot, mode);
        }

        private string CreateServiceSlug(string service)
        {
            var sb = new StringBuilder();
            char sep = Path.DirectorySeparatorChar;

            if (!string.IsNullOrWhiteSpace(Namespace))
            {
                sb.AppendFormat("{0}{1}", Namespace, sep);
            }

            if (Uri.TryCreate(service, UriKind.Absolute, out Uri serviceUri))
            {
                sb.AppendFormat("{0}{1}", serviceUri.Scheme, sep);
                sb.AppendFormat("{0}", serviceUri.Host);

                if (!serviceUri.IsDefaultPort)
                {
                    sb.Append(PlatformUtils.IsWindows() ? '-' : ':');
                    sb.Append(serviceUri.Port);
                }

                sb.Append(serviceUri.AbsolutePath.Replace('/', sep));
            }
            else
            {
                sb.Append(service);
            }

            return sb.ToString();
        }
    }
}
