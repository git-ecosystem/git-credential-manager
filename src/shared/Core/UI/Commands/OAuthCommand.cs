using System;
using System.Collections.Generic;
using System.CommandLine;
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
            var title = new Option<string>("--title", "Window title (optional).");
            AddOption(title);

            var resource = new Option<string>("--resource", "Resource name or URL (optional).");
            AddOption(resource);

            var browser = new Option<bool>("--browser", "Show browser authentication option.");
            AddOption(browser);

            var deviceCode = new Option<bool>("--device-code", "Show device code authentication option.");
            AddOption(deviceCode);

            var noLogo = new Option<bool>("--no-logo", "Hide the Git Credential Manager logo and logotype.");
            AddOption(noLogo);

            this.SetHandler(ExecuteAsync, title, resource, browser, deviceCode, noLogo);
        }

        private async Task<int> ExecuteAsync(string title, string resource, bool browser, bool deviceCode, bool noLogo)
        {
            var viewModel = new OAuthViewModel();

            if (!string.IsNullOrWhiteSpace(title))
            {
                viewModel.Title = title;
            }

            viewModel.Description = !string.IsNullOrWhiteSpace(resource)
                ? $"Sign in to '{resource}'"
                : "Select a sign-in option";

            viewModel.ShowBrowserLogin = browser;
            viewModel.ShowDeviceCodeLogin = deviceCode;
            viewModel.ShowProductHeader = !noLogo;

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Trace2Exception(Context.Trace2, "User cancelled dialog.");
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
