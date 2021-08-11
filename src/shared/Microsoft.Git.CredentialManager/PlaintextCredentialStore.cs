// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Git.CredentialManager
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

        #region ICredentialStore

        public ICredential Get(string service, string account)
        {
            string serviceSlug = CreateServiceSlug(service);
            string searchPath = Path.Combine(StoreRoot, serviceSlug);
            bool anyAccount = string.IsNullOrWhiteSpace(account);

            if (!FileSystem.DirectoryExists(searchPath))
            {
                return null;
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
                        return credential;
                    }
                }
            }

            return null;
        }

        public void AddOrUpdate(string service, string account, string secret)
        {
            // Ensure the store root exists and permissions are set
            EnsureStoreRoot();

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
            string serviceSlug = CreateServiceSlug(service);
            string searchPath = Path.Combine(StoreRoot, serviceSlug);
            bool anyAccount = string.IsNullOrWhiteSpace(account);

            if (!FileSystem.DirectoryExists(searchPath))
            {
                return false;
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
                        // Delete the credential file
                        FileSystem.DeleteFile(fullPath);
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

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
                string password = text.Substring(0, line1Idx);

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
                writer.WriteLine(credential.Password);
                writer.WriteLine("service={0}", credential.Service);
                writer.WriteLine("account={0}", credential.Account);
                writer.Flush();
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
