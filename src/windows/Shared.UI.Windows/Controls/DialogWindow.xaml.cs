using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace Microsoft.Git.CredentialManager.UI.Controls
{
    public partial class DialogWindow : Window
    {
        private readonly UserControl _view;

        public DialogWindow(UserControl view)
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;

            _view = view;
            ContentHolder.Content = _view;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is WindowViewModel vm)
            {
                vm.Accepted += (s, _) =>
                {
                    DialogResult = true;
                    Close();
                };

                vm.Canceled += (s, _) =>
                {
                    DialogResult = false;
                    Close();
                };
            }

            if (_view is IFocusable focusable)
            {
                // Send a focus request to the child view on idle
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() => focusable.SetFocus()));
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is WindowViewModel vm)
            {
                vm.Cancel();
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}
