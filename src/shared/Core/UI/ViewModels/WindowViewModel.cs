using System;

namespace GitCredentialManager.UI.ViewModels
{
    public class WindowViewModel : ViewModel
    {
        private bool _extendClientArea;
        private bool _showCustomChromeOverride;
        private bool _showDebugControls;
        private string _title;

        public event EventHandler Accepted;
        public event EventHandler Canceled;

        public WindowViewModel()
        {
            Title = Constants.DefaultWindowTitle;
            
            // Extend the client area on Windows and macOS only
            ExtendClientArea = PlatformUtils.IsMacOS() || PlatformUtils.IsWindows();
        }

        public bool WindowResult { get; private set; }

        public bool ShowDebugControls
        {
            get => _showDebugControls;
            set => SetAndRaisePropertyChanged(ref _showDebugControls, value);
        }

        public bool ShowCustomChrome
        {
            // On macOS we typically do NOT want to show the custom chrome if we've extended the client area
            // because the native 'traffic light' controls will still be visible and we don't want to show our own.
            get => ShowCustomChromeOverride || (ExtendClientArea && !PlatformUtils.IsMacOS());
        }

        public bool ShowCustomWindowBorder
        {
            // Draw the window border explicitly on Windows
            get => ShowCustomChrome && PlatformUtils.IsWindows();
        }

        public bool ShowCustomChromeOverride
        {
            get => _showCustomChromeOverride;
            set
            {
                SetAndRaisePropertyChanged(ref _showCustomChromeOverride, value);
                RaisePropertyChanged(nameof(ShowCustomChrome));
                RaisePropertyChanged(nameof(ShowCustomWindowBorder));
            }
        }

        public bool ExtendClientArea
        {
            get => _extendClientArea;
            set
            {
                SetAndRaisePropertyChanged(ref _extendClientArea, value);
                RaisePropertyChanged(nameof(ShowCustomChrome));
                RaisePropertyChanged(nameof(ShowCustomWindowBorder));
            }
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
