using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.ViewModels;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace Atlassian.Bitbucket.UI.Commands
{
    public abstract class CredentialsCommand : HelperCommand
    {
        protected CredentialsCommand(ICommandContext context)
            : base(context, "prompt", "Show authentication prompt.")
        {
            AddOption(
                new Option<string>("--url", "Bitbucket Server or Data Center URL")
            );

            AddOption(
                new Option<string>("--username", "Username or email.")
            );

            AddOption(
                new Option("--show-oauth", "Show OAuth option.")
            );

            AddOption(
                new Option("--show-basic", "Show username/password option.")
            );

            Handler = CommandHandler.Create<Uri, string, bool, bool>(ExecuteAsync);
        }

        private async Task<int> ExecuteAsync(Uri url, string userName, bool showOAuth, bool showBasic)
        {
            var viewModel = new CredentialsViewModel(Context.Environment)
            {
                Url = url,
                UserName = userName,
                ShowOAuth = showOAuth,
                ShowBasic = showBasic
            };

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult || viewModel.SelectedMode == AuthenticationModes.None)
            {
                throw new Exception("User cancelled dialog.");
            }

            switch (viewModel.SelectedMode)
            {
                case AuthenticationModes.OAuth:
                    WriteResult(new Dictionary<string, string>
                    {
                        ["mode"] = "oauth"
                    });
                    break;

                case AuthenticationModes.Basic:
                    WriteResult(new Dictionary<string, string>
                    {
                        ["mode"] = "basic",
                        ["username"] = viewModel.UserName,
                        ["password"] = viewModel.Password,
                    });
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(AuthenticationModes),
                        "Unknown authentication mode", viewModel.SelectedMode.ToString());
            }

            return 0;
        }

        protected abstract Task ShowAsync(CredentialsViewModel viewModel, CancellationToken ct);
    }
}
