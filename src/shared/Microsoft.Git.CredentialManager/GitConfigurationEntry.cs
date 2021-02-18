// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Microsoft.Git.CredentialManager
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
