using System;
using System.Collections.Generic;
using System.CommandLine;
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
        // login [--url <url>]
        //
        var loginCmd = new Command("login", "Add a GitHub account.");
        loginCmd.AddOption(urlOpt);
        loginCmd.SetHandler(AddAccountAsync, urlOpt);

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

    private async Task<int> AddAccountAsync(Uri url)
    {
        // Default to GitHub.com
        url ??= new Uri($"https://{GitHubConstants.GitHubBaseUrlHost}");

        string userName = url.GetUserName();
        string service = GetServiceName(url);

        ICredential credential = await GenerateCredentialAsync(url, userName);
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
