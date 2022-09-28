using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
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
            AddOption(
                new Option<string>("--title", "Window title (optional).")
            );

            AddOption(
                new Option<string>("--resource", "Resource name or URL (optional).")
            );

            AddOption(
                new Option<string>("--username", "User name (optional).")
            );

            AddOption(
                new Option("--no-logo", "Hide the Git Credential Manager logo and logotype.")
            );

            Handler = CommandHandler.Create<CommandOptions>(ExecuteAsync);
        }

        private class CommandOptions
        {
            public string Title { get; set; }
            public string Resource { get; set; }
            public string UserName { get; set; }
            public bool NoLogo { get; set; }
        }

        private async Task<int> ExecuteAsync(CommandOptions options)
        {
            var viewModel = new CredentialsViewModel();

            viewModel.Title = !string.IsNullOrWhiteSpace(options.Title)
                ? options.Title
                : "Git Credential Manager";

            viewModel.Description = !string.IsNullOrWhiteSpace(options.Resource)
                ? $"Enter your credentials for '{options.Resource}'"
                : "Enter your credentials";

            if (!string.IsNullOrWhiteSpace(options.UserName))
            {
                viewModel.UserName = options.UserName;
            }

            viewModel.ShowProductHeader = !options.NoLogo;

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
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
