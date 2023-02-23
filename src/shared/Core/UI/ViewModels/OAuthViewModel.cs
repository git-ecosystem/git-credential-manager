using GitCredentialManager.Authentication;

namespace GitCredentialManager.UI.ViewModels
{
    public class OAuthViewModel : WindowViewModel
    {
        private string _description;
        private bool _showProductHeader = true;
        private bool _showBrowserLogin;
        private bool _showDeviceCodeLogin;
        private RelayCommand _signInBrowserCommand;
        private RelayCommand _signInDeviceCodeCommand;

        public OAuthViewModel()
        {
            SignInBrowserCommand = new RelayCommand(SignInWithBrowser);
            SignInDeviceCodeCommand = new RelayCommand(SignInWithDeviceCode);
        }

        private void SignInWithBrowser()
        {
            SelectedMode = OAuthAuthenticationModes.Browser;
            Accept();
        }

        private void SignInWithDeviceCode()
        {
            SelectedMode = OAuthAuthenticationModes.DeviceCode;
            Accept();
        }

        public string Description
        {
            get => _description;
            set => SetAndRaisePropertyChanged(ref _description, value);
        }

        public bool ShowProductHeader
        {
            get => _showProductHeader;
            set => SetAndRaisePropertyChanged(ref _showProductHeader, value);
        }

        public bool ShowBrowserLogin
        {
            get => _showBrowserLogin;
            set => SetAndRaisePropertyChanged(ref _showBrowserLogin, value);
        }

        public bool ShowDeviceCodeLogin
        {
            get => _showDeviceCodeLogin;
            set => SetAndRaisePropertyChanged(ref _showDeviceCodeLogin, value);
        }

        public RelayCommand SignInBrowserCommand
        {
            get => _signInBrowserCommand;
            set => SetAndRaisePropertyChanged(ref _signInBrowserCommand, value);
        }

        public RelayCommand SignInDeviceCodeCommand
        {
            get => _signInDeviceCodeCommand;
            set => SetAndRaisePropertyChanged(ref _signInDeviceCodeCommand, value);
        }

        public OAuthAuthenticationModes SelectedMode { get; private set; }
    }
}
