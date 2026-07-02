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
        [Fact]
        public void PlaintextCredentialStore_AccountWithPathSeparators_StoresInServiceDirectory()
        {
            var fs = new TestFileSystem();

            var collection = new PlaintextCredentialStore(fs, StoreRoot, TestNamespace);

            string uniqueGuid = Guid.NewGuid().ToString("N");
            string service = $"https://example.com/{uniqueGuid}";
            // Account name with path traversal characters
            const string userName = "../../malicious/account";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            // Expected: path separators replaced with '_', file stays inside service directory
            string safeUserName = ".._.._malicious_account";
            string expectedSlug = Path.Combine(
                TestNamespace,
                "https",
                "example.com",
                uniqueGuid,
                $"{safeUserName}.credential");
            string expectedFilePath = Path.Combine(StoreRoot, expectedSlug);

            // Write
            collection.AddOrUpdate(service, userName, password);

            // Verify the file is created inside the expected service directory (no traversal)
            Assert.True(fs.Files.ContainsKey(expectedFilePath),
                $"Expected credential file at '{expectedFilePath}' but it was not found.");

            // Verify no files were created outside the store root
            foreach (string filePath in fs.Files.Keys)
            {
                Assert.True(filePath.StartsWith(StoreRoot, StringComparison.Ordinal),
                    $"Credential file '{filePath}' was created outside the store root.");
            }

            // Verify the credential can be retrieved using the original account name
            ICredential outCredential = collection.Get(service, userName);
            Assert.NotNull(outCredential);
            Assert.Equal(password, outCredential.Password);
        }

    }
}
