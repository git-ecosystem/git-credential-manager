using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using GitHub.UI.ViewModels;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace GitHub.UI.Commands
{
    public abstract class TwoFactorCommand : HelperCommand
    {
        protected TwoFactorCommand(ICommandContext context)
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

            await ShowAsync(viewModel, CancellationToken.None);

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

        protected abstract Task ShowAsync(TwoFactorViewModel viewModel, CancellationToken ct);
    }
}
