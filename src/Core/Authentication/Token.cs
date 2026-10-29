namespace GitCredentialManager.Authentication
{
    public interface IToken
    {
        string Type { get; }
        string Value { get; }
        long? Expiry { get; }
    }

    public static class Token
    {
        protected class Jwt(string value, long? expiry) : IToken
        {
            public string Type => JsonWebToken.Type;
            public string Value => value;
            public long? Expiry => expiry;

        }

        public static bool TryCreate(string value, out IToken token)
        {
            if (JsonWebToken.TryCreate(value, out JsonWebToken jwt))
            {
                token = new Jwt(value, jwt.Expiry);
                return true;
            }

            token = null;
            return false;
        }
    }
}
