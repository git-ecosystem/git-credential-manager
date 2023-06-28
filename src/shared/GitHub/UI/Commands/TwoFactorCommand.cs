using System.Collections.Generic;
using System.CommandLine;
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
            var sms = new Option<bool>("--sms", "Two-factor code was sent via SMS.");
            AddOption(sms);

            this.SetHandler(ExecuteAsync, sms);
        }

        private async Task<int> ExecuteAsync(bool sms)
        {
            var viewModel = new TwoFactorViewModel(Context.Environment, Context.ProcessManager)
            {
                IsSms = sms
            };

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Trace2Exception(Context.Trace2, "User cancelled dialog.");
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
