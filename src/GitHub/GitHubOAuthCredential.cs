using System;
using System.Text.Json.Serialization;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace GitHub;

internal sealed class GitHubOAuthCredential : ICredential
{
    [JsonRequired]
    public string Account { get; set; }

    [JsonRequired]
    public string AccessToken { get; set; }

    [JsonRequired]
    public string RefreshToken { get; set; }

    [JsonRequired]
    public string ClientId { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    [JsonIgnore]
    public string Password => AccessToken;

    public static GitHubOAuthCredential Create(
        string account, string clientId, OAuth2TokenResult result, DateTimeOffset requestedAt,
        GitHubOAuthCredential previous = null)
    {
        if (string.IsNullOrWhiteSpace(result.AccessToken) ||
            result.ExpiresIn <= TimeSpan.Zero ||
            result.RefreshTokenExpiresIn <= TimeSpan.Zero)
        {
            throw new OAuth2Exception("GitHub returned invalid OAuth credentials.");
        }

        string refreshToken = string.IsNullOrWhiteSpace(result.RefreshToken)
            ? previous?.RefreshToken : result.RefreshToken;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new OAuth2Exception("GitHub did not return a refresh token.");
        }

        return new GitHubOAuthCredential
        {
            Account = account,
            AccessToken = result.AccessToken,
            RefreshToken = refreshToken,
            ClientId = clientId,
            ExpiresAt = result.ExpiresIn.HasValue ? requestedAt + result.ExpiresIn.Value : null,
            RefreshTokenExpiresAt = result.RefreshTokenExpiresIn.HasValue
                ? requestedAt + result.RefreshTokenExpiresIn.Value
                : refreshToken == previous?.RefreshToken ? previous.RefreshTokenExpiresAt : null
        };
    }
}

[JsonSerializable(typeof(GitHubOAuthCredential))]
internal partial class GitHubCredentialJsonContext : JsonSerializerContext;
