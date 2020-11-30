using System.ComponentModel;
using System.Security;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace GitHub.UI.Login
{
    public class LoginCredentialsViewModel : WindowViewModel
    {
        public LoginCredentialsViewModel(bool showUsernameAuth, bool showBrowserAuth, bool showPatAuth)
        {
            isLoginUsingUsernameAndPasswordVisible = showUsernameAuth;
            isLoginUsingBrowserVisible = showBrowserAuth;
            isLoginUsingTokenVisible = showPatAuth;

            LoginUsingUsernameAndPasswordCommand = new RelayCommand(LoginUsingUsernameAndPassword, CanLoginUsingUsernameAndPassword);
            LoginUsingTokenCommand = new RelayCommand(LoginUsingToken, CanLoginUsingToken);
            LoginUsingBrowserCommand = new RelayCommand(LoginUsingBrowser);

            PropertyChanged += LoginCredentialsViewModel_PropertyChanged;
        }

        private void LoginCredentialsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(UsernameOrEmail):
                case nameof(Password):
                    LoginUsingUsernameAndPasswordCommand.RaiseCanExecuteChanged();
                    break;

                case nameof(Token):
                    LoginUsingTokenCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        public override bool IsValid => IsLoginUsingBrowserVisible || CanLoginUsingToken() || CanLoginUsingUsernameAndPassword();

        public override string Title => GitHubResources.LoginTitle;

        /// <summary>
        /// The command that will be invoked when the user attempts to login with a username and password combination.
        /// </summary>
        public RelayCommand LoginUsingUsernameAndPasswordCommand { get; }

        /// <summary>
        /// The command that will be invoked when the user attempts to login with an access token.
        /// </summary>
        public RelayCommand LoginUsingTokenCommand { get; }

        /// <summary>
        /// The command that will be invoked when the user clicks on the "Sign in with your browser" link
        /// </summary>
        public RelayCommand LoginUsingBrowserCommand { get; }

        /// <summary>
        /// The URL of the GitHub Enterprise instance if this is a GHE authentication dialog.
        /// </summary>
        public string GitHubEnterpriseUrl
        {
            get => gitHubEnterpriseUrl;
            set => SetAndRaisePropertyChanged(ref gitHubEnterpriseUrl, value);
        }
        private string gitHubEnterpriseUrl = null;

        /// <summary>
        /// The value that is typed into the username textbox.
        /// </summary>
        public string UsernameOrEmail
        {
            get => username;
            set => SetAndRaisePropertyChanged(ref username, value);
        }
        private string username = null;

        /// <summary>
        /// The value that is typed into the password textbox.
        /// </summary>
        public SecureString Password
        {
            get => password;
            set => SetAndRaisePropertyChanged(ref password, value);
        }
        private SecureString password = null;

        /// <summary>
        /// The value that is typed into the token textbox.
        /// </summary>
        public SecureString Token
        {
            get => token;
            set => SetAndRaisePropertyChanged(ref token, value);
        }
        private SecureString token = null;

        public bool IsLoginUsingUsernameAndPasswordVisible
        {
            get => isLoginUsingUsernameAndPasswordVisible;
            set => SetAndRaisePropertyChanged(ref isLoginUsingUsernameAndPasswordVisible, value);
        }
        private bool isLoginUsingUsernameAndPasswordVisible;

        public bool IsLoginUsingBrowserVisible
        {
            get => isLoginUsingBrowserVisible;
            set => SetAndRaisePropertyChanged(ref isLoginUsingBrowserVisible, value);
        }
        private bool isLoginUsingBrowserVisible;

        public bool IsLoginUsingTokenVisible
        {
            get => isLoginUsingTokenVisible;
            set => SetAndRaisePropertyChanged(ref isLoginUsingTokenVisible, value);
        }
        private bool isLoginUsingTokenVisible;
        
        public bool IsBrowserSeparatorVisible
        {
            get => IsLoginUsingBrowserVisible && (IsLoginUsingUsernameAndPasswordVisible || IsLoginUsingTokenVisible);
        }
        
        public bool IsTokenSeparatorVisible
        {
            get => IsLoginUsingUsernameAndPasswordVisible && IsLoginUsingTokenVisible;
        }

        public string ErrorMessage
        {
            get => errorMessage;
            private set => SetAndRaisePropertyChanged(ref errorMessage, value);
        }
        private string errorMessage;

        public CredentialPromptResult SelectedAuthType { get; private set; }

        /// <summary>
        /// Should the user be allowed to attempt to login with a username and password combination?
        /// </summary>
        private bool CanLoginUsingUsernameAndPassword()
        {
            return !string.IsNullOrEmpty(UsernameOrEmail) && password != null && password.Length > 0;
        }

        private bool CanLoginUsingToken()
        {
            return token != null && token.Length > 0;
        }

        private void LoginUsingUsernameAndPassword()
        {
            SelectedAuthType = CredentialPromptResult.BasicAuthentication;
            Accept();
        }

        private void LoginUsingToken()
        {
            SelectedAuthType = CredentialPromptResult.PersonalAccessToken;
            Accept();
        }

        private void LoginUsingBrowser()
        {
            SelectedAuthType = CredentialPromptResult.OAuthAuthentication;
            Accept();
        }
    }
}
