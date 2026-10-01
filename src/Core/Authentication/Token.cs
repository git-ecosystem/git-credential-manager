namespace GitCredentialManager.Authentication
{
    public interface IToken
    {
        bool IsExpired { get; }
        string Type { get; }
        string Value { get; }
    }

    public static class Token
    {
        public static bool TryCreate(string value, out IToken token)
        {
            if (JsonWebToken.TryCreate(value, out JsonWebToken jwt))
            {
                token = jwt;
                return true;
            }
            token = null;
            return false;
        }
    }
}
