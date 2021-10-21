using System;
using Xunit;
using GitCredentialManager.Interop;
using GitCredentialManager.Interop.MacOS;
using GitCredentialManager.Interop.MacOS.Native;

namespace GitCredentialManager.Tests.Interop.MacOS
{
    public class MacOSKeychainTests
    {
        private const string TestNamespace = "git-test";

        [SkippablePlatformFact(Platforms.MacOS)]
        public void MacOSKeychain_ReadWriteDelete()
        {
            var keychain = new MacOSKeychain(TestNamespace);

            // Create a service that is guaranteed to be unique
            string service = $"https://example.com/{Guid.NewGuid():N}";
            const string account = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            try
            {
                // Write
                keychain.AddOrUpdate(service, account, password);

                // Read
                ICredential outCredential = keychain.Get(service, account);

                Assert.NotNull(outCredential);
                Assert.Equal(account, outCredential.Account);
                Assert.Equal(password, outCredential.Password);
            }
            // There is an unknown issue that the keychain can sometimes get itself in where all API calls
            // result in an errSecAuthFailed error. The only solution seems to be a machine restart, which
            // isn't really possible in CI!
            // The problem has plagued others who are calling the same Keychain APIs from C# such as the
            // MSAL.NET team - they don't know either. It might have something to do with the code signing
            // signature of the binary (our collective best theory).
            // It's probably only diagnosable at this point by Apple, but we don't have a reliable way to
            // reproduce the problem.
            // For now we will just mark the test as "skipped" when we hit this problem.
            catch (InteropException iex) when (iex.ErrorCode == SecurityFramework.ErrorSecAuthFailed)
            {
                AssertEx.Skip("macOS Keychain is in an invalid state (errSecAuthFailed)");
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                keychain.Remove(service, account);
            }
        }

        [PlatformFact(Platforms.MacOS)]
        public void MacOSKeychain_Get_NotFound_ReturnsNull()
        {
            var keychain = new MacOSKeychain(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            ICredential credential = keychain.Get(service, account: null);
            Assert.Null(credential);
        }

        [PlatformFact(Platforms.MacOS)]
        public void MacOSKeychain_Remove_NotFound_ReturnsFalse()
        {
            var keychain = new MacOSKeychain(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            bool result = keychain.Remove(service, account: null);
            Assert.False(result);
        }
    }
}
