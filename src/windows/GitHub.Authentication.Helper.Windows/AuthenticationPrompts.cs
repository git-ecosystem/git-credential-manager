// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using GitHub.Authentication.Helper.ViewModels;

namespace GitHub.Authentication.Helper
{
    public class AuthenticationPrompts
    {
        public AuthenticationPrompts(IGui gui, IntPtr parentHwnd)
        {
            _gui = gui;
            _parentHwnd = parentHwnd;
        }

        public AuthenticationPrompts(IGui gui)
            : this(gui, IntPtr.Zero)
        { }

        private readonly IGui _gui;
        private readonly IntPtr _parentHwnd;

        public bool CredentialModalPrompt(out string username, out string password)
        {
            var credentialViewModel = new CredentialsViewModel();

            bool credentialValid = _gui.ShowViewModel(credentialViewModel, () => new CredentialsWindow(_parentHwnd));

            username = credentialViewModel.Login;
            password = credentialViewModel.Password;

            return credentialValid;
        }

        public bool AuthenticationCodeModalPrompt(bool isSms, out string authenticationCode)
        {
            var twoFactorViewModel = new TwoFactorViewModel(isSms);

            bool authenticationCodeValid = _gui.ShowViewModel(twoFactorViewModel, () => new TwoFactorWindow(_parentHwnd));

            authenticationCode = authenticationCodeValid
                ? twoFactorViewModel.AuthenticationCode
                : null;

            return authenticationCodeValid;
        }
    }
}
