using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Interop.MacOS;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;

namespace GitCredentialManager.UI.Controls
{
    public class TesterWindow : Window
    {
        private readonly IEnvironment _environment;

        public TesterWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

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

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ShowBasic(object sender, RoutedEventArgs e)
        {
            var vm = new CredentialsViewModel
            {
                Title = this.FindControl<TextBox>("title").Text,
                Description = this.FindControl<TextBox>("description").Text,
                UserName = this.FindControl<TextBox>("username").Text,
                ShowProductHeader = this.FindControl<CheckBox>("showLogo").IsChecked ?? false
            };
            var view = new CredentialsView();
            var window = new DialogWindow(view) {DataContext = vm};
            window.ShowDialog(this);
        }

        private void ShowOAuth(object sender, RoutedEventArgs e)
        {
            var vm = new OAuthViewModel
            {
                Title = this.FindControl<TextBox>("oauthTitle").Text,
                Description = this.FindControl<TextBox>("oauthDescription").Text,
                ShowBrowserLogin = this.FindControl<CheckBox>("oauthBrowser").IsChecked ?? false,
                ShowDeviceCodeLogin = this.FindControl<CheckBox>("oauthDeviceCode").IsChecked ?? false,
                ShowProductHeader = this.FindControl<CheckBox>("oauthShowLogo").IsChecked ?? false
            };
            var view = new OAuthView();
            var window = new DialogWindow(view) {DataContext = vm};
            window.ShowDialog(this);
        }

        private void ShowDeviceCode(object sender, RoutedEventArgs e)
        {
            var vm = new DeviceCodeViewModel(_environment)
            {
                Title = this.FindControl<TextBox>("deviceTitle").Text,
                UserCode = this.FindControl<TextBox>("deviceUserCode").Text,
                VerificationUrl = this.FindControl<TextBox>("deviceVerificationUrl").Text,
                ShowProductHeader = this.FindControl<CheckBox>("deviceShowLogo").IsChecked ?? false
            };
            var view = new DeviceCodeView();
            var window = new DialogWindow(view) {DataContext = vm};
            window.ShowDialog(this);
        }
    }
}
