namespace GitCredentialManager.Authentication
{
    public interface IToken
    {
        public bool IsExpired { get; }
        public string Type { get; }
        public string Value { get; }
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
