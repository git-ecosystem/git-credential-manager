using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager.UI.Controls;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.Views
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
