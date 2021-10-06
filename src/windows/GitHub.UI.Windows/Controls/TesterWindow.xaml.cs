using System.Windows;
using GitHub.UI.ViewModels;
using GitHub.UI.Views;
using Microsoft.Git.CredentialManager.Interop.Windows;
using Microsoft.Git.CredentialManager.UI.Controls;

namespace GitHub.UI.Controls
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
                EnterpriseUrl = enterpriseUrl.Text,
                UserName = username.Text
            };
            var view = new CredentialsView();
            var window = new DialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }

        private void ShowTwoFactorCode(object sender, RoutedEventArgs e)
        {
            var vm = new TwoFactorViewModel(_environment)
            {
                IsSms = twoFaSms.IsChecked ?? false,
            };
            var view = new TwoFactorView();
            var window = new DialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }
    }
}
