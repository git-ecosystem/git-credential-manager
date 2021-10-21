using System;
using System.IO;
using System.Text;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class PlaintextCredentialStoreTests
    {
        private const string TestNamespace = "git-test";
        private const string StoreRoot = "/tmp/test-store";

        [Fact]
        public void PlaintextCredentialStore_ReadWriteDelete()
        {
            var fs = new TestFileSystem();

            var collection = new PlaintextCredentialStore(fs, StoreRoot, TestNamespace);

            // Create a service that is guaranteed to be unique
            string uniqueGuid = Guid.NewGuid().ToString("N");
            string service = $"https://example.com/{uniqueGuid}";
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            string expectedSlug = Path.Combine(
                TestNamespace,
                "https",
                "example.com",
                uniqueGuid,
                $"{userName}.credential");
            string expectedFilePath = Path.Combine(StoreRoot, expectedSlug);
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

        [Fact]
        public void PlaintextCredentialStore_Get_NotFound_ReturnsNull()
        {
            var fs = new TestFileSystem();

            var collection = new PlaintextCredentialStore(fs, StoreRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            ICredential credential = collection.Get(service, null);
            Assert.Null(credential);
        }

        [Fact]
        public void PlaintextCredentialStore_Remove_NotFound_ReturnsFalse()
        {
            var fs = new TestFileSystem();

            var collection = new PlaintextCredentialStore(fs, StoreRoot, TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            bool result = collection.Remove(service, account: null);
            Assert.False(result);
        }
    }
}
