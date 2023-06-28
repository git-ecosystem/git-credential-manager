using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.Commands
{
    public abstract class DeviceCodeCommand : HelperCommand
    {
        protected DeviceCodeCommand(ICommandContext context)
            : base(context, "device", "Show device code prompt.")
        {
            var code = new Option<string>("--code", "User code.");
            AddOption(code);

            var url =new Option<string>("--url", "Verification URL.");
            AddOption(url);

            var noLogo = new Option<bool>("--no-logo", "Hide the Git Credential Manager logo and logotype.");
            AddOption(noLogo);

            this.SetHandler(ExecuteAsync, code, url, noLogo);
        }

        private async Task<int> ExecuteAsync(string code, string url, bool noLogo)
        {
            var viewModel = new DeviceCodeViewModel(Context.Environment)
            {
                UserCode = code,
                VerificationUrl = url,
            };

            viewModel.ShowProductHeader = !noLogo;

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
