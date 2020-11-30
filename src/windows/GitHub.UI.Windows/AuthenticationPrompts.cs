// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using GitHub.UI.Login;
using Microsoft.Git.CredentialManager.UI;

namespace GitHub.UI
{
    public class AuthenticationPrompts
    {
        public AuthenticationPrompts(IGui gui)
        {
            _gui = gui;
        }

        private readonly IGui _gui;

        public CredentialPromptResult ShowCredentialPrompt(
            string enterpriseUrl, bool showBasic, bool showOAuth, bool showPat,
            ref string username, out string password, out string token)
        {
            password = null;
            token = null;

            var viewModel = new LoginCredentialsViewModel(showBasic, showOAuth, showPat)
            {
                GitHubEnterpriseUrl = enterpriseUrl,
                UsernameOrEmail = username
            };

            bool valid = _gui.ShowDialogWindow(viewModel, () => new LoginCredentialsView());

            switch (viewModel.SelectedAuthType)
            {
                case CredentialPromptResult.BasicAuthentication:
                    if (valid)
                    {
                        username = viewModel.UsernameOrEmail;
                        password = viewModel.Password.ToUnsecureString();
                        return CredentialPromptResult.BasicAuthentication;
                    }
                    break;

                case CredentialPromptResult.OAuthAuthentication:
                    return CredentialPromptResult.OAuthAuthentication;

                case CredentialPromptResult.PersonalAccessToken:
                    if (valid)
                    {
                        token = viewModel.Token.ToUnsecureString();
                        return CredentialPromptResult.PersonalAccessToken;
                    }
                    break;
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
        PersonalAccessToken,
        Cancel,
    }
}
