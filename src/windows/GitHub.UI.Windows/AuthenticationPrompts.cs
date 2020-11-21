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

        public CredentialPromptResult ShowCredentialPrompt(string enterpriseUrl, bool showPassword, bool showPAT, bool showOAuth, ref string username, out string password)
        {
            password = null;

            var viewModel = new LoginCredentialsViewModel(enterpriseUrl, username, showPassword, showPAT, showOAuth);

            bool valid = _gui.ShowDialogWindow(viewModel, () => new LoginCredentialsView());

            if (viewModel.HasUsedBrowserLogin)
            {
                return CredentialPromptResult.OAuth;
            }

            if (valid)
            {
                username = viewModel.UsernameOrEmail;
                // Trim because when copying PAT from browsers it's easy to copy an additional whitespace.
                password = viewModel.Password.ToUnsecureString().Trim();
                if (showPassword && !showPAT) return CredentialPromptResult.Password;
                if (!showPassword && showPAT) return CredentialPromptResult.PAT;
                return CredentialPromptResult.Basic;
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
        Basic,
        Password,
        PAT,
        OAuth,
        Cancel,
    }
}
