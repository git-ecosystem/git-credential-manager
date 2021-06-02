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
    public class CredentialsCommand : HelperCommand
    {
        public CredentialsCommand(ICommandContext context)
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
                new Option("--oauth", "Enable browser-based OAuth authentication.")
            );

            AddOption(
                new Option("--pat", "Enable personal access token authentication.")
            );

            Handler = CommandHandler.Create<string, string, bool, bool, bool>(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(string enterpriseUrl, string userName, bool basic, bool oauth, bool pat)
        {
            var viewModel = new CredentialsViewModel(Context.Environment)
            {
                ShowBrowserLogin = oauth,
                ShowTokenLogin   = pat,
                ShowBasicLogin   = basic,
            };

            if (!GitHubHostProvider.IsGitHubDotCom(enterpriseUrl))
            {
                viewModel.EnterpriseUrl = enterpriseUrl;
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                viewModel.UserName = userName;
            }

            await AvaloniaUi.ShowViewAsync<CredentialsView>(viewModel, GetParentHandle(), CancellationToken.None);

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

                case AuthenticationModes.OAuth:
                    result["mode"] = "oauth";
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
    }
}
