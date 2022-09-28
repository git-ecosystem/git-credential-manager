using System.Windows.Controls;
using System.Windows.Input;
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

            //
            // Select the best available authentication mechanism that is visible
            // and make the textbox/button focused when it next made visible.
            //
            Control element = string.IsNullOrWhiteSpace(vm.UserName)
                ? userNameTextBox
                : passwordTextBox;

            // Set logical focus
            element.Focus();

            // Set keyboard focus
            Keyboard.Focus(element);
        }
    }
}
