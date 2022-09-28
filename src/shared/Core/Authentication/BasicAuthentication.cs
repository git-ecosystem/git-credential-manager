using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitCredentialManager.Authentication
{
    public interface IBasicAuthentication
    {
        Task<ICredential> GetCredentialsAsync(string resource, string userName);
    }

    public static class BasicAuthenticationExtensions
    {
        public static Task<ICredential> GetCredentialsAsync(this IBasicAuthentication basicAuth, string resource)
        {
            return basicAuth.GetCredentialsAsync(resource, null);
        }
    }

    public class BasicAuthentication : AuthenticationBase, IBasicAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "basic",
        };

        public BasicAuthentication(ICommandContext context)
            : base (context) { }

        public async Task<ICredential> GetCredentialsAsync(string resource, string userName)
        {
            EnsureArgument.NotNullOrWhiteSpace(resource, nameof(resource));

            ThrowIfUserInteractionDisabled();

            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession &&
                TryFindHelperExecutablePath(out string helperPath))
            {
                return await GetCredentialsByUiAsync(helperPath, resource, userName);
            }

            ThrowIfTerminalPromptsDisabled();

            return GetCredentialsByTty(resource, userName);
        }

        private ICredential GetCredentialsByTty(string resource, string userName)
        {
            Context.Terminal.WriteLine("Enter basic credentials for '{0}':", resource);

            if (!string.IsNullOrWhiteSpace(userName))
            {
                // Don't need to prompt for the username if it has been specified already
                Context.Terminal.WriteLine("Username: {0}", userName);
            }
            else
            {
                // Prompt for username
                userName = Context.Terminal.Prompt("Username");
            }

            // Prompt for password
            string password = Context.Terminal.PromptSecret("Password");

            return new GitCredential(userName, password);
        }

        private async Task<ICredential> GetCredentialsByUiAsync(string helperPath, string resource, string userName)
        {
            var promptArgs = new StringBuilder("basic");

            if (!string.IsNullOrWhiteSpace(resource))
            {
                promptArgs.AppendFormat(" --resource {0}", QuoteCmdArg(resource));
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                promptArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));
            }

            IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, promptArgs.ToString(), null);

            if (!resultDict.TryGetValue("username", out userName))
            {
                throw new Exception("Missing 'username' in response");
            }

            if (!resultDict.TryGetValue("password", out string password))
            {
                throw new Exception("Missing 'password' in response");
            }

            return new GitCredential(userName, password);
        }

        private bool TryFindHelperExecutablePath(out string path)
        {
            return TryFindHelperExecutablePath(
                Constants.EnvironmentVariables.GcmUiHelper,
                Constants.GitConfiguration.Credential.UiHelper,
                Constants.DefaultUiHelper,
                out path);
        }
    }
}
