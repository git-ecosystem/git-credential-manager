using System;
using System.Windows.Controls;
using System.Windows.Threading;
using GitCredentialManager.UI.Controls;

namespace GitHub.UI.Views
{
    public partial class TwoFactorView : UserControl, IFocusable
    {
        public TwoFactorView()
        {
            InitializeComponent();

            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)SetFocus);
                }
            };
        }

        public void SetFocus()
        {
            codeInput.SetFocus();
        }
    }
}
