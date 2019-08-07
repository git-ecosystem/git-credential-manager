// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Authentication
{
    public class BasicAuthenticationTests
    {
        [Fact]
        public void BasicAuthentication_GetCredentials_NullResource_ThrowsException()
        {
            var context = new TestCommandContext();
            var basicAuth = new BasicAuthentication(context);

            Assert.Throws<ArgumentNullException>(() => basicAuth.GetCredentials(null));
        }

        [Fact]
        public void BasicAuthentication_GetCredentials_ResourceAndUserName_PasswordPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";

            var context = new TestCommandContext();
            context.Terminal.SecretPrompts["Password"] = testPassword;

            var basicAuth = new BasicAuthentication(context);

            GitCredential credential = basicAuth.GetCredentials(testResource, testUserName);

            Assert.Equal(testUserName, credential.UserName);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public void BasicAuthentication_GetCredentials_Resource_UserPassPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";

            var context = new TestCommandContext();
            context.Terminal.Prompts["Username"] = testUserName;
            context.Terminal.SecretPrompts["Password"] = testPassword;

            var basicAuth = new BasicAuthentication(context);

            GitCredential credential = basicAuth.GetCredentials(testResource);

            Assert.Equal(testUserName, credential.UserName);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public void BasicAuthentication_GetCredentials_NoTerminalPrompts_ThrowsException()
        {
            const string testResource = "https://example.com";

            var context = new TestCommandContext
            {
                Settings = {IsTerminalPromptsEnabled = false},
            };

            var basicAuth = new BasicAuthentication(context);

            Assert.Throws<InvalidOperationException>(() => basicAuth.GetCredentials(testResource));
        }
    }
}
