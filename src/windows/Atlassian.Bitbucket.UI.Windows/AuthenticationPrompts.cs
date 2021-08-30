using Atlassian.Bitbucket.UI.Controls;
using Atlassian.Bitbucket.UI.ViewModels;
using Microsoft.Git.CredentialManager.UI;

namespace Atlassian.Bitbucket.UI
{
    public class AuthenticationPrompts
    {
        public AuthenticationPrompts(IGui gui)
        {
            _gui = gui;
        }

        private readonly IGui _gui;

        public bool ShowCredentialsPrompt(ref string username, out string password)
        {
            // If there is a user in the remote URL then populate the UI with it.
            var credentialViewModel = new CredentialsViewModel(username);

            bool credentialValid = _gui.ShowDialogWindow(credentialViewModel, () => new CredentialsControl());

            username = credentialViewModel.Login;
            password = credentialViewModel.Password.ToUnsecureString();

            return credentialValid;
        }

        public bool ShowOAuthPrompt()
        {
            var oauthViewModel = new OAuthViewModel();

            bool useOAuth = _gui.ShowDialogWindow(oauthViewModel, () => new OAuthControl());

            return useOAuth;
        }
    }
}
