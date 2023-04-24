using System.ComponentModel;
using System.Windows.Input;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;

namespace GitHub.UI.ViewModels
{
    public class CredentialsViewModel : WindowViewModel
    {
        private readonly IEnvironment _environment;
        private readonly IProcessManager _processManager;

        private string _enterpriseUrl;
        private string _token;
        private string _userName;
        private string _password;
        private bool _showBrowserLogin;
        private bool _showDeviceLogin;
        private bool _showTokenLogin;
        private bool _showBasicLogin;
        private ICommand _signUpCommand;
        private ICommand _signInBrowserCommand;
        private ICommand _signInDeviceCommand;
        private RelayCommand _signInBasicCommand;
        private RelayCommand _signInTokenCommand;

        public CredentialsViewModel()
        {
            // Constructor the XAML designer
        }

        public CredentialsViewModel(IEnvironment environment, IProcessManager processManager)
        {
            EnsureArgument.NotNull(environment, nameof(environment));
            EnsureArgument.NotNull(processManager, nameof(processManager));

            _environment = environment;
            _processManager = processManager;

            Title = "Connect to GitHub";
            SignUpCommand = new RelayCommand(SignUp);
            SignInBrowserCommand = new RelayCommand(SignInBrowser);
            SignInDeviceCommand = new RelayCommand(SignInDevice);
            SignInTokenCommand = new RelayCommand(SignInToken, CanSignInToken);
            SignInBasicCommand = new RelayCommand(SignInBasic, CanSignInBasic);

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(UserName):
                case nameof(Password):
                    SignInBasicCommand.RaiseCanExecuteChanged();
                    break;

                case nameof(Token):
                    SignInTokenCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        private void SignUp()
        {
            BrowserUtils.OpenDefaultBrowser(_environment, "https://github.com/pricing");
        }

        private void SignInBrowser()
        {
            SelectedMode = AuthenticationModes.Browser;
            Accept();
        }

        private void SignInDevice()
        {
            SelectedMode = AuthenticationModes.Device;
            Accept();
        }

        private bool CanSignInToken()
        {
            return !string.IsNullOrWhiteSpace(Token);
        }

        private void SignInToken()
        {
            SelectedMode = AuthenticationModes.Pat;
            Accept();
        }

        private bool CanSignInBasic()
        {
            return !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrEmpty(Password);
        }

        private void SignInBasic()
        {
            SelectedMode = AuthenticationModes.Basic;
            Accept();
        }

        public string Token
        {
            get => _token;
            set => SetAndRaisePropertyChanged(ref _token, value);
        }

        public string UserName
        {
            get => _userName;
            set => SetAndRaisePropertyChanged(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => SetAndRaisePropertyChanged(ref _password, value);
        }

        public string EnterpriseUrl
        {
            get => _enterpriseUrl;
            set => SetAndRaisePropertyChanged(ref _enterpriseUrl, value);
        }

        public string OAuthModeTitle
        {
            get
            {
                if (ShowBrowserLogin && ShowDeviceLogin)
                    return "Browser/Device";
                if (ShowBrowserLogin) return "Browser";
                if (ShowDeviceLogin)  return "Device";
                return "OAuth";
            }
        }

        public bool ShowBrowserLogin
        {
            get => _showBrowserLogin;
            set
            {
                SetAndRaisePropertyChanged(ref _showBrowserLogin, value);
                RaisePropertyChanged(OAuthModeTitle);
            }
        }

        public bool ShowDeviceLogin
        {
            get => _showDeviceLogin;
            set
            {
                SetAndRaisePropertyChanged(ref _showDeviceLogin, value);
                RaisePropertyChanged(OAuthModeTitle);
            }
        }

        public bool ShowTokenLogin
        {
            get => _showTokenLogin;
            set => SetAndRaisePropertyChanged(ref _showTokenLogin, value);
        }

        public bool ShowBasicLogin
        {
            get => _showBasicLogin;
            set => SetAndRaisePropertyChanged(ref _showBasicLogin, value);
        }

        public ICommand SignUpCommand
        {
            get => _signUpCommand;
            set => SetAndRaisePropertyChanged(ref _signUpCommand, value);
        }

        public ICommand SignInBrowserCommand
        {
            get => _signInBrowserCommand;
            set => SetAndRaisePropertyChanged(ref _signInBrowserCommand, value);
        }

        public ICommand SignInDeviceCommand
        {
            get => _signInDeviceCommand;
            set => SetAndRaisePropertyChanged(ref _signInDeviceCommand, value);
        }

        public RelayCommand SignInTokenCommand
        {
            get => _signInTokenCommand;
            set => SetAndRaisePropertyChanged(ref _signInTokenCommand, value);
        }

        public RelayCommand SignInBasicCommand
        {
            get => _signInBasicCommand;
            set => SetAndRaisePropertyChanged(ref _signInBasicCommand, value);
        }

        public AuthenticationModes SelectedMode { get; private set; }
    }
}
