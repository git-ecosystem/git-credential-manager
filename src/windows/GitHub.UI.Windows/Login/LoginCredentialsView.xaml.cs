using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GitHub.UI.Login
{
    public partial class LoginCredentialsView : UserControl
    {
        public LoginCredentialsView()
        {
            InitializeComponent();

            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() => SetFocus()));
                }
            };
        }

        /// <summary>
        /// The DataContext of this view as a LoginCredentialsViewModel.
        /// </summary>
        public LoginCredentialsViewModel ViewModel => DataContext as LoginCredentialsViewModel;

        void SetFocus()
        {
            if (ViewModel is null)
            {
                return;
            }

            if (ViewModel.IsLoginUsingUsernameAndPasswordVisible)
            {
                if (string.IsNullOrWhiteSpace(ViewModel.UsernameOrEmail))
                {
                    loginBox.Focus();
                }
                else
                {
                    passwordBox.Focus();
                }
            }
            else if (ViewModel.IsLoginUsingBrowserVisible)
            {
                loginLink.Focus();
            }
        }
    }
}
