using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager.UI.Controls;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.Views
{
    public class CredentialsView : UserControl, IFocusable
    {
        private TextBox _userNameTextBox;
        private TextBox _passwordTextBox;

        public CredentialsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _userNameTextBox = this.FindControl<TextBox>("userNameTextBox");
            _passwordTextBox = this.FindControl<TextBox>("passwordTextBox");
        }

        public void SetFocus()
        {
            if (!(DataContext is CredentialsViewModel vm))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(vm.UserName))
            {
                _userNameTextBox.Focus();
            }
            else
            {
                _passwordTextBox.Focus();
            }
        }
    }
}
