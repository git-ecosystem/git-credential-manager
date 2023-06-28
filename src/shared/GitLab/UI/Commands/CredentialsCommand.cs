using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using GitLab.UI.ViewModels;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace GitLab.UI.Commands
{
    public abstract class CredentialsCommand : HelperCommand
    {
        protected CredentialsCommand(ICommandContext context)
            : base(context, "prompt", "Show authentication prompt.")
        {
            var url = new Option<string>("--url", "GitLab instance URL.");
            AddOption(url);

            var userName = new Option<string>("--username", "Username or email.");
            AddOption(userName);

            var basic = new Option<bool>("--basic", "Enable username/password (basic) authentication.");
            AddOption(basic);

            var browser = new Option<bool>("--browser", "Enable browser-based OAuth authentication.");
            AddOption(browser);

            var pat = new Option<bool>("--pat", "Enable personal access token authentication.");
            AddOption(pat);

            var all = new Option<bool>("--all", "Enable all available authentication options.");
            AddOption(all);

            this.SetHandler(ExecuteAsync, url, userName, basic, browser, pat, all);
        }

        private async Task<int> ExecuteAsync(string userName, string url, bool basic, bool browser, bool pat, bool all)
        {
            var viewModel = new CredentialsViewModel(Context.Environment)
            {
                ShowBrowserLogin = all || browser,
                ShowTokenLogin   = all || pat,
                ShowBasicLogin   = all || basic,
            };

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && !GitLabConstants.IsGitLabDotCom(uri))
            {
                viewModel.Url = url;
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                viewModel.UserName = userName;
                viewModel.TokenUserName = userName;
            }

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Trace2Exception(Context.Trace2, "User cancelled dialog.");
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

                case AuthenticationModes.Pat:
                    result["mode"] = "pat";
                    result["pat"] = viewModel.Token;
                    result["username"] = viewModel.TokenUserName;
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
