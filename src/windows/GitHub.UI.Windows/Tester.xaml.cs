// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Windows;
using System.Windows.Interop;
using GitHub.UI.Login;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.Controls;

namespace GitHub.UI
{
    /// <summary>
    /// Interaction logic for Tester.xaml
    /// </summary>
    public partial class Tester : Window
    {
        public Tester()
        {
            InitializeComponent();
        }

        private IntPtr Handle => new WindowInteropHelper(this).Handle;

        private void ShowLoginCredentialsView(string gitHubEnterpriseUrl, string usernameOrEmail, bool enablePasswordAuth, bool enablePatAuth, bool enableBrowserAuth)
        {
            var model = new LoginCredentialsViewModel(gitHubEnterpriseUrl, usernameOrEmail, enablePasswordAuth, enablePatAuth, enableBrowserAuth);
            var view = new LoginCredentialsView();
            var window = new DialogWindow(model, view);
            Gui.ShowDialog(window, Handle);
        }

        private void ShowCredentials_Password(object sender, RoutedEventArgs e)
        {
            ShowLoginCredentialsView("", "", true, false, false);
        }
        private void ShowCredentials_Pat(object sender, RoutedEventArgs e)
        {
            ShowLoginCredentialsView("", "", false, true, false);
        }
        private void ShowCredentials_Basic(object sender, RoutedEventArgs e)
        {
            ShowLoginCredentialsView("", "", true, true, false);
        }
        private void ShowCredentials_OAuthPassword(object sender, RoutedEventArgs e)
        {
            ShowLoginCredentialsView("", "", true, false, true);
        }
        private void ShowCredentials_OAuthPat(object sender, RoutedEventArgs e)
        {
            ShowLoginCredentialsView("", "", false, true, true);
        }
        private void ShowCredentials_OAuthBasic(object sender, RoutedEventArgs e)
        {
            ShowLoginCredentialsView("", "", true, true, true);
        }
        private void ShowCredentials_OAuth(object sender, RoutedEventArgs e)
        {
            ShowLoginCredentialsView("", "", false, false, true);
        }

        private void ShowAuthenticationCode(object sender, RoutedEventArgs e)
        {
            var model = new Login2FaViewModel(TwoFactorType.AuthenticatorApp);
            var view = new Login2FaView();
            var window = new DialogWindow(model, view);
            Gui.ShowDialog(window, Handle);
        }
    }
}
