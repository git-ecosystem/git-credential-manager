using System;
using System.Collections.Generic;
using System.Text;
using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Tests.Objects
{
    public class TestGpg : IGpg
    {
        private readonly TestFileSystem _fs;
        private readonly ISet<string> _keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public TestGpg(TestFileSystem fs)
        {
            _fs = fs;
        }

        public string DecryptFile(string path)
        {
            // No encryption
            return Encoding.UTF8.GetString(_fs.Files[path]);
        }

        public void EncryptFile(string path, string gpgId, string contents)
        {
            if (!_keys.Contains(gpgId))
            {
                throw new Exception($"No GPG key found for '{gpgId}'.");
            }

            // No encryption
            _fs.Files[path] = Encoding.UTF8.GetBytes(contents);
        }

        public void GenerateKeys(string userId)
        {
            _keys.Add(userId);
        }
    }
}
