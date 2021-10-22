using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace GitCredentialManager.UI.Controls
{
    public class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo("https://aka.ms/gcmcore")
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}
