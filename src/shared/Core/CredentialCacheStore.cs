using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitCredentialManager.Authentication.OAuth;

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
            var input = MakeGitCredentialsEntry(service, new GitCredential(account));

            var result = _git.InvokeHelperAsync(
                $"credential-cache get {_options}",
                input
            ).GetAwaiter().GetResult();

            if (result.ContainsKey("username") && result.ContainsKey("password"))
            {
                DateTimeOffset? PasswordExpiry = null;
                if (result.ContainsKey("password_expiry_utc") && long.TryParse(result["password_expiry_utc"], out long x)) {
                    PasswordExpiry = DateTimeOffset.FromUnixTimeSeconds(x);
                }
                return new GitCredential(result["username"], result["password"]) {

                    PasswordExpiry = PasswordExpiry,
                    OAuthRefreshToken = result.ContainsKey("oauth_refresh_token") ? result["oauth_refresh_token"] : null,
                };
            }

            return null;
        }

        public void AddOrUpdate(string service, ICredential credential)
        {
            var input = MakeGitCredentialsEntry(service, credential);

            // per https://git-scm.com/docs/gitcredentials :
            // For a store or erase operation, the helper’s output is ignored.
            _git.InvokeHelperAsync(
                $"credential-cache store {_options}",
                input
            ).GetAwaiter().GetResult();
        }

        public bool Remove(string service, ICredential credential)
        {
            var input = MakeGitCredentialsEntry(service, credential);

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

        private Dictionary<string, string> MakeGitCredentialsEntry(string service, ICredential credential)
        {
            var result = new Dictionary<string, string>();

            result["url"] = service;
            if (!string.IsNullOrEmpty(credential?.Account))
            {
                result["username"] = credential.Account;
            }
            if (!string.IsNullOrEmpty(credential?.Password))
            {
                result["password"] = credential.Password;
            }
            if (credential?.PasswordExpiry.HasValue ?? false)
            {
                result["password_expiry_utc"] = credential.PasswordExpiry.Value.ToUnixTimeSeconds().ToString();
            }
            if (!string.IsNullOrEmpty(credential?.OAuthRefreshToken))
            {
                result["oauth_refresh_token"] = credential.OAuthRefreshToken;
            }

            return result;
        }

        public void AddOrUpdate(string service, string account, string secret)
        => AddOrUpdate(service, new GitCredential(account, secret));

        public bool Remove(string service, string account)
        => Remove(service, new GitCredential(account));

        public bool CanStorePasswordExpiry => true;
        public bool CanStoreOAuthRefreshToken => true;
    }
}
