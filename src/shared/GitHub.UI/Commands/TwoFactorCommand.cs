using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using GitHub.UI.ViewModels;
using GitHub.UI.Views;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace GitHub.UI.Commands
{
    public class TwoFactorCommand : HelperCommand
    {
        public TwoFactorCommand(ICommandContext context)
            : base(context, "2fa", "Show two-factor prompt.")
        {
            AddOption(
                new Option("--sms", "Two-factor code was sent via SMS.")
            );

            Handler = CommandHandler.Create<bool>(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(bool sms)
        {
            var viewModel = new TwoFactorViewModel(Context.Environment)
            {
                IsSms = sms
            };

            await AvaloniaUi.ShowViewAsync<TwoFactorView>(viewModel, GetParentHandle(), CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }

            WriteResult(new Dictionary<string, string>
            {
                ["code"] = viewModel.Code
            });

            return 0;
        }
    }
}
