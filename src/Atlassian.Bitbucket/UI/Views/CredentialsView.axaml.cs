using Atlassian.Bitbucket.UI.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager;
using GitCredentialManager.UI.Controls;

namespace Atlassian.Bitbucket.UI.Views
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

            if (vm.ShowOAuth)
            {
                _authModesTabControl.SelectedIndex = 0;
                _oauthLoginButton.Focus();
            }
            else if (vm.ShowBasic)
            {
                _authModesTabControl.SelectedIndex = 1;
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
