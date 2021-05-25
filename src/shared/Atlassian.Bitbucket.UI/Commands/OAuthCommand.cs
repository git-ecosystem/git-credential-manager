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
    internal class OAuthCommand : HelperCommand
    {
        public OAuthCommand(CommandContext context)
            : base(context, "oauth", "Show OAuth required prompt.")
        {
            Handler = CommandHandler.Create(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync()
        {
            var viewModel = new OAuthViewModel(Context.Environment);
            await AvaloniaUi.ShowViewAsync<OAuthView>(viewModel, GetParentHandle(), CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }

            WriteResult(new Dictionary<string, string>
            {
                ["continue"] = "true"
            });

            return 0;
        }
    }
}
