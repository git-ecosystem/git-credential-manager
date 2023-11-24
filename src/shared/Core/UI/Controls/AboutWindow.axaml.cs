using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace GitCredentialManager.UI.Controls
{
    public partial class AboutWindow : Window
    {
        public string VersionString => $"Version {Constants.GcmVersion}";
        public string ProjectUrl => Constants.HelpUrls.GcmProjectUrl;

        public AboutWindow()
        {
            InitializeComponent();
        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo(ProjectUrl)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}
