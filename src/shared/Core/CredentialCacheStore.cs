using System;
using System.Collections.Generic;

namespace GitCredentialManager
{
    public class CredentialCacheStore : ICredentialStore
    {
        readonly IGit _git;
        readonly string _options;

        public CredentialCacheStore(IGit git, string options)
        {
            _git = git;
            if (string.IsNullOrEmpty(options))
            {
                _options = string.Empty;
            }
            else
            {
                _options = options;
            }
        }

        #region ICredentialStore

        public IList<string> GetAccounts(string service)
        {
            // Listing accounts is not supported by the credential-cache store so we just attempt to retrieve
            // the username from first credential for the given service and return an empty list if it fails.
            var input = MakeGitCredentialsEntry(service, null);

            var result = _git.InvokeHelperAsync(
                $"credential-cache get {_options}",
                input
            ).GetAwaiter().GetResult();

            if (result.TryGetValue("username", out string value))
            {
                return new List<string> { value };
            }

            return Array.Empty<string>();
        }

        public ICredential Get(string service, string account)
        {
            var input = MakeGitCredentialsEntry(service, account);

            var result = _git.InvokeHelperAsync(
                $"credential-cache get {_options}",
                input
            ).GetAwaiter().GetResult();

            if (result.ContainsKey("username") && result.ContainsKey("password"))
            {
                return new GitCredential(result["username"], result["password"]);
            }

            return null;
        }

        public void AddOrUpdate(string service, string account, string secret)
        {
            var input = MakeGitCredentialsEntry(service, account);
            input["password"] = secret;

            // per https://git-scm.com/docs/gitcredentials :
            // For a store or erase operation, the helper’s output is ignored.
            _git.InvokeHelperAsync(
                $"credential-cache store {_options}",
                input
            ).GetAwaiter().GetResult();
        }

        public bool Remove(string service, string account)
        {
            var input = MakeGitCredentialsEntry(service, account);

            // per https://git-scm.com/docs/gitcredentials :
            // For a store or erase operation, the helper’s output is ignored.
            _git.InvokeHelperAsync(
                $"credential-cache erase {_options}",
                input
            ).GetAwaiter().GetResult();

            // the credential cache doesn't tell us whether anything was erased
            // but we're optimistic sorts
            return true;
        }

        #endregion

        private Dictionary<string, string> MakeGitCredentialsEntry(string service, string account)
        {
            var result = new Dictionary<string, string>();

            result["url"] = service;
            if (!string.IsNullOrEmpty(account))
            {
                result["username"] = account;
            }

            return result;
        }
    }
}
