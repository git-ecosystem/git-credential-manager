// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Authentication
{
    public class TtyPromptBasicAuthenticationTests
    {
        [Fact]
        public void TtyPromptBasicAuthentication_GetCredentials_NullResource_ThrowsException()
        {
            var context = new TestCommandContext();
            var basicAuth = new TtyPromptBasicAuthentication(context);

            Assert.Throws<ArgumentNullException>(() => basicAuth.GetCredentials(null));
        }

        [Fact]
        public void TtyPromptBasicAuthentication_GetCredentials_ResourceAndUserName_PasswordPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";

            var context = new TestCommandContext
            {
                SecretPrompts = {["Password"] = testPassword}
            };

            var basicAuth = new TtyPromptBasicAuthentication(context);

            GitCredential credential = basicAuth.GetCredentials(testResource, testUserName);

            Assert.Equal(testUserName, credential.UserName);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public void TtyPromptBasicAuthentication_GetCredentials_Resource_UserPassPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";

            var context = new TestCommandContext
            {
                Prompts       = {["Username"] = testUserName},
                SecretPrompts = {["Password"] = testPassword}
            };

            var basicAuth = new TtyPromptBasicAuthentication(context);

            GitCredential credential = basicAuth.GetCredentials(testResource);

            Assert.Equal(testUserName, credential.UserName);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public void TtyPromptBasicAuthentication_GetCredentials_NoTerminalPrompts_ThrowsException()
        {
            const string testResource = "https://example.com";

            var context = new TestCommandContext
            {
                EnvironmentVariables = {[Constants.EnvironmentVariables.GitTerminalPrompts] = "0"}
            };

            var basicAuth = new TtyPromptBasicAuthentication(context);

            Assert.Throws<InvalidOperationException>(() => basicAuth.GetCredentials(testResource));
        }
    }
}
