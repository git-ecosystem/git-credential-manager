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

            Handler = CommandHandler.Create<string>(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(string userName)
        {
            var viewModel = new CredentialsViewModel(Context.Environment)
            {
                UserName = userName
            };

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }

            WriteResult(new Dictionary<string, string>
            {
                ["username"] = viewModel.UserName,
                ["password"] = viewModel.Password,
            });

            return 0;
        }

        protected abstract Task ShowAsync(CredentialsViewModel viewModel, CancellationToken ct);
    }
}
