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

        [PosixFact]
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

        [PosixFact]
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

        [PosixFact]
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

        [PosixFact]
        public void GnuPassCredentialStore_ReadWriteDelete_GpgIdInSubdirectory()
        {
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);

            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string storePath = Path.Combine(homePath, ".password-store");
            const string userId = "gcm-test@example.com";

            // Place .gpg-id only in the namespace subdirectory (not the store root),
            // simulating a pass store where the root has no .gpg-id but submodules do.
            string subDirPath = Path.Combine(storePath, TestNamespace);
            string gpgIdPath = Path.Combine(subDirPath, ".gpg-id");

            gpg.GenerateKeys(userId);

            fs.Directories.Add(storePath);
            fs.Directories.Add(subDirPath);
            fs.Files[gpgIdPath] = Encoding.UTF8.GetBytes(userId);

            var collection = new GpgPassCredentialStore(fs, gpg, storePath, TestNamespace);

            string service = $"https://example.com/{Guid.NewGuid():N}";
            const string userName = "john.doe";
            string password = Guid.NewGuid().ToString("N");

            try
            {
                // Write
                collection.AddOrUpdate(service, userName, password);

                // Read
                ICredential outCredential = collection.Get(service, userName);

                Assert.NotNull(outCredential);
                Assert.Equal(userName, outCredential.Account);
                Assert.Equal(password, outCredential.Password);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                collection.Remove(service, userName);
            }
        }

        [PosixFact]
        public void GnuPassCredentialStore_WriteCredential_MultipleGpgIds_UsesNearestGpgId()
        {
            // Verify that when two subdirectories each have their own .gpg-id, encrypting a credential
            // under one subdirectory uses that subdirectory's GPG identity, not the other one.
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);

            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string storePath = Path.Combine(homePath, ".password-store");

            const string personalUserId = "personal@example.com";
            const string workUserId = "work@example.com";

            // Only register the personal key; if the wrong (work) key is picked, EncryptFile will throw.
            gpg.GenerateKeys(personalUserId);

            string personalSubDir = Path.Combine(storePath, "personal");
            string workSubDir = Path.Combine(storePath, "work");

            fs.Directories.Add(storePath);
            fs.Directories.Add(personalSubDir);
            fs.Directories.Add(workSubDir);
            fs.Files[Path.Combine(personalSubDir, ".gpg-id")] = Encoding.UTF8.GetBytes(personalUserId);
            fs.Files[Path.Combine(workSubDir, ".gpg-id")] = Encoding.UTF8.GetBytes(workUserId);

            // Use "personal" namespace so credentials are stored under storePath/personal/...
            var collection = new GpgPassCredentialStore(fs, gpg, storePath, "personal");

            string service = $"https://example.com/{Guid.NewGuid():N}";
            const string userName = "john.doe";
            string password = Guid.NewGuid().ToString("N");

            try
            {
                // Write - should pick personal/.gpg-id (personalUserId), not work/.gpg-id (workUserId)
                collection.AddOrUpdate(service, userName, password);

                ICredential outCredential = collection.Get(service, userName);

                Assert.NotNull(outCredential);
                Assert.Equal(userName, outCredential.Account);
                Assert.Equal(password, outCredential.Password);
            }
            finally
            {
                collection.Remove(service, userName);
            }
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
