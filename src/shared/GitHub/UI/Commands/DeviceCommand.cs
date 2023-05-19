using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using GitHub.UI.ViewModels;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace GitHub.UI.Commands
{
    public abstract class DeviceCodeCommand : HelperCommand
    {
        protected DeviceCodeCommand(ICommandContext context)
            : base(context, "device", "Show device code prompt.")
        {
            var code = new Option<string>("--code", "User code.");
            AddOption(code);

            var url = new Option<string>("--url", "Verification URL.");
            AddOption(url);

            this.SetHandler(ExecuteAsync, code, url);
        }

        private async Task<int> ExecuteAsync(string code, string url)
        {
            var viewModel = new DeviceCodeViewModel(Context.Environment)
            {
                UserCode = code,
                VerificationUrl = url,
            };

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Trace2Exception(Context.Trace2, "User cancelled dialog.");
            }

            return 0;
        }

        protected abstract Task ShowAsync(DeviceCodeViewModel viewModel, CancellationToken ct);
    }
}
