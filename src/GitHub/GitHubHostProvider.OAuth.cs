using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tty;

namespace GitHub;

public partial class GitHubHostProvider
{
    internal static string GetOAuthServiceName(Uri uri)
    {
        var canonical = new Uri(GetServiceName(uri));
        return new UriBuilder(canonical) { Host = $"refreshable_token.{canonical.Host}" }
            .Uri.AbsoluteUri.TrimEnd('/');
    }

    private bool CanStoreOAuthCredentials => !StringComparer.OrdinalIgnoreCase.Equals(
        _context.Settings.CredentialBackingStore, Constants.CredentialStoreNames.None);

    private async Task<IDisposable> AcquireCredentialLockAsync(Uri uri) =>
        CanStoreOAuthCredentials
            ? await GitHubCredentialLock.AcquireAsync(_context.FileSystem, new Uri(GetServiceName(uri)).Host)
            : null;

    private async Task<IList<string>> GetAccountsAsync(Uri uri)
    {
        using var credentialLock = await AcquireCredentialLockAsync(uri);
        return _context.CredentialStore.GetAccounts(GetServiceName(uri))
            .Concat(_context.CredentialStore.GetAccounts(GetOAuthServiceName(uri)))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private GitHubOAuthCredential ReadOAuthCredential(Uri uri, string account)
    {
        ICredential stored = _context.CredentialStore.Get(GetOAuthServiceName(uri), account);
        if (stored is null) return null;

        GitHubOAuthCredential credential;
        try
        {
            credential = JsonSerializer.Deserialize(
                stored.Password, GitHubCredentialJsonContext.Default.GitHubOAuthCredential);
        }
        catch (JsonException)
        {
            throw new OAuth2Exception(
                "The stored GitHub OAuth credential is invalid. Run 'git credential-manager github login --force' to sign in again.");
        }

        if (credential is null ||
            !StringComparer.OrdinalIgnoreCase.Equals(stored.Account, credential.Account) ||
            string.IsNullOrWhiteSpace(credential.AccessToken) ||
            string.IsNullOrWhiteSpace(credential.RefreshToken) ||
            string.IsNullOrWhiteSpace(credential.ClientId))
        {
            throw new OAuth2Exception(
                "The stored GitHub OAuth credential is incomplete. Run 'git credential-manager github login --force' to sign in again.");
        }
        return credential;
    }

    private void SaveOAuthCredential(Uri uri, GitHubOAuthCredential credential, bool storeAccessToken = true)
    {
        // Store the pair first, so a failed legacy-token write cannot lose the rotated refresh token.
        _context.CredentialStore.AddOrUpdate(
            GetOAuthServiceName(uri), credential.Account,
            JsonSerializer.Serialize(credential, GitHubCredentialJsonContext.Default.GitHubOAuthCredential));
        if (storeAccessToken)
        {
            _context.CredentialStore.AddOrUpdate(GetServiceName(uri), credential.Account, credential.AccessToken);
        }
    }

    private async Task<ICredential> GetStoredCredentialAsync(Uri uri, string account)
    {
        using var credentialLock = await AcquireCredentialLockAsync(uri);
        GitHubOAuthCredential credential = ReadOAuthCredential(uri, account);
        if (credential is null)
        {
            return _context.CredentialStore.Get(GetServiceName(uri), account);
        }

        string clientId = GitHubOAuth2Client.GetClientId(_context.Settings);
        if (credential.ClientId != clientId)
        {
            DeleteOAuthCredential(uri, credential.Account);
            _context.Console.WriteWarning("GitHub OAuth client configuration changed; sign in again.");
            return null;
        }
        if (credential.ExpiresAt > _clock.GetUtcNow().AddMinutes(5))
        {
            return credential;
        }
        if (credential.RefreshTokenExpiresAt <= _clock.GetUtcNow())
        {
            DeleteOAuthCredential(uri, credential.Account);
            _context.Console.WriteWarning("GitHub OAuth credentials expired; sign in again.");
            return null;
        }

        EnsureSecureOAuthTransport(uri);
        DateTimeOffset requestedAt = _clock.GetUtcNow();
        OAuth2TokenResult result;
        try
        {
            result = await _gitHubAuth.RefreshOAuthTokenAsync(uri, credential.RefreshToken);
        }
        catch (OAuth2Exception exception) when (exception.ErrorCode is "bad_refresh_token" or "invalid_grant")
        {
            DeleteOAuthCredential(uri, credential.Account);
            _context.Console.WriteWarning("GitHub rejected the refresh token; sign in again.");
            return null;
        }

        GitHubOAuthCredential renewed = GitHubOAuthCredential.Create(
            credential.Account, clientId, result, requestedAt, credential);
        SaveOAuthCredential(uri, renewed);
        return renewed;
    }

    private void DeleteOAuthCredential(Uri uri, string account)
    {
        _context.CredentialStore.Remove(GetOAuthServiceName(uri), account);
        _context.CredentialStore.Remove(GetServiceName(uri), account);
    }

    private async Task StoreAccountCredentialAsync(Uri uri, ICredential credential, bool onlyReplaceOAuth = false)
    {
        using var credentialLock = await AcquireCredentialLockAsync(uri);
        if (onlyReplaceOAuth && _context.CredentialStore.Get(GetOAuthServiceName(uri), credential.Account) is null)
        {
            return;
        }
        if (credential is GitHubOAuthCredential oauth)
        {
            SaveOAuthCredential(uri, oauth);
        }
        else
        {
            _context.CredentialStore.Remove(GetOAuthServiceName(uri), credential.Account);
            _context.CredentialStore.AddOrUpdate(GetServiceName(uri), credential.Account, credential.Password);
        }
    }

    private static GitResponse CreateGitResponse(ICredential credential)
    {
        if (credential is not GitHubOAuthCredential oauth)
        {
            return new GitResponse(credential);
        }
        GitResponse response = GitResponse.Ok(credential, isEphemeral: true);
        if (oauth.ExpiresAt.HasValue)
        {
            response.AdditionalProperties["password_expiry_utc"] =
                oauth.ExpiresAt.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        }
        return response;
    }

    private void EnsureSecureOAuthTransport(Uri uri)
    {
        if (!_context.Settings.AllowUnsafeRemotes && uri.Scheme == Uri.UriSchemeHttp)
        {
            throw new InvalidOperationException(
                "Cannot refresh GitHub credentials over HTTP. Use an HTTPS remote.");
        }
    }
}
