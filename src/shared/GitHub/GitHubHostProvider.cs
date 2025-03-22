using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GitHub.Diagnostics;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Diagnostics;

namespace GitHub
{
    public partial class GitHubHostProvider : DisposableObject, IHostProvider, IDiagnosticProvider
    {
        private static readonly string[] GitHubOAuthScopes =
        {
            GitHubConstants.OAuthScopes.Repo,
            GitHubConstants.OAuthScopes.Gist,
            GitHubConstants.OAuthScopes.Workflow,
        };

        private static readonly string[] GitHubCredentialScopes =
        {
            GitHubConstants.TokenScopes.Gist,
            GitHubConstants.TokenScopes.Repo
        };

        private readonly IGitHubRestApi _gitHubApi;
        private readonly IGitHubAuthentication _gitHubAuth;
        private readonly ICommandContext _context;

        public GitHubHostProvider(ICommandContext context)
            : this(context, new GitHubRestApi(context), new GitHubAuthentication(context)) { }

        public GitHubHostProvider(ICommandContext context, IGitHubRestApi gitHubApi, IGitHubAuthentication gitHubAuth)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(gitHubApi, nameof(gitHubApi));
            EnsureArgument.NotNull(gitHubAuth, nameof(gitHubAuth));

            _context = context;
            _gitHubApi = gitHubApi;
            _gitHubAuth = gitHubAuth;
        }

        public string Id => "github";

        public string Name => "GitHub";

        public IEnumerable<string> SupportedAuthorityIds => GitHubAuthentication.AuthorityIds;

        public bool IsSupported(InputArguments input)
        {
            if (input is null)
            {
                return false;
            }

            // We do not support unencrypted HTTP communications to GitHub,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            if (!StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") &&
                !StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https"))
            {
                return false;
            }

            // Split port number and hostname from host input argument
            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                return false;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(hostName, GitHubConstants.GitHubBaseUrlHost) ||
                StringComparer.OrdinalIgnoreCase.Equals(hostName, GitHubConstants.GistBaseUrlHost))
            {
                return true;
            }

            string[] domains = hostName.Split(new char[] { '.' });

            // github[.subdomain].domain.tld
            if (domains.Length >= 3 &&
                StringComparer.OrdinalIgnoreCase.Equals(domains[0], "github"))
            {
                return true;
            }

            // gist.github[.subdomain].domain.tld
            if (domains.Length >= 4 &&
                StringComparer.OrdinalIgnoreCase.Equals(domains[0], "gist") &&
                StringComparer.OrdinalIgnoreCase.Equals(domains[1], "github"))
            {
                return true;
            }

