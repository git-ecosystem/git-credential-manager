// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Windows;
using System.Windows.Interop;
using GitHub.UI.Dialog;
using GitHub.UI.Login;

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

        private void ShowCredentials(object sender, RoutedEventArgs e)
        {
            new GitHubDialogWindow(new LoginCredentialsViewModel(true, true), new LoginCredentialsView()).ShowDialog(Handle);
        }

        private void ShowAuthenticationCode(object sender, RoutedEventArgs e)
        {
            new GitHubDialogWindow(new Login2FaViewModel(TwoFactorType.AuthenticatorApp), new Login2FaView()).ShowDialog(Handle);
        }
    }
}
