// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication.OAuth;

namespace Atlassian.Bitbucket
{
    public class BitbucketHostProvider : IHostProvider
    {
        private readonly ICommandContext _context;
        private readonly IBitbucketAuthentication _bitbucketAuth;
        private readonly IBitbucketRestApi _bitbucketApi;

        public BitbucketHostProvider(ICommandContext context)
            : this(context, new BitbucketAuthentication(context), new BitbucketRestApi(context)) { }

        public BitbucketHostProvider(ICommandContext context, IBitbucketAuthentication bitbucketAuth, IBitbucketRestApi bitbucketApi)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(bitbucketAuth, nameof(bitbucketAuth));
            EnsureArgument.NotNull(bitbucketApi, nameof(bitbucketApi));

            _context = context;
            _bitbucketAuth = bitbucketAuth;
            _bitbucketApi = bitbucketApi;
        }

        #region IHostProvider

        public string Id => "bitbucket";

        public string Name => "Bitbucket";

        public IEnumerable<string> SupportedAuthorityIds => BitbucketAuthentication.AuthorityIds;

        public bool IsSupported(InputArguments input)
        {
            // We do not support unencrypted HTTP communications to Bitbucket,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `GetCredentialAsync`.
            return input != null &&
                   (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                    StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https")) &&
                   input.Host.EndsWith(BitbucketConstants.BitbucketBaseUrlHost, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            // Compute the target URI
            Uri targetUri = GetTargetUri(input);

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http")
                && !IsBitbucketServer(targetUri))
            {
                throw new Exception("Unencrypted HTTP is not supported for Bitbucket.org. Ensure the repository remote URL is using HTTPS.");
            }

            // Try and get the username specified in the remote URL if any
            string targetUriUser = targetUri.GetUserName();

            // Check for presence of refresh_token entry in credential store
            string refreshKey = GetRefreshTokenKey(input);
            _context.Trace.WriteLine($"Checking for refresh token with key '{refreshKey}'...");
            ICredential refreshToken = _context.CredentialStore.Get(refreshKey);

            if (refreshToken is null)
            {
                // There is no refresh token either because this is a non-2FA enabled account (where OAuth is not
                // required), or because we previously erased the RT.

                // Check for the presence of a credential in the store
                string credentialKey = GetCredentialKey(input);
                _context.Trace.WriteLine($"Checking for credentials with key '{credentialKey}'...");
                ICredential credential = _context.CredentialStore.Get(credentialKey);

                if (credential is null)
                {
                    // We don't have any credentials to use at all! Start with the assumption of no 2FA requirement
                    // and capture username and password via an interactive prompt.
                    credential = await _bitbucketAuth.GetBasicCredentialsAsync(targetUri, targetUriUser);
                    if (credential is null)
                    {
                        throw new Exception("User cancelled authentication prompt.");
                    }
                }

                // Either we have an existing credential (user/pass OR some form of token [PAT or AT]),
                // or we have a freshly captured user/pass. Regardless, we must check if these credentials
                // pass and two-factor requirement on the account.
                _context.Trace.WriteLine("Checking if two-factor requirements for stored credentials...");
                bool requires2Fa = await RequiresTwoFactorAuthenticationAsync(credential, targetUri);
                if (!requires2Fa)
                {
                    _context.Trace.WriteLine("Two-factor requirement passed with stored credentials");

                    // Return the valid credential
                    return credential;
                }

                _context.Trace.WriteLine("Two-factor authentication is required - prompting for auth via OAuth...");

                // Show the 2FA/OAuth authentication required prompt
                bool @continue = await _bitbucketAuth.ShowOAuthRequiredPromptAsync();
                if (!@continue)
                {
                    throw new Exception("User cancelled OAuth authentication.");
                }

                // Fall through to the start of the interactive OAuth authentication flow
            }
            else
            {
                // TODO: should we try and compute if the AT has expired and use it?
                // This needs support from the credential store to record the expiry time!

                // It's very likely that any access token expired between the last time we used/stored it.
                // To ensure the AT is as 'fresh' as it can be, always first try to use the refresh token
                // (which lives longer) to create a new AT (and possibly also a new RT).
                try
                {
                    _context.Trace.WriteLine("Refreshing OAuth credentials using refresh token...");

                    OAuth2TokenResult refreshResult = await _bitbucketAuth.RefreshOAuthCredentialsAsync(refreshToken.Password);

                    // Resolve the username
                    _context.Trace.WriteLine("Resolving username for refreshed OAuth credential...");
                    string refreshUserName = await ResolveOAuthUserNameAsync(refreshResult.AccessToken);
                    _context.Trace.WriteLine($"Username for refreshed OAuth credential is '{refreshUserName}'");

                    // Store the refreshed RT
                    _context.Trace.WriteLine($"Storing new refresh token with key '{refreshKey}'...");
                    _context.CredentialStore.AddOrUpdate(refreshKey,
                        new GitCredential(refreshUserName, refreshResult.RefreshToken));

                    // Return new access token
                    return new GitCredential(refreshUserName, refreshResult.AccessToken);
                }
                catch (OAuth2Exception ex)
                {
                    _context.Trace.WriteLine("Failed to refresh existing OAuth credential using refresh token");
                    _context.Trace.WriteException(ex);

                    // We failed to refresh the AT using the RT; log the refresh failure and fall through to restart
                    // the OAuth authentication flow
                }
            }

            // We failed to use the refresh token either because it didn't exist, or because the refresh token is no
            // longer valid. Either way we must now try authenticating using OAuth interactively.

            // Start OAuth authentication flow
            _context.Trace.WriteLine("Starting OAuth authentication flow...");
            OAuth2TokenResult oauthResult = await _bitbucketAuth.CreateOAuthCredentialsAsync(targetUri);

            // Resolve the username
            _context.Trace.WriteLine("Resolving username for OAuth credential...");
            string newUserName = await ResolveOAuthUserNameAsync(oauthResult.AccessToken);
            _context.Trace.WriteLine($"Username for OAuth credential is '{newUserName}'");

            // Store the new RT
            _context.Trace.WriteLine($"Storing new refresh token with key '{refreshKey}'...");
            _context.CredentialStore.AddOrUpdate(refreshKey, new GitCredential(newUserName, oauthResult.RefreshToken));
            _context.Trace.WriteLine("Refresh token was successfully stored.");

            // Return the new AT as the credential
            return new GitCredential(newUserName, oauthResult.AccessToken);
        }

        public Task StoreCredentialAsync(InputArguments input)
        {
            // It doesn't matter if this is an OAuth access token, or the literal username & password
            // because we store them the same way, against the same credential key in the store.
            // The OAuth refresh token is already stored on the 'get' request.
            string credentialKey = GetCredentialKey(input);
            ICredential credential = new GitCredential(input.UserName, input.Password);

            _context.Trace.WriteLine($"Storing credential with key '{credentialKey}'...");
            _context.CredentialStore.AddOrUpdate(credentialKey, credential);
            _context.Trace.WriteLine("Credential was successfully stored.");

            Uri targetUri = GetTargetUri(input);
            if(IsBitbucketServer(targetUri))
            {
                // BBS doesn't usually include the username in the urls which means they aren't included in the GET call, 
                // which means if we store only with the username the credentials are never found again ...
                // This does have the potential to overwrite itself for different BbS accounts, 
                // but typically BbS doesn't encourage multiple user accounts
                string bbsCredentialKey = GetBbSCredentialKey(input);
                _context.Trace.WriteLine($"Storing Bitbucket Server credential with key '{bbsCredentialKey}'...");
                _context.CredentialStore.AddOrUpdate(bbsCredentialKey, credential);
                _context.Trace.WriteLine("Bitbucket Server Credential was successfully stored.");
            }

            return Task.CompletedTask;
        }

        public Task EraseCredentialAsync(InputArguments input)
        {
            // Erase the stored credential (which may be either the literal username & password, or
            // the OAuth access token). We don't need to erase the OAuth refresh token because on the
            // next 'get' request, if the RT is bad we will erase and reacquire a new one at that point.
            string credentialKey = GetCredentialKey(input);
            _context.Trace.WriteLine($"Erasing credential with key '{credentialKey}'...");
            if (_context.CredentialStore.Remove(credentialKey))
            {
                _context.Trace.WriteLine("Credential was successfully erased.");
            }
            else
            {
                _context.Trace.WriteLine("Credential was not erased.");
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task<string> ResolveOAuthUserNameAsync(string accessToken)
        {
            RestApiResult<UserInfo> result = await _bitbucketApi.GetUserInformationAsync(null, accessToken, true);
            if (result.Succeeded)
            {
                return result.Response.UserName;
            }

            throw new Exception($"Failed to resolve username. HTTP: {result.StatusCode}");
        }

        private async Task<bool> RequiresTwoFactorAuthenticationAsync(ICredential credentials, Uri targetUri)
        {
            if(IsBitbucketServer(targetUri))
            {
                // BBS does not support 2FA out of the box so neither does GCM
                return false;
            }

            RestApiResult<UserInfo> result = await _bitbucketApi.GetUserInformationAsync(credentials.UserName, credentials.Password, false);
            switch (result.StatusCode)
            {
                // 2FA may not be required
                case HttpStatusCode.OK:
                    return result.Response.IsTwoFactorAuthenticationEnabled;

                // 2FA is required
                case HttpStatusCode.Forbidden:
                    return true;

                // Incorrect credentials
                case HttpStatusCode.Unauthorized:
                    throw new Exception("Invalid credentials");

                default:
                    throw new Exception($"Unknown server response: {result.StatusCode}");
            }
        }

        private string GetCredentialKey(InputArguments input)
        {
            // The credential (user/pass or an OAuth access token) key is the full target URI.
            // If the full path is included (credential.useHttpPath = true) then respect that.
            string url = GetTargetUri(input).AbsoluteUri;

            // Trim trailing slash
            if (url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }

            return $"git:{url}";
        }

        private string GetBbSCredentialKey(InputArguments input)
        {
            // The credential (user/pass or an OAuth access token) key is the full target URI.
            // If the full path is included (credential.useHttpPath = true) then respect that.
            string url = GetBbsTargetUri(input).AbsoluteUri;

            // Trim trailing slash
            if (url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }

            return $"git:{url}";
        }

        private string GetRefreshTokenKey(InputArguments input)
        {
            Uri targetUri = GetTargetUri(input);

            // The refresh token key never includes the path component.
            // Starting from the full target URI, build the following:
            //
            //   {scheme}://[{userinfo}@]{authority}/refresh_token
            //

            var url = new StringBuilder();

            url.Append(targetUri.Scheme)
                .Append(Uri.SchemeDelimiter);

            if (!string.IsNullOrWhiteSpace(targetUri.UserInfo))
            {
                url.Append(targetUri.UserInfo)
                    .Append('@');
            }

            url.Append(targetUri.Authority)
                .Append("/refresh_token");

            return $"git:{url}";
        }

        private static Uri GetTargetUri(InputArguments input)
        {
            Uri uri = new UriBuilder
            {
                Scheme = input.Protocol,
                Host = input.Host,
                Path = input.Path,
                UserName = input.UserName
            }.Uri;

            return uri;
        }

        private static Uri GetBbsTargetUri(InputArguments input)
        {
            Uri uri = new UriBuilder
            {
                Scheme = input.Protocol,
                Host = input.Host,
                Path = input.Path
            }.Uri;

            return uri;
        }

        private bool IsBitbucketServer(Uri targetUri)
        {
            return !targetUri.Host.Equals(BitbucketConstants.BitbucketBaseUrlHost);
        }

        #endregion

        public void Dispose()
        {
            _bitbucketApi.Dispose();
            _bitbucketAuth.Dispose();
        }
    }
}
