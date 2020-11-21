using System.ComponentModel;
using System.Security;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace GitHub.UI.Login
{
    public class LoginCredentialsViewModel : WindowViewModel
    {
        public LoginCredentialsViewModel(string gitHubEnterpriseUrl, string usernameOrEmail, bool enablePasswordAuth, bool enablePatAuth, bool enableBrowserAuth)
        {
            GitHubEnterpriseUrl = gitHubEnterpriseUrl;
            UsernameOrEmail = usernameOrEmail;

            IsLoginUsingUsernameAndPasswordVisible = enablePasswordAuth | enablePatAuth;
            IsLoginUsingBrowserVisible = enableBrowserAuth;

            LoginUsingUsernameAndPasswordCommand = new RelayCommand(LoginUsingUsernameAndPasswordOrPat, CanLoginUsingUsernameAndPasswordOrPat);
            LoginUsingBrowserCommand = new RelayCommand(LoginUsingBrowser);

            PropertyChanged += LoginCredentialsViewModel_PropertyChanged;

            PasswordOrPatString = enablePasswordAuth && enablePatAuth
                ? "Password/PAT"
                : enablePasswordAuth
                    ? "Password"
                    : enablePatAuth
                        ? "PAT"
                        : null;
        }

        private void LoginCredentialsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(UsernameOrEmail):
                case nameof(Password):
                    LoginUsingUsernameAndPasswordCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        public override bool IsValid => IsLoginUsingBrowserVisible || CanLoginUsingUsernameAndPasswordOrPat();

        public override string Title => GitHubResources.LoginTitle;

        /// <summary>
        /// The command that will be invoked when the user attempts to login with a username and password/PAT combination.
        /// </summary>
        public RelayCommand LoginUsingUsernameAndPasswordCommand { get; }

        /// <summary>
        /// The command that will be invoked when the user clicks on the "Sign in with your browser" link
        /// </summary>
        public RelayCommand LoginUsingBrowserCommand { get; }

        /// <summary>
        /// The URL of the GitHub Enterprise instance if this is a GHE authentication dialog.
        /// </summary>
        public string GitHubEnterpriseUrl { get; }

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

        public bool IsLoginUsingUsernameAndPasswordVisible { get; }

        public bool IsLoginUsingBrowserVisible { get; }

        public string ErrorMessage { get; }

        public string PasswordOrPatString { get; }

        public bool HasUsedBrowserLogin { get; private set; }

        /// <summary>
        /// Should the user be allowed to attempt to login with a username and password combination?
        /// </summary>
        private bool CanLoginUsingUsernameAndPasswordOrPat()
        {
            return !string.IsNullOrEmpty(UsernameOrEmail) && password != null && password.Length > 0;
        }

        private void LoginUsingUsernameAndPasswordOrPat()
        {
            HasUsedBrowserLogin = false;
            Accept();
        }

        private void LoginUsingBrowser()
        {
            HasUsedBrowserLogin = true;
            Accept();
        }
    }
}
