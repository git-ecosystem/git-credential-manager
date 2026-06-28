using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.Commands
{
    public abstract class CredentialsCommand : HelperCommand
    {
        protected CredentialsCommand(ICommandContext context)
            : base(context, "basic", "Show basic authentication prompt.")
        {
            var title = new Option<string>("--title", "Window title (optional).");
            AddOption(title);

            var resource = new Option<string>("--resource", "Resource name or URL (optional).");
            AddOption(resource);

            var userName = new Option<string>("--username", "User name (optional).");
            AddOption(userName);

            var noLogo = new Option<bool>("--no-logo", "Hide the Git Credential Manager logo and logotype.");
            AddOption(noLogo);

            this.SetHandler(ExecuteAsync, title, resource, userName, noLogo);
        }

        private async Task<int> ExecuteAsync(string title, string resource, string userName, bool noLogo)
        {
            var viewModel = new CredentialsViewModel();

            if (!string.IsNullOrWhiteSpace(title))
            {
                viewModel.Title = title;
            }

            viewModel.Description = !string.IsNullOrWhiteSpace(resource)
                ? $"Enter your credentials for '{resource}'"
                : "Enter your credentials";

            if (!string.IsNullOrWhiteSpace(userName))
            {
                viewModel.UserName = userName;
            }

            viewModel.ShowProductHeader = !noLogo;

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Trace2Exception(Context.Trace2, "User cancelled dialog.");
            }

            WriteResult(
                new Dictionary<string, string>
                {
                    ["username"] = viewModel.UserName,
                    ["password"] = viewModel.Password
                }
            );
            return 0;
        }

        protected abstract Task ShowAsync(CredentialsViewModel viewModel, CancellationToken ct);
    }
}
