using System.Windows.Controls;
using GitCredentialManager.UI.Controls;

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
