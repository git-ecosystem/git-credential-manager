using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GitHub.UI.Login
{
    public partial class Login2FaView : UserControl
    {
        public Login2FaView()
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
        /// The DataContext of this view as a Login2FaView.
        /// </summary>
        public Login2FaViewModel ViewModel => DataContext as Login2FaViewModel;

        void SetFocus()
        {
            authenticationCode.SetFocus();
        }
    }
}
