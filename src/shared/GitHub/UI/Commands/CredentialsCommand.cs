using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using GitHub.UI.ViewModels;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace GitHub.UI.Commands
{
    public abstract class CredentialsCommand : HelperCommand
    {
        protected CredentialsCommand(ICommandContext context)
            : base(context, "prompt", "Show authentication prompt.")
        {
            var url = new Option<string>("--enterprise-url", "GitHub Enterprise URL.");
            AddOption(url);

            var userName = new Option<string>("--username", "Username or email.");
            AddOption(userName);

            var basic = new Option<bool>("--basic", "Enable username/password (basic) authentication.");
            AddOption(basic);

            var browser = new Option<bool>("--browser", "Enable browser-based OAuth authentication.");
            AddOption(browser);

            var device = new Option<bool>("--device", "Enable device code OAuth authentication.");
            AddOption(device);

            var pat = new Option<bool>("--pat", "Enable personal access token authentication.");
            AddOption(pat);

            var all = new Option<bool>("--all", "Enable all available authentication options.");
            AddOption(all);

            this.SetHandler(ExecuteAsync, url, userName, basic, browser, device, pat, all);
        }

        private async Task<int> ExecuteAsync(string userName, string enterpriseUrl,
            bool basic, bool browser, bool device, bool pat, bool all)
        {
            var viewModel = new CredentialsViewModel(Context.Environment, Context.ProcessManager)
            {
                ShowBrowserLogin = all || browser,
                ShowDeviceLogin  = all || device,
                ShowTokenLogin   = all || pat,
                ShowBasicLogin   = all || basic,
            };

            if (!GitHubHostProvider.IsGitHubDotCom(enterpriseUrl))
            {
                viewModel.EnterpriseUrl = enterpriseUrl;
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                viewModel.UserName = userName;
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
