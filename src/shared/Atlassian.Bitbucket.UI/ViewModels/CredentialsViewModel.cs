using System;
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

        private Uri _url;
        private string _userName;
        private string _password;
        private bool _showOAuth;
        private bool _showBasic;

        public CredentialsViewModel()
        {
            // Constructor the XAML designer
        }

        public CredentialsViewModel(IEnvironment environment)
        {
            EnsureArgument.NotNull(environment, nameof(environment));

            _environment = environment;

            Title = "Connect to Bitbucket";
            LoginCommand = new RelayCommand(AcceptBasic, CanLogin);
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

        private void AcceptBasic()
        {
            SelectedMode = AuthenticationModes.Basic;
            Accept();
        }

        private void AcceptOAuth()
        {
            SelectedMode = AuthenticationModes.OAuth;
            Accept();
        }

        private bool CanAcceptOAuth()
        {
            return ShowOAuth;
        }

        private void ForgotPassword()
        {
            Uri passwordResetUri = _url is null
                ? new Uri(BitbucketConstants.HelpUrls.PasswordReset)
                : new Uri(_url, BitbucketConstants.HelpUrls.DataCenterPasswordReset);

            BrowserUtils.OpenDefaultBrowser(_environment, passwordResetUri);
        }

        private void SignUp()
        {
            Uri signUpUri = _url is null
                ? new Uri(BitbucketConstants.HelpUrls.SignUp)
                : new Uri(_url, BitbucketConstants.HelpUrls.DataCenterLogin);

            BrowserUtils.OpenDefaultBrowser(_environment, signUpUri);
        }

        public Uri Url
        {
            get => _url;
            set => SetAndRaisePropertyChanged(ref _url, value);
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
        /// Show the OAuth option.
        /// </summary>
        public bool ShowOAuth
        {
            get => _showOAuth;
            set => SetAndRaisePropertyChanged(ref _showOAuth, value);
        }

        /// <summary>
        /// Show the basic authentication options.
        /// </summary>
        public bool ShowBasic
        {
            get => _showBasic;
            set => SetAndRaisePropertyChanged(ref _showBasic, value);
        }

        public AuthenticationModes SelectedMode { get; private set; }

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
