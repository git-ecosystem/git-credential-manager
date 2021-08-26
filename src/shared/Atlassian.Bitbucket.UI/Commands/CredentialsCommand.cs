using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.ViewModels;
using Atlassian.Bitbucket.UI.Views;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace Atlassian.Bitbucket.UI.Commands
{
    internal class CredentialsCommand : HelperCommand
    {
        public CredentialsCommand(CommandContext context)
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

            await AvaloniaUi.ShowViewAsync<CredentialsView>(viewModel, GetParentHandle(), CancellationToken.None);

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
    }
}
