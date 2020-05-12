// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Git.CredentialManager.UI;

namespace Atlassian.Bitbucket.UI
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
            // TODO
            var window = new Window();
            Gui.ShowDialog(window, Handle);
        }

        private void ShowOAuth(object sender, RoutedEventArgs e)
        {
            // TODO
            var window = new Window();
            Gui.ShowDialog(window, Handle);
        }
    }
}
