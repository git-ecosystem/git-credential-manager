using System.Windows;
using GitLab.UI.ViewModels;
using GitLab.UI.Views;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.Controls;

namespace GitLab.UI.Controls
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
                ShowBrowserLogin = useBrowser.IsChecked ?? false,
                ShowTokenLogin = usePat.IsChecked ?? false,
                ShowBasicLogin = useBasic.IsChecked ?? false,
                Url = instanceUrl.Text,
                UserName = username.Text
            };
            var view = new CredentialsView();
            var window = new DialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }
    }
}
