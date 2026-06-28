using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager;
using GitLab.UI.ViewModels;
using GitCredentialManager.UI.Controls;

namespace GitLab.UI.Views
{
    public partial class CredentialsView : UserControl, IFocusable
    {
        public CredentialsView()
        {
            InitializeComponent();
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
                _authModesTabControl.SelectedIndex = 0;
                _signInBrowserButton.Focus();
            }
            else if (vm.ShowTokenLogin)
            {
                _authModesTabControl.SelectedIndex = 1;
                // Workaround: https://github.com/git-ecosystem/git-credential-manager/issues/1293
                if (!PlatformUtils.IsMacOS())
                    _tokenTextBox.Focus();
            }
            else if (vm.ShowBasicLogin)
            {
                _authModesTabControl.SelectedIndex = 2;
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
