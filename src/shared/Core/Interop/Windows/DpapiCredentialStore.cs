using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GitCredentialManager.Interop.Windows
{
    public class DpapiCredentialStore : PlaintextCredentialStore
    {
        public DpapiCredentialStore(IFileSystem fileSystem, string storeRoot, string @namespace = null)
            : base(fileSystem, storeRoot, @namespace)
        {
            PlatformUtils.EnsureWindows();
        }

        protected override bool TryDeserializeCredential(string path, out FileCredential credential)
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
                // The first line is a base64 encoded set of bytes that need to be decrypted by DPAPI
                string cryptoBase64 = text.Substring(0, line1Idx);
                byte[] cryptoBytes = Convert.FromBase64String(cryptoBase64);
                byte[] plainBytes = ProtectedData.Unprotect(
                    cryptoBytes, null, DataProtectionScope.CurrentUser);
                string password = Encoding.UTF8.GetString(plainBytes);

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
            // Ensure the parent directory exists
            string parentDir = Path.GetDirectoryName(credential.FullPath);
            if (!FileSystem.DirectoryExists(parentDir))
            {
                FileSystem.CreateDirectory(parentDir);
            }

            // Use DPAPI to encrypt the password value, and then store the base64 encoding of the resulting bytes
            byte[] plainBytes = Encoding.UTF8.GetBytes(credential.Password);
            byte[] cryptoBytes = ProtectedData.Protect(
                plainBytes, null, DataProtectionScope.CurrentUser);
            string cryptoBase64 = Convert.ToBase64String(cryptoBytes);

            using (var stream = FileSystem.OpenFileStream(credential.FullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(cryptoBase64);
                writer.WriteLine("service={0}", credential.Service);
                writer.WriteLine("account={0}", credential.Account);
                writer.Flush();
            }
        }
    }
}
