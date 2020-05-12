// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Git.CredentialManager.UI;

namespace Atlassian.Bitbucket.UI
{
    public class AuthenticationPrompts
    {
        public AuthenticationPrompts(IGui gui)
        {
            _gui = gui;
        }

        private readonly IGui _gui;

        public bool ShowCredentialsPrompt(ref string username, out string password)
        {
            password = null;
            return false;
        }

        public bool ShowOAuthPrompt()
        {
            return false;
        }
    }
}
