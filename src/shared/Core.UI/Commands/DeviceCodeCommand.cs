using System;
using System.CommandLine;
using System.CommandLine.Invocation;
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
            AddOption(
                new Option<string>("--code", "User code.")
            );

            AddOption(
                new Option<string>("--url", "Verification URL.")
            );

            AddOption(
                new Option("--no-logo", "Hide the Git Credential Manager logo and logotype.")
            );

            Handler = CommandHandler.Create<string, string, bool>(ExecuteAsync);
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
                throw new Exception("User cancelled dialog.");
            }

            return 0;
        }

        protected abstract Task ShowAsync(DeviceCodeViewModel viewModel, CancellationToken ct);
    }
}
