using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitHub.UI.ViewModels;
using Microsoft.Git.CredentialManager.UI.Controls;

namespace GitHub.UI.Views
{
    public class CredentialsView : UserControl, IFocusable
    {
        private TabControl _tabControl;
        private Button _browserButton;
        private TextBox _tokenTextBox;
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
            _browserButton = this.FindControl<Button>("signInBrowserButton");
            _tokenTextBox = this.FindControl<TextBox>("tokenTextBox");
            _userNameTextBox = this.FindControl<TextBox>("userNameTextBox");
            _passwordTextBox = this.FindControl<TextBox>("passwordTextBox");
        }

        public void SetFocus()
        {
            if (!(DataContext is CredentialsViewModel vm))
            {
                return;
            }

            // Select the best available authentication mechanism that is visible
            // and focus on the button/text box
            if (vm.ShowBrowserLogin)
            {
                _tabControl.SelectedIndex = 0;
                _browserButton.Focus();
            }
            else if (vm.ShowTokenLogin)
            {
                _tabControl.SelectedIndex = 1;
                _tokenTextBox.Focus();

            }
            else if (vm.ShowBasicLogin)
            {
                _tabControl.SelectedIndex = 2;
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
