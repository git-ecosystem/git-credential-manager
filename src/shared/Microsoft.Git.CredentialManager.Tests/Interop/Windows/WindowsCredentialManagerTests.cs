// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Xunit;
using Microsoft.Git.CredentialManager.Interop.Windows;

namespace Microsoft.Git.CredentialManager.Tests.Interop.Windows
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
            const string password = "letmein123";

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
            const string password = "letmein123";

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
            const string password1 = "letmein123";
            const string password2 = "password123";

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
            const string password1 = "letmein123";
            const string password2 = "password123";

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
    }
}
