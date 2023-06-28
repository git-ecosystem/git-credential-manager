using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitCredentialManager.Interop.Posix
{
    public class GpgPassCredentialStore : PlaintextCredentialStore
    {
        public const string PasswordStoreDirEnvar = "PASSWORD_STORE_DIR";

        private readonly IGpg _gpg;

        public GpgPassCredentialStore(IFileSystem fileSystem, IGpg gpg, string storeRoot, string @namespace = null)
            : base(fileSystem, storeRoot, @namespace)
        {
            PlatformUtils.EnsurePosix();
            EnsureArgument.NotNull(gpg, nameof(gpg));
            _gpg = gpg;
        }

        protected override string CredentialFileExtension => ".gpg";

        private string GetGpgId()
        {
            string gpgIdPath = Path.Combine(StoreRoot, ".gpg-id");
            if (!FileSystem.FileExists(gpgIdPath))
            {
                throw new Exception($"Cannot find GPG ID in '{gpgIdPath}'; password store has not been initialized");
            }

            using (var stream = FileSystem.OpenFileStream(gpgIdPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadLine();
            }
        }

        protected override bool TryDeserializeCredential(string path, out FileCredential credential)
        {
            string text = _gpg.DecryptFile(path);

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

        protected override void SerializeCredential(FileCredential credential)
        {
            string gpgId = GetGpgId();

            var sb = new StringBuilder(credential.Password);
            sb.AppendFormat("{1}service={0}{1}", credential.Service, Environment.NewLine);
            sb.AppendFormat("account={0}{1}", credential.Account, Environment.NewLine);
            string fileContents = sb.ToString();

            // Ensure the parent directory exists
            string parentDir = Path.GetDirectoryName(credential.FullPath);
            if (!FileSystem.DirectoryExists(parentDir))
            {
                FileSystem.CreateDirectory(parentDir);
            }

            // Delete any existing file
            if (FileSystem.FileExists(credential.FullPath))
            {
                FileSystem.DeleteFile(credential.FullPath);
            }

            // Encrypt!
            _gpg.EncryptFile(credential.FullPath, gpgId, fileContents);
        }
    }
}
