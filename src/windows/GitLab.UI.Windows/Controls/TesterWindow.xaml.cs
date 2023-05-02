using System.Windows;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.Windows.Controls;
using GitLab.UI.ViewModels;
using GitLab.UI.Windows.Views;

namespace GitLab.UI.Windows.Controls
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
            var window = new WpfDialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }
    }
}
