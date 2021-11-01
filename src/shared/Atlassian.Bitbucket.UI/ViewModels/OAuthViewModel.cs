using System.Windows.Input;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;

namespace Atlassian.Bitbucket.UI.ViewModels
{
    public class OAuthViewModel : WindowViewModel
    {
        private readonly IEnvironment _environment;

        public OAuthViewModel()
        {
            // Constructor the XAML designer
        }

        public OAuthViewModel(IEnvironment environment)
        {
            EnsureArgument.NotNull(environment, nameof(environment));

            _environment = environment;

            Title = "OAuth authentication required";
            OkCommand = new RelayCommand(Accept);
            CancelCommand = new RelayCommand(Cancel);
            LearnMoreCommand = new RelayCommand(LearnMore);
            ForgotPasswordCommand = new RelayCommand(ForgotPassword);
            SignUpCommand = new RelayCommand(SignUp);
        }

        private void LearnMore()
        {
            BrowserUtils.OpenDefaultBrowser(_environment, "https://confluence.atlassian.com/bitbucket/two-step-verification-777023203.html");
        }

        private void ForgotPassword()
        {
            BrowserUtils.OpenDefaultBrowser(_environment, "https://bitbucket.org/account/password/reset/");
        }

        private void SignUp()
        {
            BrowserUtils.OpenDefaultBrowser(_environment, "https://bitbucket.org/account/signup/");
        }

        /// <summary>
        /// Provides a link to Bitbucket OAuth documentation
        /// </summary>
        public ICommand LearnMoreCommand { get; }

        /// <summary>
        /// Hyperlink to the Bitbucket forgotten password process.
        /// </summary>
        public ICommand ForgotPasswordCommand { get; }

        /// <summary>
        /// Hyperlink to the Bitbucket sign up process.
        /// </summary>
        public ICommand SignUpCommand { get; }

        /// <summary>
        /// Run the OAuth dance.
        /// </summary>
        public ICommand OkCommand { get; }

        /// <summary>
        /// Cancel the authentication attempt.
        /// </summary>
        public ICommand CancelCommand { get; }
    }
}