            return false;
        }

        public bool IsSupported(HttpResponseMessage response)
        {
            if (response is null)
            {
                return false;
            }

            // Look for a known GitHub.com/GHES header
            return response.Headers.Contains("X-GitHub-Request-Id");
        }

        internal static /* for testing purposes */ string GetServiceName(InputArguments input)
        {
            // Get the remote URI without user information
            var baseUri = input.GetRemoteUri(includeUser: false);

            return GetServiceName(baseUri);
        }

        private static string GetServiceName(Uri baseUri)
        {
            // Normalise the URI
            string url = NormalizeUri(baseUri).AbsoluteUri;

            // Trim trailing slash
            return url.TrimEnd('/');
        }

        public async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            string service = GetServiceName(input);
            Uri remoteUri = input.GetRemoteUri();

            // If we have a specific username then we can try and find an existing credential for that account.
            // If not, we should check what accounts are available in the store and prompt the user if there
            // are multiple options.
            string userName = input.UserName;
            bool addAccount = false;
            bool filtered = false;
            if (string.IsNullOrWhiteSpace(userName))
            {
                IList<string> accounts = _context.CredentialStore.GetAccounts(service);
                _context.Trace.WriteLine($"Found {accounts.Count} accounts in the store for service={service}{(accounts.Count > 0 ? ":" : ".")}");
                foreach (string account in accounts)
                {
                    _context.Trace.WriteLine($"  {account}");
                }

                filtered = FilterAccounts(remoteUri, input.WwwAuth, ref accounts);

                switch (accounts.Count)
                {
                    case 1:
                        _context.Trace.WriteLine("Only one account available - using that one!");
                        userName = accounts[0];
                        break;

                    case > 1:
                        _context.Trace.WriteLine("Multiple accounts available - prompting user to select one...");
                        userName = await _gitHubAuth.SelectAccountAsync(remoteUri, accounts);
                        addAccount = userName is null;
                        break;
                }
            }

            // Always try and locate an existing credential in the OS credential store unless we're being
            // told to explicitly add a new account OR have specifically filtered out irrelevant accounts.
            // If the account lookup failed for another reason we should still try to lookup an existing credential.
            ICredential credential = null;
            if (addAccount)
            {
                _context.Trace.WriteLine("Adding a new account!");
            }
            else if (!string.IsNullOrWhiteSpace(userName) || !filtered)
            {
                _context.Trace.WriteLine($"Looking for existing credential in store with service={service} account={userName}...");
                credential = _context.CredentialStore.Get(service, userName);
            }

            if (credential == null)
            {
                _context.Trace.WriteLine("No existing credentials found.");

                // No existing credential was found, create a new one
                _context.Trace.WriteLine("Creating new credential...");
                credential = await GenerateCredentialAsync(remoteUri, userName);
                _context.Trace.WriteLine("Credential created.");
            }
            else
            {
                _context.Trace.WriteLine("Existing credential found.");
            }

            return credential;
        }

        private bool FilterAccounts(Uri remoteUri, IEnumerable<string> wwwAuth, ref IList<string> accounts)
        {
            if (!IsGitHubDotCom(remoteUri))
            {
                _context.Trace.WriteLine("No account filtering outside of GitHub.com.");
            }

            // Allow the user to disable account filtering until this feature stabilises.
            // Default to enabled.
            bool enableFiltering = !_context.Settings.TryGetSetting(
                GitHubConstants.EnvironmentVariables.AccountFiltering,
                Constants.GitConfiguration.Credential.SectionName,
                GitHubConstants.GitConfiguration.Credential.AccountFiltering,
                out string enableFilteringStr
            ) || enableFilteringStr.ToBooleanyOrDefault(true);

            if (!enableFiltering)
            {
                _context.Trace.WriteLine("Account filtering is disabled.");
                return false;
            }

            _context.Trace.WriteLine("Account filtering is enabled.");

            // If we have a WWW-Authenticate header then we can try and use any domain hint information
            // to filter the list of accounts to only those that are valid for that domain.
            // We only expect one challenge header to be returned, but if we're given more we just select the first.
            GitHubAuthChallenge authChallenge = GitHubAuthChallenge.FromHeaders(wwwAuth).FirstOrDefault();
            if (authChallenge is not null)
            {
                _context.Trace.WriteLine("Filtering based on WWW-Authenticate header information...");
                accounts = accounts.Where(authChallenge.IsDomainMember).ToList();

                _context.Trace.WriteLine(string.IsNullOrWhiteSpace(authChallenge.Domain)
                    ? $"Matched {accounts.Count} accounts with public domain:"
                    : $"Matched {accounts.Count} accounts with domain={authChallenge.Domain}:");
                foreach (string account in accounts)
                {
                    _context.Trace.WriteLine($"  {account}");
                }

                return true;
            }

            return false;
        }

        public virtual Task StoreCredentialAsync(InputArguments input)
        {
            string service = GetServiceName(input);

            // WIA-authentication is signaled to Git as an empty username/password pair
            // and we will get called to 'store' these WIA credentials.
            // We avoid storing empty credentials.
            if (string.IsNullOrWhiteSpace(input.UserName) && string.IsNullOrWhiteSpace(input.Password))
            {
                _context.Trace.WriteLine("Not storing empty credential.");
            }
            else
            {
                // Add or update the credential in the store.
                _context.Trace.WriteLine($"Storing credential with service={service} account={input.UserName}...");
                _context.CredentialStore.AddOrUpdate(service, input.UserName, input.Password);
                _context.Trace.WriteLine("Credential was successfully stored.");
            }

            return Task.CompletedTask;
        }

        public virtual Task EraseCredentialAsync(InputArguments input)
        {
            string service = GetServiceName(input);

            // Try to locate an existing credential
            _context.Trace.WriteLine($"Erasing stored credential in store with service={service} account={input.UserName}...");
            if (_context.CredentialStore.Remove(service, input.UserName))
            {
                _context.Trace.WriteLine("Credential was successfully erased.");
            }
            else
            {
                _context.Trace.WriteLine("No credential was erased.");
            }

            return Task.CompletedTask;
        }

        internal /* for testing purposes */  async Task<ICredential> GenerateCredentialAsync(Uri remoteUri, string userName)
        {
            ThrowIfDisposed();

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(remoteUri.Scheme, "http"))
            {
                throw new Trace2Exception(_context.Trace2,
                    "Unencrypted HTTP is not supported for GitHub. Ensure the repository remote URL is using HTTPS.");
            }

            string service = GetServiceName(remoteUri);

            AuthenticationModes authModes = await GetSupportedAuthenticationModesAsync(remoteUri);

            AuthenticationPromptResult promptResult = await _gitHubAuth.GetAuthenticationAsync(remoteUri, userName, authModes);

            switch (promptResult.AuthenticationMode)
            {
                case AuthenticationModes.Basic:
                    GitCredential patCredential = await GeneratePersonalAccessTokenAsync(remoteUri, promptResult.Credential);

                    // HACK: Store the PAT immediately in case this PAT is not valid for SSO.
                    // We don't know if this PAT is valid for SAML SSO and if it's not Git will fail
                    // with a 403 and call neither 'store' or 'erase'. The user is expected to fiddle with
                    // the PAT permissions manually on the web and then retry the Git operation.
                    // We must store the PAT now so they can resume/repeat the operation with the same,
                    // now SSO authorized, PAT.
                    // See: https://github.com/git-ecosystem/git-credential-manager/issues/133
                    _context.CredentialStore.AddOrUpdate(service, patCredential.Account, patCredential.Password);
                    return patCredential;

                case AuthenticationModes.Browser:
                    return await GenerateOAuthCredentialAsync(remoteUri, loginHint: userName, useBrowser: true);

                case AuthenticationModes.Device:
                    return await GenerateOAuthCredentialAsync(remoteUri, loginHint: userName, useBrowser: false);

                case AuthenticationModes.Pat:
                    // The token returned by the user should be good to use directly as the password for Git
                    string token = promptResult.Credential.Password;

                    // Resolve the GitHub user handle if we don't have a specific username already from the
                    // initial request. The reason for this is GitHub requires a (any?) value for the username
                    // when Git makes calls to GitHub.
                    userName = promptResult.Credential.Account;
                    if (userName is null)
                    {
                        GitHubUserInfo userInfo = await _gitHubApi.GetUserInfoAsync(remoteUri, token);
                        userName = userInfo.Login;
                    }

                    return new GitCredential(userName, token);

                default:
                    throw new ArgumentOutOfRangeException(nameof(promptResult));
            }
        }

        private async Task<GitCredential> GenerateOAuthCredentialAsync(Uri targetUri, string loginHint, bool useBrowser)
        {
            OAuth2TokenResult result = useBrowser
                ? await _gitHubAuth.GetOAuthTokenViaBrowserAsync(targetUri, GitHubOAuthScopes, loginHint)
                : await _gitHubAuth.GetOAuthTokenViaDeviceCodeAsync(targetUri, GitHubOAuthScopes);

            // Resolve the GitHub user handle
            GitHubUserInfo userInfo = await _gitHubApi.GetUserInfoAsync(targetUri, result.AccessToken);

            return new GitCredential(userInfo.Login, result.AccessToken);
        }

        private async Task<GitCredential> GeneratePersonalAccessTokenAsync(Uri targetUri, ICredential credentials)
        {
            AuthenticationResult result = await _gitHubApi.CreatePersonalTokenAsync(
                targetUri, credentials, null, GitHubCredentialScopes);

            string token = null;

            if (result.Type == GitHubAuthenticationResultType.Success)
            {
                _context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");

                token = result.Token;
            }
            else if (result.Type == GitHubAuthenticationResultType.TwoFactorApp ||
                     result.Type == GitHubAuthenticationResultType.TwoFactorSms)
            {
                bool isSms = result.Type == GitHubAuthenticationResultType.TwoFactorSms;

                string authCode = await _gitHubAuth.GetTwoFactorCodeAsync(targetUri, isSms);

                result = await _gitHubApi.CreatePersonalTokenAsync(targetUri, credentials, authCode, GitHubCredentialScopes);

                if (result.Type == GitHubAuthenticationResultType.Success)
                {
                    _context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded.");

                    token = result.Token;
                }
            }

            if (token != null)
            {
                // Resolve the GitHub user handle
                GitHubUserInfo userInfo = await _gitHubApi.GetUserInfoAsync(targetUri, token);

                return new GitCredential(userInfo.Login, token);
            }

            var format = "Interactive logon for '{0}' failed.";
            var message = string.Format(format, targetUri);
            throw new Trace2Exception(_context.Trace2, message, format);
        }

        internal async Task<AuthenticationModes> GetSupportedAuthenticationModesAsync(Uri targetUri)
        {
            // Check for an explicit override for supported authentication modes
            if (_context.Settings.TryGetSetting(
                GitHubConstants.EnvironmentVariables.AuthenticationModes,
                Constants.GitConfiguration.Credential.SectionName, GitHubConstants.GitConfiguration.Credential.AuthenticationModes,
                out string authModesStr))
            {
                if (Enum.TryParse(authModesStr, true, out AuthenticationModes authModes) && authModes != AuthenticationModes.None)
                {
                    _context.Trace.WriteLine($"Supported authentication modes override present: {authModes}");
                    return authModes;
                }
                else
                {
                    _context.Trace.WriteLine($"Invalid value for supported authentication modes override setting: '{authModesStr}'");
                }
            }

            // GitHub.com should use OAuth or manual PAT based authentication only, never basic auth as of 13th November 2020
            // https://developer.github.com/changes/2020-02-14-deprecating-oauth-auth-endpoint
            if (IsGitHubDotCom(targetUri))
            {
                _context.Trace.WriteLine($"{targetUri} is github.com - authentication schemes: '{GitHubConstants.DotComAuthenticationModes}'");
                return GitHubConstants.DotComAuthenticationModes;
            }

            // For GitHub Enterprise we must do some detection of supported modes
            _context.Trace.WriteLine($"{targetUri} is GitHub Enterprise - checking for supported authentication schemes...");

            try
            {
                GitHubMetaInfo metaInfo = await _gitHubApi.GetMetaInfoAsync(targetUri);

                // All Enterprise/AE instances support PATs
                var modes = AuthenticationModes.Pat;

                // If the server says it supports basic auth, we can use that too!
                if (metaInfo.VerifiablePasswordAuthentication)
                {
                    modes |= AuthenticationModes.Basic;
                }

                // If the version is unknown, we *assume* it supports OAuth.
                // If the server version at least the minimum required, we *know* we can use OAuth.
                if (!Version.TryParse(metaInfo.InstalledVersion, out var version) ||
                    version >= GitHubConstants.MinimumOnPremOAuthVersion)
                {
                    modes |= AuthenticationModes.OAuth;
                }

                _context.Trace.WriteLine($"GitHub Enterprise instance has version '{metaInfo.InstalledVersion}' and supports authentication schemes: {modes}");
                return modes;
            }
            catch (Exception ex)
            {
                var format = "Failed to query '{0}' for supported authentication schemes.";
                var message = string.Format(format, targetUri);

                _context.Trace.WriteLine(message);
                _context.Trace.WriteException(ex);
                _context.Trace2.WriteError(message, format);

                _context.Terminal.WriteLine($"warning: {message}");

                // Fall-back to offering all modes so the user is never blocked from authenticating by at least one mode
                return AuthenticationModes.All;
            }
        }

        protected override void ReleaseManagedResources()
        {
            _gitHubApi.Dispose();
            _gitHubAuth.Dispose();
            base.ReleaseManagedResources();
        }

        public IEnumerable<IDiagnostic> GetDiagnostics()
        {
            yield return new GitHubApiDiagnostic(_gitHubApi, _context);
        }

        #region Private Methods

        public static bool IsGitHubDotCom(string targetUrl)
        {
            return Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri uri) && IsGitHubDotCom(uri);
        }

        public static bool IsGitHubDotCom(Uri targetUri)
        {
            EnsureArgument.AbsoluteUri(targetUri, nameof(targetUri));

            // github.com or gist.github.com are both considered dotcom
            return StringComparer.OrdinalIgnoreCase.Equals(targetUri.Host, GitHubConstants.GitHubBaseUrlHost) ||
                   StringComparer.OrdinalIgnoreCase.Equals(targetUri.Host, GitHubConstants.GistBaseUrlHost);
        }

        internal static Uri NormalizeUri(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            // Special case for gist.github.com which are git backed repositories under the hood.
            // Credentials for these repositories are the same as the one stored with "github.com".
            // Same for gist.github[.subdomain].domain.tld. The general form was already checked via IsSupported.
            int firstDot = uri.DnsSafeHost.IndexOf(".", StringComparison.Ordinal);
            if (firstDot > -1 && uri.DnsSafeHost.Substring(0, firstDot).Equals("gist", StringComparison.OrdinalIgnoreCase))
            {
                return new Uri("https://" + uri.DnsSafeHost.Substring(firstDot+1));
            }

            return uri;
        }

        #endregion
    }
}
