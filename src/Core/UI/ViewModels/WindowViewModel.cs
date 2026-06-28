using System;
using Avalonia.Controls;

namespace GitCredentialManager.UI.ViewModels
{
    public class WindowViewModel : ViewModel
    {
        private bool _extendClientArea;
        private WindowDecorations _windowDecorations;
        private bool _showDebugControls;
        private string _title;

        public static readonly WindowDecorations[] WindowDecorationsValues = Enum.GetValues<WindowDecorations>();

        public event EventHandler Accepted;
        public event EventHandler Canceled;

        public WindowViewModel()
        {
            Title = Constants.DefaultWindowTitle;
            
            // Extend the client area on Windows and macOS only
            ExtendClientArea = PlatformUtils.IsMacOS() || PlatformUtils.IsWindows();

            // On Windows we prefer to show our own title bar, but we want the system
            // to continue to draw the window border for us, which includes rounded
            // window corners and shadow.
            WindowDecorations = PlatformUtils.IsWindows()
                ? WindowDecorations.BorderOnly
                : WindowDecorations.Full;
        }

        public bool WindowResult { get; private set; }

        public bool ShowDebugControls
        {
            get => _showDebugControls;
            set => SetAndRaisePropertyChanged(ref _showDebugControls, value);
        }

        public WindowDecorations WindowDecorations
        {
            get => _windowDecorations;
            set
            {
                SetAndRaisePropertyChanged(ref _windowDecorations, value);
                RaisePropertyChanged(nameof(ShowCustomTitleBar));
            }
        }

        // Without system window decorations there's now way to close the window,
        // so we need to draw our own title bar and close button.
        public bool ShowCustomTitleBar => WindowDecorations != WindowDecorations.Full;

        public bool ExtendClientArea
        {
            get => _extendClientArea;
            set => SetAndRaisePropertyChanged(ref _extendClientArea, value);
        }

        public string Title
        {
            get => _title;
            set => SetAndRaisePropertyChanged(ref _title, value);
        }

        public void Accept()
        {
            WindowResult = true;
            Accepted?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            WindowResult = false;
            Canceled?.Invoke(this, EventArgs.Empty);
        }
    }
}
