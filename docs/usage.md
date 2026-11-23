# Command-line usage

After installation, Git will use Git Credential Manager and you will only need
to interact with any authentication dialogs asking for credentials.
GCM stays invisible as much as possible, so ideally you’ll forget that you’re
depending on GCM at all.

Assuming GCM has been installed, use your favorite terminal to execute the
following commands to interact directly with GCM.

```shell
git credential-manager [<command> [<args>]]
```

## Commands

### --help / -h / -?

Displays a list of available commands.

### --version

Displays the current version.

### get / store / erase

Commands for interaction with Git. You shouldn't need to run these manually.

Read the [Git manual][git-credentials-custom-helpers] about custom helpers for
more information.

### configure/unconfigure

Set your user-level Git configuration (`~/.gitconfig`) to use GCM. If you pass
`--system` to these commands, they act on the system-level Git configuration
(`/etc/gitconfig`) instead.

### azure-repos

Interact with the Azure Repos host provider to bind/unbind user accounts to
Azure DevOps organizations or specific remote URLs, and manage the
authentication authority cache.

For more information about managing user account bindings see
[here][azure-access-tokens-ua].

[azure-access-tokens-ua]: azrepos-users-and-tokens.md#useraccounts
[git-credentials-custom-helpers]: https://git-scm.com/docs/gitcredentials#_custom_helpers

### github

Interact with the GitHub host provider to manage your accounts on GitHub.com and
GitHub Enterprise Server instances.

#### Expiring GitHub OAuth tokens

New browser and device sign-ins on GitHub.com and GHE.com request
`offline_access`. If GitHub issues a refreshable token, GCM stores the access
token, refresh token, and expiration times in the configured credential store.
It refreshes on demand within five minutes of access-token expiry, saving the
rotated pair before returning credentials to Git. GCM serializes GitHub
credential reads and writes through lock files under its writable user data
directory, including account enumeration and token rotation.

Existing non-expiring credentials are unchanged. To migrate an account, sign in
again:

```shell
git credential-manager github login --username YOUR_USERNAME --force --device
```

GitHub Enterprise Server sign-ins keep their existing scopes for compatibility
with older servers. GCM handles refreshable tokens if the server issues them.
Manually supplied personal access tokens are not refreshed.

Expired or rejected refresh tokens require signing in again; transient refresh
errors do not erase stored credentials. When Git rejects the current access
token, GCM retains the refresh token and renews on the next request.
Use `github logout YOUR_USERNAME` to remove both tokens. To replace a
refreshable credential with a PAT, use
`github login --username YOUR_USERNAME --force --token YOUR_PAT`.

GCM returns access-token expiry to Git and marks refreshable credentials as
non-cacheable when Git advertises the `authtype` capability (Git 2.46 or newer).
Older Git versions still work, but avoid chaining a credential caching helper
that might reuse a token after rotation. All GCM instances sharing a credential
store must use the same writable GCM data directory for refresh locking.

With `credential.credentialStore=none`, GCM does not request `offline_access`,
so another credential helper can continue storing a non-expiring token. If
GitHub nevertheless issues a refreshable token, GCM requires configuring a
credential store rather than discarding the refresh token.
