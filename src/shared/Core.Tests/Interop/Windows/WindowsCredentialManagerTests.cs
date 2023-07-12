using System;
using Xunit;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.Interop.Windows.Native;

namespace GitCredentialManager.Tests.Interop.Windows
{
    public class WindowsCredentialManagerTests
    {
        private const string TestNamespace = "git-test";

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_ReadWriteDelete()
        {
            var credManager = new WindowsCredentialManager(TestNamespace);

            // Create a service that is guaranteed to be unique
            string uniqueGuid = Guid.NewGuid().ToString("N");
            string service = $"https://example.com/{uniqueGuid}";
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            string expectedTargetName = $"{TestNamespace}:https://example.com/{uniqueGuid}";

            try
            {
                // Write
                credManager.AddOrUpdate(service, userName, password);

                // Read
                ICredential cred = credManager.Get(service, userName);

                // Valdiate
                var winCred = cred as WindowsCredential;
                Assert.NotNull(winCred);
                Assert.Equal(userName, winCred.UserName);
                Assert.Equal(password, winCred.Password);
                Assert.Equal(service, winCred.Service);
                Assert.Equal(expectedTargetName, winCred.TargetName);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                credManager.Remove(service, userName);
            }
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_AddOrUpdate_UsernameWithAtCharacter()
        {
            var credManager = new WindowsCredentialManager(TestNamespace);

            // Create a service that is guaranteed to be unique
            string uniqueGuid = Guid.NewGuid().ToString("N");
            string service = $"https://example.com/{uniqueGuid}";
            const string userName = "john.doe@auth.com";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            string expectedTargetName = $"{TestNamespace}:https://example.com/{uniqueGuid}";

            try
            {
                // Write
                credManager.AddOrUpdate(service, userName, password);

                // Read
                ICredential cred = credManager.Get(service, userName);

                // Validate
                var winCred = cred as WindowsCredential;
                Assert.NotNull(winCred);
                Assert.Equal(userName, winCred.UserName);
                Assert.Equal(password, winCred.Password);
                Assert.Equal(service, winCred.Service);
                Assert.Equal(expectedTargetName, winCred.TargetName);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                credManager.Remove(service, userName);
            }
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_Get_KeyNotFound_ReturnsNull()
        {
            var credManager = new WindowsCredentialManager(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = Guid.NewGuid().ToString("N");

            ICredential credential = credManager.Get(service, account: null);
            Assert.Null(credential);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_Remove_KeyNotFound_ReturnsFalse()
        {
            var credManager = new WindowsCredentialManager(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = Guid.NewGuid().ToString("N");

            bool result = credManager.Remove(service, account: null);
            Assert.False(result);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_AddOrUpdate_TargetNameAlreadyExists_CreatesWithUserInTargetName()
        {
            var credManager = new WindowsCredentialManager(TestNamespace);

            // Create a service that is guaranteed to be unique
            string uniqueGuid = Guid.NewGuid().ToString("N");
            string service = $"https://example.com/{uniqueGuid}";
            const string userName1 = "john.doe";
            const string userName2 = "jane.doe";
            const string password1 = "letmein123";  // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string password2 = "password123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            string expectedTargetName1 = $"{TestNamespace}:https://example.com/{uniqueGuid}";
            string expectedTargetName2 = $"{TestNamespace}:https://{userName2}@example.com/{uniqueGuid}";

            try
            {
                // Add first credential
                credManager.AddOrUpdate(service, userName1, password1);

                // Add second credential
                credManager.AddOrUpdate(service, userName2, password2);

                // Validate first credential properties
                ICredential cred1 = credManager.Get(service, userName1);
                var winCred1 = cred1 as WindowsCredential;
                Assert.NotNull(winCred1);
                Assert.Equal(userName1, winCred1.UserName);
                Assert.Equal(password1, winCred1.Password);
                Assert.Equal(service,   winCred1.Service);
                Assert.Equal(expectedTargetName1, winCred1.TargetName);

                // Validate second credential properties
                ICredential cred2 = credManager.Get(service, userName2);
                var winCred2 = cred2 as WindowsCredential;
                Assert.NotNull(winCred2);
                Assert.Equal(userName2, winCred2.UserName);
                Assert.Equal(password2, winCred2.Password);
                Assert.Equal(service,   winCred2.Service);
                Assert.Equal(expectedTargetName2, winCred2.TargetName);
            }
            finally
            {
                // Ensure we clean up after ourselves in case of failures
                credManager.Remove(service, userName1);
                credManager.Remove(service, userName2);
            }
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_AddOrUpdate_TargetNameAlreadyExistsAndUserWithAtCharacter_CreatesWithEscapedUserInTargetName()
        {
            var credManager = new WindowsCredentialManager(TestNamespace);

            // Create a service that is guaranteed to be unique
            string uniqueGuid = Guid.NewGuid().ToString("N");
            string service = $"https://example.com/{uniqueGuid}";
            const string userName1 = "john.doe@auth.com";
            const string userName2 = "jane.doe@auth.com";
            const string escapedUserName2 = "jane.doe_auth.com";
            const string password1 = "letmein123";  // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string password2 = "password123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            string expectedTargetName1 = $"{TestNamespace}:https://example.com/{uniqueGuid}";
            string expectedTargetName2 = $"{TestNamespace}:https://{escapedUserName2}@example.com/{uniqueGuid}";

            try
            {
                // Add first credential
                credManager.AddOrUpdate(service, userName1, password1);

                // Add second credential
                credManager.AddOrUpdate(service, userName2, password2);

                // Validate first credential properties
                ICredential cred1 = credManager.Get(service, userName1);
                var winCred1 = cred1 as WindowsCredential;
                Assert.NotNull(winCred1);
                Assert.Equal(userName1, winCred1.UserName);
                Assert.Equal(password1, winCred1.Password);
                Assert.Equal(service,   winCred1.Service);
                Assert.Equal(expectedTargetName1, winCred1.TargetName);

                // Validate second credential properties
                ICredential cred2 = credManager.Get(service, userName2);
                var winCred2 = cred2 as WindowsCredential;
                Assert.NotNull(winCred2);
                Assert.Equal(userName2, winCred2.UserName);
                Assert.Equal(password2, winCred2.Password);
                Assert.Equal(service,   winCred2.Service);
                Assert.Equal(expectedTargetName2, winCred2.TargetName);
            }
            finally
            {
                // Ensure we clean up after ourselves in case of failures
                credManager.Remove(service, userName1);
                credManager.Remove(service, userName2);
            }
        }

        [Theory]
        [InlineData("https://example.com", "https://example.com")]
        [InlineData("https://example.com/", "https://example.com/")]
        [InlineData("https://example.com/@", "https://example.com/@")]
        [InlineData("https://example.com/path", "https://example.com/path")]
        [InlineData("https://example.com/path@", "https://example.com/path@")]
        [InlineData("https://example.com:123/path@", "https://example.com:123/path@")]
        [InlineData("https://example.com/path/", "https://example.com/path/")]
        [InlineData("https://example.com/path@/", "https://example.com/path@/")]
        [InlineData("https://example.com:123/path@/", "https://example.com:123/path@/")]
        [InlineData("https://example.com/path/foo", "https://example.com/path/foo")]
        [InlineData("https://example.com/path@/foo", "https://example.com/path@/foo")]
        [InlineData("https://userinfo@example.com", "https://example.com")]
        [InlineData("https://userinfo@example.com/", "https://example.com/")]
        [InlineData("https://userinfo@example.com/@", "https://example.com/@")]
        [InlineData("https://userinfo@example.com/path", "https://example.com/path")]
        [InlineData("https://userinfo@example.com/path@", "https://example.com/path@")]
        [InlineData("https://userinfo@example.com/path/", "https://example.com/path/")]
        [InlineData("https://userinfo@example.com:123/path/", "https://example.com:123/path/")]
        [InlineData("https://userinfo@example.com/path@/", "https://example.com/path@/")]
        [InlineData("https://userinfo@example.com:123/path@/", "https://example.com:123/path@/")]
        [InlineData("https://userinfo@example.com/path/foo", "https://example.com/path/foo")]
        [InlineData("https://userinfo@example.com/path@/foo", "https://example.com/path@/foo")]
        public void WindowsCredentialManager_RemoveUriUserInfo(string input, string expected)
        {
            string actual = WindowsCredentialManager.RemoveUriUserInfo(input);
            Assert.Equal(expected, actual);
        }

        [PlatformTheory(Platforms.Windows)]
        [InlineData("https://example.com", null, "https://example.com", "alice", true)]
        [InlineData("https://example.com", "alice", "https://example.com", "alice", true)]
        [InlineData("https://example.com", null, "https://example.com:443", "alice", true)]
        [InlineData("https://example.com", "alice", "https://example.com:443", "alice", true)]
        [InlineData("https://example.com:1234", null, "https://example.com:1234", "alice", true)]
        [InlineData("https://example.com:1234", "alice", "https://example.com:1234", "alice", true)]
        [InlineData("https://example.com", null, "http://example.com", "alice", false)]
        [InlineData("https://example.com", "alice", "http://example.com", "alice", false)]
        [InlineData("https://example.com", "alice", "http://example.com:443", "alice", false)]
        [InlineData("http://example.com:443", "alice", "https://example.com", "alice", false)]
        [InlineData("https://example.com", "bob", "https://example.com", "alice", false)]
        [InlineData("https://example.com", "bob", "https://bob@example.com", "bob", true)]
        [InlineData("https://example.com", "bob", "https://example.com", "bob", true)]
        [InlineData("https://example.com", "alice", "https://example.com", "ALICE", false)] // username case sensitive
        [InlineData("https://example.com", "alice", "https://EXAMPLE.com", "alice", true)] // host NOT case sensitive
        [InlineData("https://example.com/path", "alice", "https://example.com/path", "alice", true)]
        [InlineData("https://example.com/path", "alice", "https://example.com/PATH", "alice", true)] // path NOT case sensitive
        public void WindowsCredentialManager_IsMatch(
            string service, string account, string targetName, string userName, bool expected)
        {
            string fullTargetName = $"{WindowsCredentialManager.TargetNameLegacyGenericPrefix}{TestNamespace}:{targetName}";
            var win32Cred = new Win32Credential
            {
                UserName =  userName,
                TargetName = fullTargetName
            };

            var credManager = new WindowsCredentialManager(TestNamespace);

            bool actual = credManager.IsMatch(service, account, win32Cred);

            Assert.Equal(expected, actual);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_IsMatch_NoNamespace_NotMatched()
        {
            var win32Cred = new Win32Credential
            {
                UserName = "test",
                TargetName = $"{WindowsCredentialManager.TargetNameLegacyGenericPrefix}https://example.com"
            };

            var credManager = new WindowsCredentialManager(TestNamespace);

            bool result = credManager.IsMatch("https://example.com", null, win32Cred);

            Assert.False(result);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_IsMatch_DifferentNamespace_NotMatched()
        {
            var win32Cred = new Win32Credential
            {
                UserName = "test",
                TargetName = $"{WindowsCredentialManager.TargetNameLegacyGenericPrefix}:random-namespace:https://example.com"
            };

            var credManager = new WindowsCredentialManager(TestNamespace);

            bool result = credManager.IsMatch("https://example.com", null, win32Cred);

            Assert.False(result);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_IsMatch_CaseSensitiveNamespace_NotMatched()
        {
            var win32Cred = new Win32Credential
            {
                UserName = "test",
                TargetName = $"{WindowsCredentialManager.TargetNameLegacyGenericPrefix}:nAmEsPaCe:https://example.com"
            };

            var credManager = new WindowsCredentialManager("namespace");

            bool result = credManager.IsMatch("https://example.com", null, win32Cred);

            Assert.False(result);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsCredentialManager_IsMatch_NoNamespaceInQuery_IsMatched()
        {
            var win32Cred = new Win32Credential
            {
                UserName = "test",
                TargetName = $"{WindowsCredentialManager.TargetNameLegacyGenericPrefix}https://example.com"
            };

            var credManager = new WindowsCredentialManager();

            bool result = credManager.IsMatch("https://example.com", null, win32Cred);

            Assert.True(result);
        }

        [PlatformTheory(Platforms.Windows)]
        [InlineData("https://example.com", null, "https://example.com")]
        [InlineData("https://example.com", "bob", "https://bob@example.com")]
        [InlineData("https://example.com", "bob@id.example.com", "https://bob_id.example.com@example.com")] // @ in user
        [InlineData("https://example.com:443", null, "https://example.com")] // default port
        [InlineData("https://example.com:1234", null, "https://example.com:1234")]
        [InlineData("https://example.com/path", null, "https://example.com/path")]
        [InlineData("https://example.com/path/with/more/parts", null, "https://example.com/path/with/more/parts")]
        [InlineData("https://example.com/path/trim/", null, "https://example.com/path/trim")] // path trailing slash
        [InlineData("https://example.com/", null, "https://example.com")] // no path trailing slash
        public void WindowsCredentialManager_CreateTargetName(string service, string account, string expected)
        {
            string fullExpected = $"{TestNamespace}:{expected}";

            var credManager = new WindowsCredentialManager(TestNamespace);

            string actual = credManager.CreateTargetName(service, account);

            Assert.Equal(fullExpected, actual);
        }

        [PlatformTheory(Platforms.Windows)]
        [InlineData(TestNamespace, "https://example.com", null, $"{TestNamespace}:https://example.com")]
        [InlineData(null, "https://example.com", null, "https://example.com")]
        [InlineData("", "https://example.com", null, "https://example.com")]
        [InlineData("    ", "https://example.com", null, "https://example.com")]
        public void WindowsCredentialManager_CreateTargetName_Namespace(string @namespace, string service, string account, string expected)
        {
            var credManager = new WindowsCredentialManager(@namespace);

            string actual = credManager.CreateTargetName(service, account);

            Assert.Equal(expected, actual);
        }
    }
}
