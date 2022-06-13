using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitCredentialManager.Interop.Posix
{
    public class GpgPassCredentialStore : PlaintextCredentialStore
    {
        public delegate IGit GitFactory(string workdir);

        public const string PasswordStoreDirEnvar = "PASSWORD_STORE_DIR";

        private readonly IGpg _gpg;
        private readonly GitFactory _gitFactory;

        public GpgPassCredentialStore(ITrace trace, IFileSystem fileSystem, IGpg gpg, GitFactory gitFactory, string storeRoot, string @namespace = null)
            : base(trace, fileSystem, storeRoot, @namespace)
        {
            PlatformUtils.EnsurePosix();
            EnsureArgument.NotNull(gpg, nameof(gpg));

            _gpg = gpg;
            _gitFactory = gitFactory;
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

        private bool TryGetGitRepository(out IGit git)
        {
            if (_gitFactory != null)
            {
                string gitDir = Path.Combine(StoreRoot, ".git");
                if (FileSystem.DirectoryExists(gitDir))
                {
                    git = _gitFactory(StoreRoot);
                    return true;
                }
            }

            git = null;
            return false;
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

            // If the store is a Git repository then add and commit the file addition or modification
            if (TryGetGitRepository(out IGit git))
            {
                bool hasAccount = !string.IsNullOrWhiteSpace(credential.Account);

                sb.Clear();
                sb.AppendFormat("[GCM] Update credential {0}", credential.Service);
                if (hasAccount)
                {
                    sb.AppendFormat(" ({0})", credential.Account);
                }

                sb.AppendLine();
                sb.AppendLine();
                sb.AppendFormat("Update Git Credential Manager credential:{0}", Environment.NewLine);
                sb.AppendFormat("  Service: {1}{0}", Environment.NewLine, credential.Service);
                if (hasAccount)
                {
                    sb.AppendFormat("  Account: {1}{0}", Environment.NewLine, credential.Account);
                }
                sb.AppendLine();

                string message = sb.ToString();

                try
                {
                    git.AddFile(credential.FullPath);
                    git.Commit(message);
                }
                catch (GitException ex)
                {
                    // Don't fail just because we failed to commit in the pass store Git repo
                    Trace.WriteLine("Failed to update Git repository in pass store");
                    Trace.WriteException(ex);
                }
            }
        }

        protected override bool DestroyCredential(FileCredential credential)
        {
            FileSystem.DeleteFile(credential.FullPath);

            // If the store is a Git repository then add and commit the file deletion
            if (TryGetGitRepository(out IGit git))
            {
                bool hasAccount = !string.IsNullOrWhiteSpace(credential.Account);

                var sb = new StringBuilder();
                sb.AppendFormat("[GCM] Remove credential {0}", credential.Service);
                if (hasAccount)
                {
                    sb.AppendFormat(" ({0})", credential.Account);
                }

                sb.AppendLine();
                sb.AppendLine();
                sb.AppendFormat("Remove Git Credential Manager credential from pass:{0}", Environment.NewLine);
                sb.AppendFormat("  Service: {0}{1}", credential.Service, Environment.NewLine);
                if (hasAccount)
                {
                    sb.AppendFormat("  Account: {0}{1}", credential.Account, Environment.NewLine);
                }

                string message = sb.ToString();

                try
                {
                    git.AddFile(credential.FullPath);
                    git.Commit(message);
                }
                catch (GitException ex)
                {
                    // Don't fail just because we failed to commit in the pass store Git repo
                    Trace.WriteLine("Failed to update Git repository in pass store");
                    Trace.WriteException(ex);
                }
            }

            return true;
        }
    }
}
