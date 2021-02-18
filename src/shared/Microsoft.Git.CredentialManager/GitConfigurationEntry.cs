// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Microsoft.Git.CredentialManager
{
    public class GitConfigurationEntry
    {
        public GitConfigurationEntry(GitConfigurationLevel level, string key, string value)
        {
            Level = level;
            Key = key;
            Value = value;
        }

        public GitConfigurationLevel Level { get; }
        public string Key { get; }
        public string Value { get; }
    }
}
