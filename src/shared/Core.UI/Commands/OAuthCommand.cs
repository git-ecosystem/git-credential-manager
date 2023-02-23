using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Authentication;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.Commands
{
    public abstract class OAuthCommand : HelperCommand
    {
        protected OAuthCommand(ICommandContext context)
            : base(context, "oauth", "Show OAuth authentication prompt.")
        {
            AddOption(
                new Option<string>("--title", "Window title (optional).")
            );

            AddOption(
                new Option<string>("--resource", "Resource name or URL (optional).")
            );

            AddOption(
                new Option("--browser", "Show browser authentication option.")
            );

            AddOption(
                new Option("--device-code", "Show device code authentication option.")
            );

            AddOption(
                new Option("--no-logo", "Hide the Git Credential Manager logo and logotype.")
            );

            Handler = CommandHandler.Create<CommandOptions>(ExecuteAsync);
        }

        private class CommandOptions
        {
            public string Title { get; set; }
            public string Resource { get; set; }
            public bool Browser { get; set; }
            public bool DeviceCode { get; set; }
            public bool NoLogo { get; set; }
        }

        private async Task<int> ExecuteAsync(CommandOptions options)
        {
            var viewModel = new OAuthViewModel();

            viewModel.Title = !string.IsNullOrWhiteSpace(options.Title)
                ? options.Title
                : "Git Credential Manager";

            viewModel.Description = !string.IsNullOrWhiteSpace(options.Resource)
                ? $"Sign in to '{options.Resource}'"
                : "Select a sign-in option";

            viewModel.ShowBrowserLogin = options.Browser;
            viewModel.ShowDeviceCodeLogin = options.DeviceCode;
            viewModel.ShowProductHeader = !options.NoLogo;

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }

            var result = new Dictionary<string, string>();
            switch (viewModel.SelectedMode)
            {
                case OAuthAuthenticationModes.Browser:
                    result["mode"] = "browser";
                    break;

                case OAuthAuthenticationModes.DeviceCode:
                    result["mode"] = "devicecode";
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            WriteResult(result);
            return 0;
        }

        protected abstract Task ShowAsync(OAuthViewModel viewModel, CancellationToken ct);
    }
}
