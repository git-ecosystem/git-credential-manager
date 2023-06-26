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
            var url = new Option<Uri>("--url", "Bitbucket Server or Data Center URL");
            AddOption(url);

            var userName = new Option<string>("--username", "Username or email.");
            AddOption(userName);

            var oauth = new Option<bool>("--show-oauth", "Show OAuth option.");
            AddOption(oauth);

            var basic = new Option<bool>("--show-basic", "Show username/password option.");
            AddOption(basic);

            this.SetHandler(ExecuteAsync, url, userName, oauth, basic);
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
                throw new Trace2Exception(Context.Trace2, "User cancelled dialog.");
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
