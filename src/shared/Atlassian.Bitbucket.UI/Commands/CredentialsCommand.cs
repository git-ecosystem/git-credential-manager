using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.ViewModels;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace Atlassian.Bitbucket.UI.Commands
{
    public abstract class CredentialsCommand : HelperCommand
    {
        protected CredentialsCommand(ICommandContext context)
            : base(context, "userpass", "Show authentication prompt.")
        {
            AddOption(
                new Option<string>("--username", "Username or email.")
            );

            AddOption(
                new Option("--show-oauth", "Show OAuth option.")
            );

            Handler = CommandHandler.Create<string, bool>(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(string userName, bool showOAuth)
        {
            var viewModel = new CredentialsViewModel(Context.Environment)
            {
                UserName = userName,
                ShowOAuth = showOAuth
            };

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }

            if (viewModel.UseOAuth)
            {
                WriteResult(new Dictionary<string, string>
                {
                    ["mode"] = "oauth"
                });
            }
            else
            {
                WriteResult(new Dictionary<string, string>
                {
                    ["mode"] = "basic",
                    ["username"] = viewModel.UserName,
                    ["password"] = viewModel.Password,
                });
            }

            return 0;
        }

        protected abstract Task ShowAsync(CredentialsViewModel viewModel, CancellationToken ct);
    }
}
