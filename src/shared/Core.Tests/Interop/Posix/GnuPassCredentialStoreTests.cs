using System;
using System.IO;
using System.Text;
using Xunit;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Tests.Objects;
using Moq;

namespace GitCredentialManager.Tests.Interop.Posix
{
    public class GnuPassCredentialStoreTests
    {
        private const string TestNamespace = "git-test";

        [PlatformFact(Platforms.Posix)]
        public void GnuPassCredentialStore_ReadWriteDelete()
        {
            var trace = new NullTrace();
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);
            string storeRoot = InitializePasswordStore(fs, gpg);

            var collection = new GpgPassCredentialStore(trace, fs, gpg, null, storeRoot, TestNamespace);

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
            var trace = new NullTrace();
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);
            string storeRoot = InitializePasswordStore(fs, gpg);

            var collection = new GpgPassCredentialStore(trace, fs, gpg, null, storeRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            ICredential credential = collection.Get(service, null);
            Assert.Null(credential);
        }

        [PlatformFact(Platforms.Posix)]
        public void GnuPassCredentialStore_Remove_NotFound_ReturnsFalse()
        {
            var trace = new NullTrace();
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);
            string storeRoot = InitializePasswordStore(fs, gpg);

            var collection = new GpgPassCredentialStore(trace, fs, gpg, null, storeRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            bool result = collection.Remove(service, account: null);
            Assert.False(result);
        }

        [PlatformFact(Platforms.Posix)]
        public void GnuPassCredentialStore_ReadWriteDelete_GitRepo()
        {
            var trace = new NullTrace();
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);
            string storeRoot = InitializePasswordStore(fs, gpg, gitInit: true);

            var mockGit = new Mock<IGit>();
            IGit GitFactory(string path) => mockGit.Object;

            var collection = new GpgPassCredentialStore(trace, fs, gpg, GitFactory, storeRoot, TestNamespace);

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

            mockGit.Setup(x => x.AddFile(expectedFilePath));
            mockGit.Setup(x => x.Commit(It.IsAny<string>()));

            try
            {
                // Write
                collection.AddOrUpdate(service, userName, password);

                mockGit.Verify(x => x.AddFile(expectedFilePath), Times.Once);
                mockGit.Verify(x => x.Commit(It.IsAny<string>()), Times.Once);
                mockGit.Reset();

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
                mockGit.Verify(x => x.AddFile(expectedFilePath), Times.Once);
                mockGit.Verify(x => x.Commit(It.IsAny<string>()), Times.Once);
            }
        }

        [PlatformFact(Platforms.Posix)]
        public void GnuPassCredentialStore_Get_GitRepo_NotFound_ReturnsNull()
        {
            var trace = new NullTrace();
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);
            string storeRoot = InitializePasswordStore(fs, gpg, gitInit: true);

            var mockGit = new Mock<IGit>();
            IGit GitFactory(string path) => mockGit.Object;

            var collection = new GpgPassCredentialStore(trace, fs, gpg, GitFactory, storeRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            ICredential credential = collection.Get(service, null);
            Assert.Null(credential);
            mockGit.VerifyNoOtherCalls();
        }

        [PlatformFact(Platforms.Posix)]
        public void GnuPassCredentialStore_Remove_GitRepo_NotFound_ReturnsFalse()
        {
            var trace = new NullTrace();
            var fs = new TestFileSystem();
            var gpg = new TestGpg(fs);
            string storeRoot = InitializePasswordStore(fs, gpg, gitInit: true);

            var mockGit = new Mock<IGit>();
            IGit GitFactory(string path) => mockGit.Object;

            var collection = new GpgPassCredentialStore(trace, fs, gpg, GitFactory, storeRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            bool result = collection.Remove(service, account: null);
            Assert.False(result);
            mockGit.VerifyNoOtherCalls();
        }

        private static string InitializePasswordStore(TestFileSystem fs, TestGpg gpg, bool gitInit = false)
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

            // Init the Git repository
            if (gitInit)
            {
                string gitDir = Path.Combine(storePath, ".git");
                fs.Directories.Add(gitDir);
            }

            return storePath;
        }
    }
}
