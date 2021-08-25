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

        private void ShowCredentials(object sender, RoutedEventArgs e)
        {
            var model = new LoginCredentialsViewModel(useBasic.IsChecked ?? false, useOAuth.IsChecked ?? false, usePat.IsChecked ?? false);
            if (!string.IsNullOrWhiteSpace(enterpriseUrl.Text))
            {
                model.GitHubEnterpriseUrl = enterpriseUrl.Text;
            }

            if (!string.IsNullOrWhiteSpace(username.Text))
            {
                model.UsernameOrEmail = username.Text;
            }

            var view = new LoginCredentialsView();
            var window = new DialogWindow(model, view);
            Gui.ShowDialog(window, Handle);
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
