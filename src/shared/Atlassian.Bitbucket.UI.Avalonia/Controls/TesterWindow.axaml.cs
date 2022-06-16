using System;
using Atlassian.Bitbucket.UI.ViewModels;
using Atlassian.Bitbucket.UI.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitCredentialManager;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Interop.MacOS;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.Controls;

namespace Atlassian.Bitbucket.UI.Controls
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

        private void ShowCredentials(object sender, RoutedEventArgs e)
        {
            var vm = new CredentialsViewModel(_environment)
            {
                ShowOAuth = this.FindControl<CheckBox>("showOAuth").IsChecked ?? false,
                ShowBasic = this.FindControl<CheckBox>("showBasic").IsChecked ?? false,
                UserName = this.FindControl<TextBox>("username").Text
            };

            if (Uri.TryCreate(this.FindControl<TextBox>("url").Text, UriKind.Absolute, out Uri uri))
            {
                vm.Url = uri;
            }

            var view = new CredentialsView();
            var window = new DialogWindow(view) {DataContext = vm};
            window.ShowDialog(this);
        }
    }
}
