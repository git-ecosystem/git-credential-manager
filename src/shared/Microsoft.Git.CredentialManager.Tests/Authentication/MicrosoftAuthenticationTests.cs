// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Authentication
{
    public class MicrosoftAuthenticationTests
    {
        [Fact]
        public async System.Threading.Tasks.Task MicrosoftAuthentication_GetAccessTokenAsync_NoInteraction_ThrowsException()
        {
            const string authority = "https://login.microsoftonline.com/common";
            const string clientId = "C9E8FDA6-1D46-484C-917C-3DBD518F27C3";
            Uri redirectUri = new Uri("https://localhost");
            const string resource = "https://graph.microsoft.com";
            Uri remoteUri = new Uri("https://example.com");
            const string userName = null; // No user to ensure we do not use an existing token

            var context = new TestCommandContext
            {
                Settings = {IsInteractionAllowed = false},
            };

            var msAuth = new MicrosoftAuthentication(context);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => msAuth.GetTokenAsync(authority, clientId, redirectUri, resource, remoteUri, userName));
        }
    }
}
