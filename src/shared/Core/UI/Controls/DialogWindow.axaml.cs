using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.Controls
{
    public partial class DialogWindow : Window
    {
        private readonly Control _view;

        public DialogWindow() : this(null)
        {
            // Constructor the XAML designer
        }

        public DialogWindow(Control view)
        {
            InitializeComponent();
            _view = view;
            _contentHolder.Content = _view;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            if (DataContext is WindowViewModel vm)
            {
                vm.Accepted += (s, _) => Close(true);
                vm.Canceled += (s, _) => Close(false);

                // Send a focus request to the child view on idle
                if (_view is IFocusable focusable)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => focusable.SetFocus(), DispatcherPriority.Normal);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is WindowViewModel vm)
            {
                vm.Cancel();
            }
        }

        private void Window_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
#if DEBUG
            if (e.Key == Key.D && e.KeyModifiers == KeyModifiers.Alt &&
                DataContext is WindowViewModel vm)
            {
                // Toggle debug controls
                vm.ShowDebugControls = !vm.ShowDebugControls;
            }
#endif
        }
    }
}
