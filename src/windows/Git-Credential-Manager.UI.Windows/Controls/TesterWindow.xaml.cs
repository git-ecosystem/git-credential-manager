using System.Windows;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.Controls;

namespace GitCredentialManager.UI.Controls
{
    public partial class TesterWindow : Window
    {
        public TesterWindow()
        {
            InitializeComponent();
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
    }
}
