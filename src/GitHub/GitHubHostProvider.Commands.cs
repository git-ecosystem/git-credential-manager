using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Commands;

namespace GitHub;

public partial class GitHubHostProvider : ICommandProvider
{
    public ProviderCommand CreateCommand()
    {
        var rootCmd = new ProviderCommand(this);

        var urlOpt = new Option<Uri>("--url",
            "URL of the GitHub instance to target, otherwise use GitHub.com");

        //
        // list [--url <url>]
        //
        var listCmd = new Command("list", "List all known GitHub accounts.");
        listCmd.AddOption(urlOpt);
        listCmd.SetHandler(ListAccounts, urlOpt);

        //
        // login [--url <url>] [--username <username>] [--device | --browser/--web | --pat/--token <token>]
        //
        var loginCmd = new Command("login", "Add a GitHub account.");
        var userNameOpt = new Option<string>("--username", "User name to authenticate with");
        var deviceOpt = new Option<bool>("--device", "Use device flow to authenticate");
        var browserOpt = new Option<bool>(new[]{"--web", "--browser"}, "Use a web browser to authenticate");
        var tokenOpt = new Option<string>(new[] {"--pat", "--token"}, "Use personal access token to authenticate");
        var forceOpt = new Option<bool>("--force", "Force re-authentication even if a credential already exists for the account");
        loginCmd.AddOption(urlOpt);
        loginCmd.AddOption(userNameOpt);
        loginCmd.AddOptionSet(OptionArity.ZeroOrOne, deviceOpt, browserOpt, tokenOpt);
        loginCmd.AddOption(forceOpt);
        loginCmd.SetHandler(AddAccountAsync, urlOpt, userNameOpt, deviceOpt, browserOpt, tokenOpt, forceOpt);

        //
        // logout <account> [--url <url>]
        //
        var logoutCmd = new Command("logout", "Remove a GitHub account.");
        var accountArg = new Argument<string>("account", "Account to remove")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        logoutCmd.AddArgument(accountArg);
        logoutCmd.AddOption(urlOpt);
        logoutCmd.SetHandler(RemoveAccount, accountArg, urlOpt);

        rootCmd.AddCommand(listCmd);
        rootCmd.AddCommand(loginCmd);
        rootCmd.AddCommand(logoutCmd);

        return rootCmd;
    }

    private void ListAccounts(Uri url)
    {
        string service = url is null || IsGitHubDotCom(url)
            ? $"https://{GitHubConstants.GitHubBaseUrlHost}"
            : GetServiceName(url);

        IList<string> accounts = _context.CredentialStore.GetAccounts(service);

        foreach (string account in accounts)
        {
            _context.Streams.Out.WriteLine(account);
        }
    }

    private async Task<int> AddAccountAsync(Uri url, string userName, bool device, bool browser, string token, bool force)
    {
        // Default to GitHub.com
        url ??= new Uri($"https://{GitHubConstants.GitHubBaseUrlHost}");

        // Prefer the username specified on the command-line
        userName ??= url.GetUserName();

        string service = GetServiceName(url);

        // If we've already got a credential for this account then we can skip the login flow
        // (so long as the user isn't explicitly forcing a re-authentication).
        if (!string.IsNullOrWhiteSpace(userName) && !force)
        {
            IList<string> existingAccounts = _context.CredentialStore.GetAccounts(service);
            if (existingAccounts.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x, userName)))
            {
                string prettyUrl = url.AbsoluteUri.TrimEnd('/');
                _context.Streams.Out.WriteLine(
                    $"Account '{userName}' already has credentials for {prettyUrl}; use --force to re-authenticate"
                );
                return 0;
            }
        }

        ICredential credential;
        if (token is not null)
        {
            // Resolve the GitHub user handle if the user didn't supply one
            if (string.IsNullOrEmpty(userName))
            {
                GitHubUserInfo userInfo = await _gitHubApi.GetUserInfoAsync(url, token);
                userName = userInfo.Login;
            }

            credential = new GitCredential(userName, token);
        }
        else if (device || browser)
        {
            credential = await GenerateOAuthCredentialAsync(url, loginHint: userName, useBrowser: browser);
        }
        else
        {
            credential = await GenerateCredentialAsync(url, userName);
        }

        _context.CredentialStore.AddOrUpdate(service, credential.Account, credential.Password);

        return 0;
    }

    private Task<int> RemoveAccount(string account, Uri url)
    {
        string service = url is null || IsGitHubDotCom(url)
            ? $"https://{GitHubConstants.GitHubBaseUrlHost}"
            : GetServiceName(url);

        bool result = _context.CredentialStore.Remove(service, account);

        if (!result)
        {
            _context.Streams.Error.WriteLine($"warning: no such account '{account}' found.");
            return Task.FromResult(-1);
        }

        return Task.FromResult(0);
    }
}
