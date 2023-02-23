using System.Windows;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Interop.MacOS;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.Controls;

namespace GitCredentialManager.UI.Controls
{
    public partial class TesterWindow : Window
    {
        private readonly IEnvironment _environment;

        public TesterWindow()
        {
            InitializeComponent();

            if (PlatformUtils.IsWindows())
            {
                _environment = new WindowsEnvironment(new WindowsFileSystem());
            }
            else
            {
                IFileSystem fs;
                if (PlatformUtils.IsMacOS())
                {
                    fs = new MacOSFileSystem();
                }
                else
                {
                    fs = new LinuxFileSystem();
                }

                _environment = new PosixEnvironment(fs);
            }
        }

        private void ShowBasic(object sender, RoutedEventArgs e)
        {
            var vm = new CredentialsViewModel
            {
                Title = title.Text,
                Description = description.Text,
                UserName = username.Text,
                ShowProductHeader = showLogo.IsChecked ?? false
            };
            var view = new CredentialsView();
            var window = new DialogWindow(view) {DataContext = vm};
            window.ShowDialog();
        }

        private void ShowOAuth(object sender, RoutedEventArgs e)
        {
            var vm = new OAuthViewModel
            {
                Title = oauthTitle.Text,
                Description = oauthDescription.Text,
                ShowBrowserLogin = oauthBrowser.IsChecked ?? false,
                ShowDeviceCodeLogin = oauthDeviceCode.IsChecked ?? false,
                ShowProductHeader = oauthShowLogo.IsChecked ?? false
            };
            var view = new OAuthView();
            var window = new DialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }

        private void ShowDeviceCode(object sender, RoutedEventArgs e)
        {
            var vm = new DeviceCodeViewModel(_environment)
            {
                Title = deviceTitle.Text,
                UserCode = deviceUserCode.Text,
                VerificationUrl = deviceVerificationUrl.Text,
                ShowProductHeader = deviceShowLogo.IsChecked ?? false
            };
            var view = new DeviceCodeView();
            var window = new DialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }
    }
}
