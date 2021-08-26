using Atlassian.Bitbucket.UI.ViewModels;
using Atlassian.Bitbucket.UI.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Interop.Linux;
using Microsoft.Git.CredentialManager.Interop.MacOS;
using Microsoft.Git.CredentialManager.Interop.Posix;
using Microsoft.Git.CredentialManager.Interop.Windows;
using Microsoft.Git.CredentialManager.UI.Controls;

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
            var vm = new CredentialsViewModel(_environment);
            var view = new CredentialsView();
            var window = new DialogWindow(view) {DataContext = vm};
            window.ShowDialog(this);
        }

        private void ShowOAuth(object sender, RoutedEventArgs e)
        {
            var vm = new OAuthViewModel(_environment);
            var view = new OAuthView();
            var window = new DialogWindow(view) {DataContext = vm};
            window.ShowDialog(this);
        }
    }
}
