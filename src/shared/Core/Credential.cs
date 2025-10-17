
using System;
using GitCredentialManager.Authentication.OAuth;

namespace GitCredentialManager
{
    /// <summary>
    /// Represents a credential.
    /// </summary>
    public interface ICredential
    {
        /// <summary>
        /// Account associated with this credential.
        /// </summary>
        string Account { get; }

        /// <summary>
        /// Password.
        /// </summary>
        string Password { get; }

        /// <summary>
        /// The expiry date of the password. This is Git's password_expiry_utc
        /// attribute. https://git-scm.com/docs/git-credential#Documentation/git-credential.txt-codepasswordexpiryutccode
        /// </summary>
        DateTimeOffset? PasswordExpiry { get; }

        /// <summary>
        /// An OAuth refresh token. This is Git's oauth_refresh_token
        /// attribute. https://git-scm.com/docs/git-credential#Documentation/git-credential.txt-codeoauthrefreshtokencode
        /// </summary>
        string OAuthRefreshToken { get; }
    }

    /// <summary>
    /// Represents a credential (username/password pair) that Git can use to authenticate to a remote repository.
    /// </summary>
    public class GitCredential : ICredential
    {
        public GitCredential(string userName, string password)
        {
            Account = userName;
            Password = password;
        }

        public GitCredential(string account)
        {
            Account = account;
        }

        public GitCredential(InputArguments input)
        {
            Account = input.UserName;
            Password = input.Password;
            OAuthRefreshToken = input.OAuthRefreshToken;
            if (long.TryParse(input.PasswordExpiry, out long x)) {
                PasswordExpiry = DateTimeOffset.FromUnixTimeSeconds(x);
            }
        }

        public GitCredential(OAuth2TokenResult tokenResult, string userName)
        {
            Account = userName;
            Password = tokenResult.AccessToken;
            OAuthRefreshToken = tokenResult.RefreshToken;
            if (tokenResult.ExpiresIn.HasValue) {
                PasswordExpiry = DateTimeOffset.UtcNow + tokenResult.ExpiresIn.Value;
            }
        }

        public string Account { get; set; }

        public string Password { get; set; }

        public DateTimeOffset? PasswordExpiry { get; set; }
        
        public string OAuthRefreshToken { get; set; }
    }
}
