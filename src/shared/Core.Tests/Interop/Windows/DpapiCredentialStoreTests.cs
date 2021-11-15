using System;
using Xunit;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.Tests.Objects;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace GitCredentialManager.Tests.Interop.Windows
{
    public class DpapiCredentialStoreTests
    {
        private const string TestStoreRoot = @"C:\dpapi_store";
        private const string TestNamespace = "git-test";

        [PlatformFact(Platforms.Windows)]
        public void DpapiCredentialStore_AddOrUpdate_CreatesUTF8ProtectedFile()
        {
            var fs = new TestFileSystem();
            var store = new DpapiCredentialStore(fs, TestStoreRoot, TestNamespace);

            string service = "https://example.com";
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            string expectedServiceSlug = Path.Combine(TestNamespace, "https", "example.com");
            string expectedFileName = $"{userName}.credential";
            string expectedFilePath = Path.Combine(TestStoreRoot, expectedServiceSlug, expectedFileName);

            store.AddOrUpdate(service, userName, password);

            Assert.True(fs.Directories.Contains(Path.Combine(TestStoreRoot, expectedServiceSlug)));
            Assert.True(fs.Files.TryGetValue(expectedFilePath, out byte[] data));

            string contents = Encoding.UTF8.GetString(data);
            Assert.False(string.IsNullOrWhiteSpace(contents));

            string[] lines = contents.Split(Environment.NewLine);

            Assert.Equal(4, lines.Length);

            byte[] cryptoData = Convert.FromBase64String(lines[0]);
            byte[] plainData = ProtectedData.Unprotect(cryptoData, null, DataProtectionScope.CurrentUser);
            string plainLine0 = Encoding.UTF8.GetString(plainData);

            Assert.Equal(password, plainLine0);
            Assert.Equal($"service={service}", lines[1]);
            Assert.Equal($"account={userName}", lines[2]);
            Assert.True(string.IsNullOrWhiteSpace(lines[3]));
        }

        [PlatformFact(Platforms.Windows)]
        public void DpapiCredentialStore_Get_KeyNotFound_ReturnsNull()
        {
            var fs = new TestFileSystem();
            var store = new DpapiCredentialStore(fs, TestStoreRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = Guid.NewGuid().ToString("N");

            ICredential credential = store.Get(service, account: null);
            Assert.Null(credential);
        }

        [PlatformFact(Platforms.Windows)]
        public void DpapiCredentialStore_Get_ReadProtectedFile()
        {
            var fs = new TestFileSystem();
            var store = new DpapiCredentialStore(fs, TestStoreRoot, TestNamespace);

            string service = "https://example.com";
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            string serviceSlug = Path.Combine(TestNamespace, "https", "example.com");
            string fileName = $"{userName}.credential";
            string filePath = Path.Combine(TestStoreRoot, serviceSlug, fileName);

            byte[] plainData = Encoding.UTF8.GetBytes(password);
            byte[] cryptoData = ProtectedData.Protect(plainData, null, DataProtectionScope.CurrentUser);
            string cryptoLine0 = Convert.ToBase64String(cryptoData);

            var contents = new StringBuilder();
            contents.AppendLine(cryptoLine0);
            contents.AppendLine($"service={service}");
            contents.AppendLine($"account={userName}");
            contents.AppendLine();

            byte[] data = Encoding.UTF8.GetBytes(contents.ToString());

            fs.Directories.Add(Path.Combine(TestStoreRoot, serviceSlug));
            fs.Files[filePath] = data;

            ICredential credential = store.Get(service, userName);

            Assert.Equal(password, credential.Password);
            Assert.Equal(userName, credential.Account);
        }

        [PlatformFact(Platforms.Windows)]
        public void DpapiCredentialStore_Remove_KeyNotFound_ReturnsFalse()
        {
            var fs = new TestFileSystem();
            var store = new DpapiCredentialStore(fs, TestStoreRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = Guid.NewGuid().ToString("N");

            bool result = store.Remove(service, account: null);
            Assert.False(result);
        }
    }
}
