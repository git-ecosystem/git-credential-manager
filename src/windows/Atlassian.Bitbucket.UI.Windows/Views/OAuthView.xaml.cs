using System.Windows.Controls;
using Microsoft.Git.CredentialManager.UI.Controls;

namespace Atlassian.Bitbucket.UI.Views
{
    public partial class OAuthView : UserControl, IFocusable
    {
        public OAuthView()
        {
            InitializeComponent();
        }

        public void SetFocus()
        {
            okButton.Focus();
        }
    }
}
