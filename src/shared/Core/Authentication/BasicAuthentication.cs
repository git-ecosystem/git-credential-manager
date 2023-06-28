using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;

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

            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession)
            {
                if (TryFindHelperCommand(out string command, out string args))
                {
                    return await GetCredentialsViaHelperAsync(command, args, resource, userName);
                }

                return await GetCredentialsViaUiAsync(resource, userName);
            }

            ThrowIfTerminalPromptsDisabled();

            return GetCredentialsViaTty(resource, userName);
        }

        private async Task<ICredential> GetCredentialsViaUiAsync(string resource, string userName)
        {
            var viewModel = new CredentialsViewModel
            {
                Description = !string.IsNullOrWhiteSpace(resource)
                    ? $"Enter your credentials for '{resource}'"
                    : "Enter your credentials",
                UserName = string.IsNullOrWhiteSpace(userName) ? null : userName,
            };

            await AvaloniaUi.ShowViewAsync<CredentialsView>(viewModel, GetParentWindowHandle(), CancellationToken.None);

            ThrowIfWindowCancelled(viewModel);

            return new GitCredential(viewModel.UserName, viewModel.Password);
        }

        private ICredential GetCredentialsViaTty(string resource, string userName)
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

        private async Task<ICredential> GetCredentialsViaHelperAsync(string command, string args, string resource, string userName)
        {
            var promptArgs = new StringBuilder(args);
            promptArgs.Append("basic");

            if (!string.IsNullOrWhiteSpace(resource))
            {
                promptArgs.AppendFormat(" --resource {0}", QuoteCmdArg(resource));
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                promptArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));
            }

            IDictionary<string, string> resultDict = await InvokeHelperAsync(command, promptArgs.ToString(), null);

            if (!resultDict.TryGetValue("username", out userName))
            {
                throw new Trace2Exception(Context.Trace2, "Missing 'username' in response");
            }

            if (!resultDict.TryGetValue("password", out string password))
            {
                throw new Trace2Exception(Context.Trace2, "Missing 'password' in response");
            }

            return new GitCredential(userName, password);
        }

        private bool TryFindHelperCommand(out string command, out string args)
        {
            return TryFindHelperCommand(
                Constants.EnvironmentVariables.GcmUiHelper,
                Constants.GitConfiguration.Credential.UiHelper,
                Constants.DefaultUiHelper,
                out command,
                out args);
        }
    }
}
