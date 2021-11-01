using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.ViewModels;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace Atlassian.Bitbucket.UI.Commands
{
    public abstract class OAuthCommand : HelperCommand
    {
        protected OAuthCommand(ICommandContext context)
            : base(context, "oauth", "Show OAuth required prompt.")
        {
            Handler = CommandHandler.Create(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync()
        {
            var viewModel = new OAuthViewModel(Context.Environment);

            await ShowAsync(viewModel, CancellationToken.None);

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

        protected abstract Task ShowAsync(OAuthViewModel viewModel, CancellationToken ct);
    }
}
