using System;
using Avalonia.Platform;

namespace Microsoft.Git.CredentialManager.UI.ViewModels
{
    public class WindowViewModel : ViewModel
    {
        private bool _extendClientArea;
        private bool _showClientChromeOverride;
        private bool _showDebugControls;
        private string _title;

        public event EventHandler Accepted;
        public event EventHandler Canceled;

        public WindowViewModel()
        {
            // Default to hiding the system chrome on macOS only for now
            ExtendClientArea = PlatformUtils.IsMacOS();
        }

        public bool WindowResult { get; private set; }

        public bool ShowDebugControls
        {
            get => _showDebugControls;
            set => SetAndRaisePropertyChanged(ref _showDebugControls, value);
        }

        public bool ShowCustomChrome
        {
            get => ShowClientChromeOverride || (ExtendClientArea && !PlatformUtils.IsMacOS());
        }

        public ExtendClientAreaChromeHints ChromeHints
        {
            get => ShowCustomChrome
                ? ExtendClientAreaChromeHints.NoChrome
                : ExtendClientAreaChromeHints.PreferSystemChrome;
        }

        public bool ShowClientChromeOverride
        {
            get => _showClientChromeOverride;
            set
            {
                SetAndRaisePropertyChanged(ref _showClientChromeOverride, value);
                RaisePropertyChanged(nameof(ShowCustomChrome));
                RaisePropertyChanged(nameof(ChromeHints));
            }
        }

        public bool ExtendClientArea
        {
            get => _extendClientArea;
            set
            {
                SetAndRaisePropertyChanged(ref _extendClientArea, value);
                RaisePropertyChanged(nameof(ShowCustomChrome));
                RaisePropertyChanged(nameof(ChromeHints));
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
