namespace GitCredentialManager.Authentication
{
    public interface IStructuredSecret
    {
        long? Expiry { get; }
        string Provider { get; }
        string Type { get; }
        string Value { get; }

        protected class Generic(string type, string secret, long? expiry = null, string provider = null) : IStructuredSecret
        {
            public long? Expiry => expiry;
            public string Provider => provider;
            public string Type => type;
            public string Value => secret;
        }

        public static bool TryCreate(string raw, out IStructuredSecret secret)
        {
            // probe multi-line secret with password/token on first line
            var lines = raw.Split('\n');
            if (lines.Length > 1)
            {
                string password = lines[0];
                string provider = null;
                long? expiry = null;

                if (password.Length > 0 && password[^1] == '\r')
                {
                    password = password[..^1];
                }

                for (var i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Length < 2)
                    {
                        continue;
                    }
                    if (line.Length > 0 && line[^1] == '\r')
                    {
                        line = line[..^1];
                    }
                    var parts = line.Split('=', 2);
                    if (parts.Length < 2)
                    {
                        continue;
                    }
                    var key = parts[0];
                    var val = parts[1];
                    // alternative secret value
                    if ("password".Equals(key)
                        || "credential".Equals(key))
                    {
                        password = val;
                    }
                    // expiry info (git and git-credential-libsecret, alternative namespace)
                    if (("password_expiry_utc".Equals(key)
                        || "git:password_expiry_utc".Equals(key))
                        && long.TryParse(val, out var e))
                    {
                        expiry = e;
                    }
                    // GCM provider selection
                    if ("gcm:provider".Equals(key))
                    {
                        provider = val;
                    }
                    // API and detection to be extended for:
                    //   - oauth_refresh_token
                    //   - gcm:refresh_url
                }
                var type = expiry == null ? "persistent" : "expiring";
                secret = new Generic(type, password, expiry, provider);
                return true;
            }

            // probe secret value for known inline metadata format
            if (JsonWebToken.TryCreate(raw, out JsonWebToken jwt))
            {
                secret = new Generic(JsonWebToken.Type, raw, jwt.Expiry);
                return true;
            }

            // regular raw secret without structured data
            secret = null;
            return false;
        }
    }
}
