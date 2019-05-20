// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Windows;

namespace GitHub.Authentication.Helper
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

        private void ShowCredentials(object sender, RoutedEventArgs e)
        {
            new CredentialsWindow(IntPtr.Zero).ShowDialog();
        }

        private void ShowAuthenticationCode(object sender, RoutedEventArgs e)
        {
            new TwoFactorWindow(IntPtr.Zero).ShowDialog();
        }
    }
}
