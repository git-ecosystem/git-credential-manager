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
        public void TtyPromptBasicAuthentication_GetCredentials_NullUri_ThrowsException()
        {
            var context = new TestCommandContext();
            var basicAuth = new TtyPromptBasicAuthentication(context);

            Assert.Throws<ArgumentNullException>(() => basicAuth.GetCredentials(null));
        }

        [Fact]
        public void TtyPromptBasicAuthentication_GetCredentials_UriWithUserInfo_PasswordPromptReturnsCredentials()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";

            var context = new TestCommandContext
            {
                Prompts = {["Password"] = testPassword}
            };

            var basicAuth = new TtyPromptBasicAuthentication(context);
            var uri = new Uri($"https://{testUserName}@example.com");

            GitCredential credential = basicAuth.GetCredentials(uri);

            Assert.Equal(testUserName, credential.UserName);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public void TtyPromptBasicAuthentication_GetCredentials_Uri_UserPassPromptReturnsCredentials()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";

            var context = new TestCommandContext
            {
                Prompts =
                {
                    ["Username"] = testUserName,
                    ["Password"] = testPassword
                }
            };

            var basicAuth = new TtyPromptBasicAuthentication(context);
            var uri = new Uri("https://example.com");

            GitCredential credential = basicAuth.GetCredentials(uri);

            Assert.Equal(testUserName, credential.UserName);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public void TtyPromptBasicAuthentication_GetCredentials_NoTerminalPrompts_ThrowsException()
        {
            var context = new TestCommandContext
            {
                EnvironmentVariables = {[Constants.EnvironmentVariables.GitTerminalPrompts] = "0"}
            };

            var basicAuth = new TtyPromptBasicAuthentication(context);
            var uri = new Uri("https://example.com");

            Assert.Throws<InvalidOperationException>(() => basicAuth.GetCredentials(uri));
        }
    }
}
