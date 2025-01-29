using System;
using System.Collections.Generic;

namespace GitCredentialManager;

/// <summary>
/// Credential store that does nothing. This is useful when you want to disable internal credential storage
/// and only use another helper configured in Git to store credentials.
/// </summary>
public class NullCredentialStore : ICredentialStore
{
    public IList<string> GetAccounts(string service) => Array.Empty<string>();

    public ICredential Get(string service, string account) => null;

    public void AddOrUpdate(string service, string account, string secret) { }

    public bool Remove(string service, string account) => false;
}
