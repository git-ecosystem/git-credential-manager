using System;
using System.Windows;
using Atlassian.Bitbucket.UI.ViewModels;
using Atlassian.Bitbucket.UI.Views;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.Controls;

namespace Atlassian.Bitbucket.UI.Controls
{
    public partial class TesterWindow : Window
    {
        private readonly WindowsEnvironment _environment = new WindowsEnvironment(new WindowsFileSystem());

        public TesterWindow()
        {
            InitializeComponent();
        }

        private void ShowCredentials(object sender, RoutedEventArgs e)
        {
            var vm = new CredentialsViewModel(_environment)
            {
                ShowOAuth = showOAuth.IsChecked ?? false,
                ShowBasic = showBasic.IsChecked ?? false,
                UserName = username.Text
            };

            if (Uri.TryCreate(url.Text, UriKind.Absolute, out Uri uri))
            {
                vm.Url = uri;
            }

            var view = new CredentialsView();
            var window = new DialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }
    }
}
