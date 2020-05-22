// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using GitHub.UI.Login;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.Controls;

namespace GitHub.UI
{
    public class AuthenticationPrompts
    {
        public AuthenticationPrompts(IGui gui)
        {
            _gui = gui;
        }

        private readonly IGui _gui;

        public CredentialPromptResult ShowCredentialPrompt(string enterpriseUrl, bool showBasic, bool showOAuth, out string username, out string password)
        {
            username = null;
            password = null;

            var viewModel = new LoginCredentialsViewModel(showBasic, showOAuth)
            {
                GitHubEnterpriseUrl = enterpriseUrl
            };

            bool valid = _gui.ShowDialogWindow(viewModel, () => new LoginCredentialsView());

            if (viewModel.UseBrowserLogin)
            {
                return CredentialPromptResult.OAuthAuthentication;
            }

            if (valid)
            {
                username = viewModel.UsernameOrEmail;
                password = viewModel.Password.ToUnsecureString();
                return CredentialPromptResult.BasicAuthentication;
            }

            return CredentialPromptResult.Cancel;
        }

        public bool ShowAuthenticationCodePrompt(bool isSms, out string authenticationCode)
        {
            var viewModel = new Login2FaViewModel(isSms ? TwoFactorType.Sms : TwoFactorType.AuthenticatorApp);

            bool valid = _gui.ShowDialogWindow(viewModel, () => new Login2FaView());

            authenticationCode = valid ? viewModel.AuthenticationCode : null;

            return valid;
        }
    }

    public enum CredentialPromptResult
    {
        BasicAuthentication,
        OAuthAuthentication,
        Cancel,
    }
}
