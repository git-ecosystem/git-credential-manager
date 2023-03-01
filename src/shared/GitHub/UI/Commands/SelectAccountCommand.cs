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
    public abstract class SelectAccountCommand : HelperCommand
    {
        protected SelectAccountCommand(ICommandContext context)
            : base(context, "select-account", "Show account selection prompt. Accounts are read line-by-line from standard input.")
        {
            var url = new Option<string>(new[] { "--enterprise-url" }, "Enterprise URL.");
            AddOption(url);

            var noHelp = new Option<bool>(new[] { "--no-help" }, "Hide the help link.");
            AddOption(noHelp);

            this.SetHandler(ExecuteAsync, url, noHelp);
        }

        private async Task<int> ExecuteAsync(string enterpriseUrl, bool noHelp)
        {
            // Read accounts from standard input
            IList<string> accounts = ReadAccounts();

            var viewModel = new SelectAccountViewModel(Context.Environment, accounts)
            {
                EnterpriseUrl = enterpriseUrl,
                ShowHelpLink = !noHelp
            };

            await ShowAsync(viewModel, CancellationToken.None);

            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }

            WriteResult(new Dictionary<string, string>
            {
                ["account"] = viewModel.SelectedAccount?.UserName
            });

            return 0;
        }

        private IList<string> ReadAccounts()
        {
            var accounts = new List<string>();

            string line;
            while (!string.IsNullOrWhiteSpace(line = Context.Streams.In.ReadLine()))
            {
                accounts.Add(line.Trim());
            }

            return accounts;
        }

        protected abstract Task ShowAsync(SelectAccountViewModel viewModel, CancellationToken ct);
    }
}
