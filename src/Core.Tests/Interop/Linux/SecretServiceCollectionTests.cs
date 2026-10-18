using System;
using Xunit;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Tests.Objects;
using Operation = GitCredentialManager.Interop.Linux.SecretServiceCollection.Operation;

namespace GitCredentialManager.Tests.Interop.Linux
{
    public class SecretServiceCollectionTests
    {
        private const string TestNamespace = "git-test";

        [LinuxFact]
        public void CredentialStore_SecretService_HeadlessSession_SelectsStore()
        {
            var context = new TestCommandContext
            {
                Settings = {CredentialBackingStore = Constants.CredentialStoreNames.SecretService},
                SessionManager = {IsDesktopSession = false},
            };

            var store = new CredentialStore(context);

            Assert.Equal("freedesktop.org Secret Service", store.Name);
        }

        [Theory]
        [InlineData(nameof(Operation.ConnectBus), "g-io-error-quark", 0, "D-Bus session bus is running and accessible")]
        [InlineData(nameof(Operation.OpenService), "g-dbus-error-quark", 2, "No Secret Service provider is available")]
        [InlineData(nameof(Operation.OpenService), "g-dbus-error-quark", 3, "No Secret Service provider is available")]
        [InlineData(nameof(Operation.Unlock), "secret-error", 2, "secret item or collection is locked")]
        [InlineData(nameof(Operation.Load), "secret-error", 2, "secret item or collection is locked")]
        [InlineData(nameof(Operation.Unlock), "g-io-error-quark", 19, "operation was cancelled")]
        [InlineData(nameof(Operation.Search), "g-dbus-error-quark", 2, "required D-Bus service is unavailable")]
        public void SecretServiceCollection_NativeFailure_PreservesErrorAndExplainsCause(
            string operation, string domain, int code, string expectedHint)
        {
            const string nativeMessage = "Native failure details";

            var exception = SecretServiceCollection.CreateException(Enum.Parse<Operation>(operation), domain, code, nativeMessage);

            Assert.Equal(code, exception.ErrorCode);
            Assert.Equal($"{domain}: {nativeMessage}", exception.InnerException?.Message);
            Assert.Contains(expectedHint, exception.Message);
            Assert.Contains(Constants.HelpUrls.GcmCredentialStores, exception.Message);
        }

        [Theory]
        [InlineData("g-io-error-quark", 2)]
        [InlineData("unrelated-error-domain", 19)]
        public void SecretServiceCollection_NativeFailure_DoesNotMisclassifyOtherDomains(string domain, int code)
        {
            var exception = SecretServiceCollection.CreateException(Operation.Search, domain, code, "Native failure");

            Assert.DoesNotContain("locked", exception.Message);
            Assert.DoesNotContain("cancelled", exception.Message);
            Assert.DoesNotContain("service is unavailable", exception.Message);
            Assert.Equal(code, exception.ErrorCode);
            Assert.Equal($"{domain}: Native failure", exception.InnerException?.Message);
        }

        [Theory]
        [InlineData(nameof(Operation.Unlock), "dismissed or cancelled")]
        [InlineData(nameof(Operation.Store), "dismissed or cancelled")]
        [InlineData(nameof(Operation.Load), "No secret value was returned")]
        public void SecretServiceCollection_FailureWithoutNativeError_ExplainsIncompleteOperation(
            string operation, string expectedHint)
        {
            var exception = SecretServiceCollection.CreateException(Enum.Parse<Operation>(operation), null, -1, null);

            Assert.Equal(-1, exception.ErrorCode);
            Assert.Null(exception.InnerException);
            Assert.Contains(expectedHint, exception.Message);
        }

        [LinuxFact(Skip = "Requires a configured Secret Service provider")]
        public void SecretServiceCollection_ReadWriteDelete()
        {
            var collection = new SecretServiceCollection(TestNamespace);

            // Create a service that is guaranteed to be unique
            string service = $"https://example.com/{Guid.NewGuid():N}";
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            try
            {
                // Write
                collection.AddOrUpdate(service, userName, password);

                // Read
                ICredential outCredential = collection.Get(service, userName);

                Assert.NotNull(outCredential);
                Assert.Equal(userName, userName);
                Assert.Equal(password, outCredential.Password);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                collection.Remove(service, userName);
            }
        }

        [LinuxFact(Skip = "Requires a configured Secret Service provider")]
        public void SecretServiceCollection_Get_NotFound_ReturnsNull()
        {
            var collection = new SecretServiceCollection(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            ICredential credential = collection.Get(service, null);
            Assert.Null(credential);
        }

        [LinuxFact(Skip = "Requires a configured Secret Service provider")]
        public void SecretServiceCollection_Remove_NotFound_ReturnsFalse()
        {
            var collection = new SecretServiceCollection(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            bool result = collection.Remove(service, account: null);
            Assert.False(result);
        }
        
        [LinuxFact]
        public void SecretServiceCollection_MaxCredentialSize_ReturnsZero()
        {
            ICredentialStore credentialStore = new SecretServiceCollection(TestNamespace);
            Assert.Equal(0, credentialStore.MaxCredentialSize);
        }
    }
}
