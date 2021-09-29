using System.Windows.Controls;
using Atlassian.Bitbucket.UI.ViewModels;
using Microsoft.Git.CredentialManager.UI.Controls;

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

            if (string.IsNullOrWhiteSpace(vm.UserName))
            {
                userNameTextBox.Focus();
            }
            else
            {
                passwordTextBox.Focus();
            }
        }
    }
}
