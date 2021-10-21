namespace GitCredentialManager
{
    public class GitConfigurationEntry
    {
        public GitConfigurationEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public string Value { get; }
    }
}
