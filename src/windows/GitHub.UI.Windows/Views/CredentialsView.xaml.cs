using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GitHub.UI.ViewModels;
using Microsoft.Git.CredentialManager.UI.Controls;

namespace GitHub.UI.Views
{
    public partial class CredentialsView : UserControl, IFocusable
    {
        public CredentialsView()
        {
            InitializeComponent();
        }

        // Set focus on a UIElement the next time it becomes visible
        private static void OnIsVisibleChangedOneTime(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element)
            {
                // Unsubscribe to prevent re-triggering
                element.IsVisibleChanged -= OnIsVisibleChangedOneTime;

                // Set logical focus
                element.Focus();

                // Set keyboard focus
                Keyboard.Focus(element);
            }
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
            // In WPF the controls in a TabItem are not part of the visual tree until
            // the TabControl has been switched to that tab, so we must delay focusing
            // on the textbox/button until it becomes visible.
            //
            // This means as the user first moves through the tabs, the "correct" control
            // will be given focus in that tab.
            //
            void SetFocusOnNextVisible(UIElement element)
            {
                element.IsVisibleChanged += OnIsVisibleChangedOneTime;
            }

            // Set up focus events on all controls
            SetFocusOnNextVisible(
                string.IsNullOrWhiteSpace(vm.UserName)
                    ? userNameTextBox
                    : passwordTextBox);
            SetFocusOnNextVisible(tokenTextBox);
            SetFocusOnNextVisible(browserButton);

            // Switch to the preferred tab
            if (vm.ShowBrowserLogin)
            {
                tabControl.SelectedIndex = 0;
            }
            else if (vm.ShowTokenLogin)
            {
                tabControl.SelectedIndex = 1;
            }
            else if (vm.ShowBasicLogin)
            {
                tabControl.SelectedIndex = 2;
            }
        }
    }
}
