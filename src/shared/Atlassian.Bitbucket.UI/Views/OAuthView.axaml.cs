using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Git.CredentialManager.UI.Controls;

namespace Atlassian.Bitbucket.UI.Views
{
    public class OAuthView : UserControl, IFocusable
    {
        private Button _okButton;

        public OAuthView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _okButton = this.FindControl<Button>("okButton");
        }

        public void SetFocus()
        {
            _okButton.Focus();
        }
    }
}
