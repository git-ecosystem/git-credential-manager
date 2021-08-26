using System;
using System.Windows;
using System.Windows.Interop;
using Atlassian.Bitbucket.UI.Controls;
using Atlassian.Bitbucket.UI.ViewModels;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.Controls;

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
            Gui.ShowDialog(new DialogWindow(new CredentialsViewModel(), new CredentialsControl()), Handle);
        }

        private void ShowOAuth(object sender, RoutedEventArgs e)
        {
            Gui.ShowDialog(new DialogWindow(new OAuthViewModel(), new OAuthControl()), Handle);
        }
    }
}
