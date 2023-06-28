using GitCredentialManager;

namespace GitHub
{
    public struct AuthenticationResult
    {
        public AuthenticationResult(GitHubAuthenticationResultType type)
        {
            Type = type;
            Token = null;
        }

        public AuthenticationResult(GitHubAuthenticationResultType type, string token)
        {
            Type = type;
            Token = token;
        }

        public GitHubAuthenticationResultType Type { get; }

        public string Token { get; }
    }

    public enum GitHubAuthenticationResultType
    {
        Success,
        Failure,
        TwoFactorApp,
        TwoFactorSms,
    }
}
