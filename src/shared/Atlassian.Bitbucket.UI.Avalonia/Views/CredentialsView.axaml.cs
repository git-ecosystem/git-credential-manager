using Atlassian.Bitbucket.UI.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager.UI.Controls;

namespace Atlassian.Bitbucket.UI.Views
{
    public class CredentialsView : UserControl, IFocusable
    {
        private TabControl _tabControl;
        private Button _oauthLoginButton;
        private TextBox _userNameTextBox;
        private TextBox _passwordTextBox;

        public CredentialsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _tabControl = this.FindControl<TabControl>("authModesTabControl");
            _oauthLoginButton = this.FindControl<Button>("oauthLoginButton");
            _userNameTextBox = this.FindControl<TextBox>("userNameTextBox");
            _passwordTextBox = this.FindControl<TextBox>("passwordTextBox");
        }

        public void SetFocus()
        {
            if (!(DataContext is CredentialsViewModel vm))
            {
                return;
            }

            if (vm.ShowOAuth)
            {
                _tabControl.SelectedIndex = 0;
                _oauthLoginButton.Focus();
            }
            else if (vm.ShowBasic)
            {
                _tabControl.SelectedIndex = 1;
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
}
