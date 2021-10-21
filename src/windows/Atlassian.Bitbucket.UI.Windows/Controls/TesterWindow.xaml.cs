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
            var vm = new CredentialsViewModel(_environment);
            var view = new CredentialsView();
            var window = new DialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }

        private void ShowOAuth(object sender, RoutedEventArgs e)
        {
            var vm = new OAuthViewModel(_environment);
            var view = new OAuthView();
            var window = new DialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }
    }
}
