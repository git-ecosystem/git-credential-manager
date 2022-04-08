using System.ComponentModel;
using System.Windows.Input;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;

namespace Atlassian.Bitbucket.UI.ViewModels
{
    public class CredentialsViewModel : WindowViewModel
    {
        private readonly IEnvironment _environment;

        private string _userName;
        private string _password;
        private bool _showOAuth;

        public CredentialsViewModel()
        {
            // Constructor the XAML designer
        }

        public CredentialsViewModel(IEnvironment environment)
        {
            EnsureArgument.NotNull(environment, nameof(environment));

            _environment = environment;

            Title = "Connect to Bitbucket";
            LoginCommand = new RelayCommand(Accept, CanLogin);
            CancelCommand = new RelayCommand(Cancel);
            OAuthCommand = new RelayCommand(AcceptOAuth, CanAcceptOAuth);
            ForgotPasswordCommand = new RelayCommand(ForgotPassword);
            SignUpCommand = new RelayCommand(SignUp);

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(UserName):
                case nameof(Password):
                    LoginCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);
        }

        private void AcceptOAuth()
        {
            UseOAuth = true;
            Accept();
        }

        private bool CanAcceptOAuth()
        {
            return ShowOAuth;
        }

        private void ForgotPassword()
        {
            BrowserUtils.OpenDefaultBrowser(_environment, "https://bitbucket.org/account/password/reset/");
        }

        private void SignUp()
        {
            BrowserUtils.OpenDefaultBrowser(_environment, "https://bitbucket.org/account/signup/");
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

        /// <summary>
        /// Show the direct-to-OAuth button.
        /// </summary>
        public bool ShowOAuth
        {
            get => _showOAuth;
            set => SetAndRaisePropertyChanged(ref _showOAuth, value);
        }

        /// <summary>
        /// User indicated a preference to use OAuth authentication over username/password.
        /// </summary>
        public bool UseOAuth
        {
            get;
            private set;
        }

        /// <summary>
        /// Start the process to validate the username/password
        /// </summary>
        public RelayCommand LoginCommand { get; }

        /// <summary>
        /// Cancel the authentication attempt.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Use OAuth authentication instead of username/password.
        /// </summary>
        public ICommand OAuthCommand { get; }

        /// <summary>
        /// Hyperlink to the Bitbucket forgotten password process.
        /// </summary>
        public ICommand ForgotPasswordCommand { get; }

        /// <summary>
        /// Hyperlink to the Bitbucket sign up process.
        /// </summary>
        public ICommand SignUpCommand { get; }
    }
}
