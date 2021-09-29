using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using GitHub.UI.ViewModels;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace GitHub.UI.Commands
{
    public abstract class CredentialsCommand : HelperCommand
    {
        protected CredentialsCommand(ICommandContext context)
            : base(context, "prompt", "Show authentication prompt.")
        {
            AddOption(
                new Option<string>("--enterprise-url", "GitHub Enterprise URL.")
            );

            AddOption(
                new Option<string>("--username", "Username or email.")
            );

            AddOption(
                new Option("--basic", "Enable username/password (basic) authentication.")
            );

            AddOption(
                new Option("--browser", "Enable browser-based OAuth authentication.")
            );

            AddOption(
                new Option("--device", "Enable device code OAuth authentication.")
            );

            AddOption(
                new Option("--pat", "Enable personal access token authentication.")
            );

            AddOption(
                new Option("--all", "Enable all available authentication options.")
            );

            Handler = CommandHandler.Create<CommandOptions>(ExecuteAsync);
        }

        private class CommandOptions
        {
            public string UserName { get; set; }
            public string EnterpriseUrl { get; set; }
            public bool Basic { get; set; }
            public bool Browser { get; set; }
            public bool Device { get; set; }
            public bool Pat { get; set; }
            public bool All { get; set; }
        }

        private async Task<int> ExecuteAsync(CommandOptions options)
        {
            var viewModel = new CredentialsViewModel(Context.Environment)
            {
                ShowBrowserLogin = options.All || options.Browser,
                ShowDeviceLogin  = options.All || options.Device,
                ShowTokenLogin   = options.All || options.Pat,
                ShowBasicLogin   = options.All || options.Basic,
            };

            if (!GitHubHostProvider.IsGitHubDotCom(options.EnterpriseUrl))
            {
                viewModel.EnterpriseUrl = options.EnterpriseUrl;
            }

            if (!string.IsNullOrWhiteSpace(options.UserName))
            {
                viewModel.UserName = options.UserName;
            }

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }

            var result = new Dictionary<string, string>();

            switch (viewModel.SelectedMode)
            {
                case AuthenticationModes.Basic:
                    result["mode"] = "basic";
                    result["username"] = viewModel.UserName;
                    result["password"] = viewModel.Password;
                    break;

                case AuthenticationModes.Browser:
                    result["mode"] = "browser";
                    break;

                case AuthenticationModes.Device:
                    result["mode"] = "device";
                    break;

                case AuthenticationModes.Pat:
                    result["mode"] = "pat";
                    result["pat"] = viewModel.Token;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            WriteResult(result);
            return 0;
        }

        protected abstract Task ShowAsync(CredentialsViewModel viewModel, CancellationToken ct);
    }
}
