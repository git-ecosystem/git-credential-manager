using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager;
using GitHub.UI.Controls;
using GitCredentialManager.UI.Controls;

namespace GitHub.UI.Views
{
    public partial class TwoFactorView : UserControl, IFocusable
    {
        public TwoFactorView()
        {
            InitializeComponent();
        }

        public void SetFocus()
        {
            // Workaround: https://github.com/git-ecosystem/git-credential-manager/issues/1293
            if (!PlatformUtils.IsMacOS())
                _codeInput.SetFocus();
        }
    }
}
