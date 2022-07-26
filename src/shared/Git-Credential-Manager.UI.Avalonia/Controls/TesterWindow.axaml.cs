using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;

namespace GitCredentialManager.UI.Controls
{
    public class TesterWindow : Window
    {
        public TesterWindow()
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
    }
}
