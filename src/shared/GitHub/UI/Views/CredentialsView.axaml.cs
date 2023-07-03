using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager;
using GitHub.UI.ViewModels;
using GitCredentialManager.UI.Controls;

namespace GitHub.UI.Views
{
    public partial class CredentialsView : UserControl, IFocusable
    {
        private TabControl _tabControl;
        private Button _browserButton;
        private Button _deviceButton;
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
            _deviceButton = this.FindControl<Button>("signInDeviceButton");
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
            else if (vm.ShowDeviceLogin)
            {
                _tabControl.SelectedIndex = 0;
                _deviceButton.Focus();
            }
            else if (vm.ShowTokenLogin)
            {
                _tabControl.SelectedIndex = 1;
                // Workaround: https://github.com/git-ecosystem/git-credential-manager/issues/1293
                if (!PlatformUtils.IsMacOS())
                    _tokenTextBox.Focus();

            }
            else if (vm.ShowBasicLogin)
            {
                _tabControl.SelectedIndex = 2;
                if (string.IsNullOrWhiteSpace(vm.UserName))
                {
                    // Workaround: https://github.com/git-ecosystem/git-credential-manager/issues/1293
                    if (!PlatformUtils.IsMacOS())
                        _userNameTextBox.Focus();
                }
                else
                {
                    // Workaround: https://github.com/git-ecosystem/git-credential-manager/issues/1293
                    if (!PlatformUtils.IsMacOS())
                        _passwordTextBox.Focus();
                }
            }
        }
    }
}
