using System;
using System.IO;
using System.Text;
using Xunit;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Tests.Objects;

namespace GitCredentialManager.Tests.Interop.Posix
{
    public class GnuPassCredentialStoreTests
    {
        private const string TestNamespace = "git-test";

        [PlatformFact(Platforms.Posix)]
        public void GnuPassCredentialStore_ReadWriteDelete()
        {
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);
            string storeRoot = InitializePasswordStore(fs, gpg);

            var collection = new GpgPassCredentialStore(fs, gpg, storeRoot, TestNamespace);

            // Create a service that is guaranteed to be unique
            string uniqueGuid = Guid.NewGuid().ToString("N");
            string service = $"https://example.com/{uniqueGuid}";
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            string expectedSlug = $"{TestNamespace}/https/example.com/{uniqueGuid}/{userName}.gpg";
            string expectedFilePath = Path.Combine(storeRoot, expectedSlug);
            string expectedFileContents = password + Environment.NewLine +
                                          $"service={service}" + Environment.NewLine +
                                          $"account={userName}" + Environment.NewLine;
            byte[] expectedFileBytes = Encoding.UTF8.GetBytes(expectedFileContents);

            try
            {
                // Write
                collection.AddOrUpdate(service, userName, password);

                // Read
                ICredential outCredential = collection.Get(service, userName);

                Assert.NotNull(outCredential);
                Assert.Equal(userName, userName);
                Assert.Equal(password, outCredential.Password);
                Assert.True(fs.Files.ContainsKey(expectedFilePath));
                Assert.Equal(expectedFileBytes, fs.Files[expectedFilePath]);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                collection.Remove(service, userName);
            }
        }

        [PlatformFact(Platforms.Posix)]
        public void GnuPassCredentialStore_Get_NotFound_ReturnsNull()
        {
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);
            string storeRoot = InitializePasswordStore(fs, gpg);

            var collection = new GpgPassCredentialStore(fs, gpg, storeRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            ICredential credential = collection.Get(service, null);
            Assert.Null(credential);
        }

        [PlatformFact(Platforms.Posix)]
        public void GnuPassCredentialStore_Remove_NotFound_ReturnsFalse()
        {
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);
            string storeRoot = InitializePasswordStore(fs, gpg);

            var collection = new GpgPassCredentialStore(fs, gpg, storeRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            bool result = collection.Remove(service, account: null);
            Assert.False(result);
        }

        private static string InitializePasswordStore(TestFileSystem fs, TestGpg gpg)
        {
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string storePath = Path.Combine(homePath, ".password-store");
            string userId = "gcm-test@example.com";
            string gpgIdPath = Path.Combine(storePath, ".gpg-id");

            // Ensure we have a GPG key for use with testing
            gpg.GenerateKeys(userId);

            // Init the password store
            fs.Directories.Add(storePath);
            fs.Files[gpgIdPath] = Encoding.UTF8.GetBytes(userId);

            return storePath;
        }
    }
}
