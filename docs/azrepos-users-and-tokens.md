# Azure Repos: Access tokens and Accounts

## Different credential types

The Azure Repos host provider supports creating multiple types of credential:

- Azure DevOps personal access tokens
- Microsoft identity OAuth tokens

To select which type of credential the Azure Repos host provider will create
and use, you can set the [`credential.azreposCredentialType`][credential-azreposCredentialType]
configuration entry (or [`GCM_AZREPOS_CREDENTIALTYPE`][gcm-azrepos-credential-type]
environment variable).

### Azure DevOps personal access tokens

Historically, the only option supported by the Azure Repos host provider was
Azure DevOps Personal Access Tokens (PATs).

These PATs are only used by Azure DevOps, and must be [managed through the Azure
DevOps user settings page][azure-devops-pats] or [REST API][azure-devops-api].

PATs have a limited lifetime and new tokens must be created once they expire. In
Git Credential Manager, when a PAT expired (or was manually revoked) this
resulted in a new authentication prompt.

### Microsoft identity OAuth tokens

"Microsoft identity OAuth token" is the generic term for OAuth-based access
tokens issued by Azure Active Directory for either Work and School Accounts
(AAD tokens) or Personal Accounts (Microsoft Account/MSA tokens).

Azure DevOps supports Git authentication using Microsoft identity OAuth tokens
as well as PATs. Microsoft identity OAuth tokens created by Git Credential
Manager are scoped to Azure DevOps only.

Unlike PATs, Microsoft identity OAuth tokens get automatically refreshed and
renewed as long as you are actively using them to perform Git operations.

These tokens are also securely shared with other Microsoft developer tools
including the Visual Studio IDE and Azure CLI. This means that as long as you're
using Git or one of these tools with the same account, you'll never need to
re-authenticate due to expired tokens!

#### User accounts

In versions of Git Credential Manager that support Microsoft identity OAuth
tokens, the user account used to authenticate for a particular Azure DevOps
organization will now be remembered.

The first time you clone, fetch or push from/to an Azure DevOps organization you
will be prompted to sign-in and select a user account. Git Credential Manager
will remember which account you used and continue to use that for all future
remote Git operations (clone/fetch/push). An account is said to be "bound" to
an Azure DevOps organization.

---

**Note:** If GCM is set to use PAT credentials, this account will **NOT** be
used and you will continue to be prompted to select a user account to renew the
credential. This may change in the future.

---

Normally you won't need to worry about managing which user accounts Git
Credential Manager is using as this is configured automatically when you first
authenticate for a particular Azure DevOps organization.

In advanced scenarios (such as using multiple accounts) you can interact with
and manage remembered user accounts using the 'azure-repos' provider command:

```shell
git-credential-manager-core azure-repos [ list | bind | unbind | ... ] <options>
```

##### Listing remembered accounts

You can list all bound user accounts by Git Credential Manager for each Azure
DevOps organization using the `list` command:

```shell
$ git-credential-manager-core azure-repos list
contoso:
  (global) -> alice@contoso.com
fabrikam:
  (global) -> user42@fabrikam.com
```

In the above example, the `contoso` Azure DevOps organization is associated with
the `alice@contoso.com` user account, while the `fabrikam` organization is
associated to the `user42@fabrikam.com` user account.

Global "bindings" apply to all remote Git operations for the current computer
user profile and are stored in `~/.gitconfig` or `%USERPROFILE%\.gitconfig`.

##### Using different accounts within a repository

If you generally use one account for an Azure DevOps organization, the default
global bindings will be sufficient. However, if you wish to use a different
user account for an organization in a particular repository you can use a local
binding.

Local account bindings only apply within a single repository and are stored in
the `.git/config` file. If there are local bindings in a repository you can show
them with the `list` command:

```shell
~/myrepo$ git-credential-manager-core azure-repos list
contoso:
  (global) -> alice@contoso.com
  (local)  -> alice-alt@contoso.com
```

Within the `~/myrepo` repository, the `alice-alt@contoso.com` account will be
used by Git and GCM for the `contoso` Azure DevOps organization.

To create a local binding, use the `bind` command with the `--local` option when
inside a repository:

```shell
~/myrepo$ git-credential-manager-core azure-repos bind --local contoso alice-alt@contso.com
```

```diff
  contoso:
    (global) -> alice@contoso.com
+   (local)  -> alice-alt@contoso.com
```

##### Forget an account

To have Git Credential Manager forget a user account, use the `unbind` command:

```shell
git-credential-manager-core azure-repos unbind fabrikam
```

```diff
  contoso:
    (global) -> alice@contoso.com
- fabrikam:
-   (global) -> user42@fabrikam.com
```

In the above example, and global account binding for the `fabrikam` organization
will be forgotten. The next time you need to renew a PAT (if using PATs) or
perform any remote Git operation (is using Azure tokens) you will be prompted
to authenticate again.

To forget or remove a local binding, within the repository run the `unbind`
command with the `--local` option:

```shell
~/myrepo$ git-credential-manager-core azure-repos unbind --local contoso
```

```diff
  contoso:
    (global) -> alice@contoso.com
-   (local)  -> alice-alt@contoso.com
```

##### Using different accounts for specific Git remotes

As well as global and local user account bindings, you can instruct Git
Credential Manager to use a specific user account for an individual Git remotes
within the same local repository.

To show which accounts are being used for each Git remote in a repository use
the `list` command with the `--show-remotes` option:

```shell
~/myrepo$ git-credential-manager-core azure-repos list --show-remotes
contoso:
  (global) -> alice@contoso.com
  origin:
    (fetch) -> (inherit)
    (push)  -> (inherit)
fabrikam:
  (global) -> alice@fabrikam.com
```

In the above example, the `~/myrepo` repository has a single Git remote named
`origin` that points to the `contoso` Azure DevOps organization. There is no
user account specifically associated with the `origin` remote, so the global
user account binding for `contoso` will be used (the global binding is
inherited).

To associate a user account with a particular Git remote you must manually edit
the remote URL using `git config` commands to include the username in the
[user information][rfc3986-s321] part of the URL.

```shell
git config --local remote.origin.url https://alice-alt%40contoso.com@contoso.visualstudio.com/project/_git/repo
```

In the above example the `alice-alt@contoso.com` account is being set as the
account to use for the `origin` Git remote.

---

**Note:** All special characters must be URL encoded/escaped, for example `@`
becomes `%40`.

---

The `list --show-remotes` command will show the user account specified in the
remote URL:

```shell
~/myrepo$ git-credential-manager-core azure-repos list --show-remotes
contoso:
  (global) -> alice@contoso.com
  origin:
    (fetch) -> alice-alt@contoso.com
    (push)  -> alice-alt@contoso.com
fabrikam:
  (global) -> alice@fabrikam.com
```

[azure-devops-pats]: https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=preview-page
[credential-azreposCredentialType]: configuration.md#credentialazreposcredentialtype
[gcm-azrepos-credential-type]: environment.md#GCM_AZREPOS_CREDENTIALTYPE
[azure-devops-api]: https://docs.microsoft.com/en-gb/rest/api/azure/devops/tokens/pats
[rfc3986-s321]: https://tools.ietf.org/html/rfc3986#section-3.2.1
