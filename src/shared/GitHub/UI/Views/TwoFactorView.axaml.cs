using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager;
using GitHub.UI.Controls;
using GitCredentialManager.UI.Controls;

namespace GitHub.UI.Views
{
    public partial class TwoFactorView : UserControl, IFocusable
    {
        private SixDigitInput _codeInput;

        public TwoFactorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _codeInput = this.FindControl<SixDigitInput>("codeInput");
        }

        public void SetFocus()
        {
            // Workaround: https://github.com/git-ecosystem/git-credential-manager/issues/1293
            if (!PlatformUtils.IsMacOS())
                _codeInput.SetFocus();
        }
    }
}
