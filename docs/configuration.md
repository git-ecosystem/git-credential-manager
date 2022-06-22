# Configuration options

[Git Credential Manager](usage.md) works out of the box for most users.

Git Credential Manager (GCM) can be configured using Git's configuration files, and follows all of the same rules Git does when consuming the files.

Global configuration settings override system configuration settings, and local configuration settings override global settings; and because the configuration details exist within Git's configuration files you can use Git's `git config` utility to set, unset, and alter the setting values. All of GCM's configuration settings begin with the term `credential`.

GCM honors several levels of settings, in addition to the standard local \> global \> system tiering Git uses.
URL-specific settings or overrides can be applied to any value in the `credential` namespace with the syntax below.

Additionally, GCM respects several GCM-specific [environment variables](environment.md) **which take precedence over configuration options**. System administrators may also configure [default values](enterprise-config.md) for many settings used by GCM.

GCM will only be used by Git if it is installed and configured. Use `git config --global credential.helper manager-core` to assign GCM as your credential helper. Use `git config credential.helper` to see the current configuration.

**Example:**

> `credential.microsoft.visualstudio.com.namespace` is more specific than `credential.visualstudio.com.namespace`, which is more specific than `credential.namespace`.

In the examples above, the `credential.namespace` setting would affect any remote repository; the `credential.visualstudio.com.namespace` would affect any remote repository in the domain, and/or any subdomain (including `www.`) of, 'visualstudio.com'; where as the `credential.microsoft.visualstudio.com.namespace` setting would only be applied to remote repositories hosted at 'microsoft.visualstudio.com'.

For the complete list of settings GCM understands, see the list below.

## Available settings

### credential.interactive

Permit or disable GCM from interacting with the user (showing GUI or TTY prompts). If interaction is required but has been disabled, an error is returned.

This can be helpful when using GCM in headless and unattended environments, such as build servers, where it would be preferable to fail than to hang indefinitely waiting for a non-existent user.

To disable interactivity set this to `false` or `0`.

#### Compatibility

In previous versions of GCM this setting had a different behavior and accepted other values.
The following table summarizes the change in behavior and the mapping of older values such as `never`:

Value(s)|Old meaning|New meaning
-|-|-
`auto`|Prompt if required – use cached credentials if possible|_(unchanged)_
`never`, `false`| Never prompt – fail if interaction is required|_(unchanged)_
`always`, `force`, `true`|Always prompt – don't use cached credentials|Prompt if required (same as the old `auto` value)

#### Example

```shell
git config --global credential.interactive false
```

Defaults to enabled.

**Also see: [GCM_INTERACTIVE](environment.md#GCM_INTERACTIVE)**

---

### credential.provider

Define the host provider to use when authenticating.

ID|Provider
-|-
`auto` _(default)_|_\[automatic\]_ ([learn more](autodetect.md))
`azure-repos`|Azure Repos
`github`|GitHub
`bitbucket`|Bitbucket
`gitlab`|GitLab _(supports OAuth in browser, personal access token and Basic Authentication)_
`generic`|Generic (any other provider not listed above)

Automatic provider selection is based on the remote URL.

This setting is typically used with a scoped URL to map a particular set of remote URLs to providers, for example to mark a host as a GitHub Enterprise instance.

#### Example

```shell
git config --global credential.ghe.contoso.com.provider github
```

**Also see: [GCM_PROVIDER](environment.md#GCM_PROVIDER)**

---

### credential.authority _(deprecated)_

> This setting is deprecated and should be replaced by `credential.provider` with the corresponding provider ID value.
>
> Click [here](https://aka.ms/gcm/authority) for more information.

Select the host provider to use when authenticating by which authority is supported by the providers.

Authority|Provider(s)
-|-
`auto` _(default)_|_\[automatic\]_
`msa`, `microsoft`, `microsoftaccount`, `aad`, `azure`, `azuredirectory`, `live`, `liveconnect`, `liveid`|Azure Repos _(supports Microsoft Authentication)_
`github`|GitHub _(supports GitHub Authentication)_
`bitbucket`|Bitbucket.org _(supports Basic Authentication and OAuth)_, Bitbucket Server _(supports Basic Authentication)_
`gitlab`|GitLab _(supports OAuth in browser, personal access token and Basic Authentication)_
`basic`, `integrated`, `windows`, `kerberos`, `ntlm`, `tfs`, `sso`|Generic _(supports Basic and Windows Integrated Authentication)_

#### Example

```shell
git config --global credential.ghe.contoso.com.authority github
```

**Also see: [GCM_AUTHORITY](environment.md#GCM_AUTHORITY-deprecated)**

---

### credential.guiPrompt

Permit or disable GCM from presenting GUI prompts. If an equivalent terminal/
text-based prompt is available, that will be shown instead.

To disable all interactivity see [credential.interactive](#credentialinteractive).

#### Example

```shell
git config --global credential.guiPrompt false
```

Defaults to enabled.

**Also see: [GCM_GUI_PROMPT](environment.md#GCM_GUI_PROMPT)**

---

### credential.autoDetectTimeout

Set the maximum length of time, in milliseconds, that GCM should wait for a
network response during host provider auto-detection probing.

See [here](autodetect.md) for more information.

**Note:** Use a negative or zero value to disable probing altogether.

Defaults to 2000 milliseconds (2 seconds).

#### Example

```shell
git config --global credential.autoDetectTimeout -1
```

**Also see: [GCM_AUTODETECT_TIMEOUT](environment.md#GCM_AUTODETECT_TIMEOUT)**

---

### credential.allowWindowsAuth

Allow detection of Windows Integrated Authentication (WIA) support for generic host providers. Setting this value to `false` will prevent the use of WIA and force a basic authentication prompt when using the Generic host provider.

**Note:** WIA is only supported on Windows.

**Note:** WIA is an umbrella term for NTLM and Kerberos (and Negotiate).

Value|WIA detection
-|-
`true` _(default)_|Permitted
`false`|Not permitted

#### Example

```shell
git config --global credential.tfsonprem123.allowWindowsAuth false
```

**Also see: [GCM_ALLOW_WINDOWSAUTH](environment.md#GCM_ALLOW_WINDOWSAUTH)**

---

### credential.httpProxy _(deprecated)_

> This setting is deprecated and should be replaced by the [standard `http.proxy` Git configuration option](https://git-scm.com/docs/git-config#Documentation/git-config.txt-httpproxy).
>
> Click [here](https://aka.ms/gcm/httpproxy) for more information.

Configure GCM to use the a proxy for network operations.

**Note:** Git itself does _not_ respect this setting; this affects GCM _only_.

#### Example

```shell
git config --global credential.httpsProxy http://john.doe:password@proxy.contoso.com
```

**Also see: [GCM_HTTP_PROXY](environment.md#GCM_HTTP_PROXY-deprecated)**

---

### credential.bitbucketAuthModes

Override the available authentication modes presented during Bitbucket authentication.
If this option is not set, then the available authentication modes will be automatically detected.

**Note:** This setting only applies to Bitbucket.org, and not Server or DC instances.

**Note:** This setting supports multiple values separated by commas.

Value|Authentication Mode
-|-
_(unset)_|Automatically detect modes
`oauth`|OAuth-based authentication
`basic`|Basic/PAT-based authentication

#### Example

```shell
git config --global credential.bitbucketAuthModes "oauth,basic"
```

**Also see: [GCM_BITBUCKET_AUTHMODES](environment.md#GCM_BITBUCKET_AUTHMODES)**

---

### credential.bitbucketAlwaysRefreshCredentials

Forces GCM to ignore any existing stored Basic Auth or OAuth access tokens and always run through the process to refresh the credentials before returning them to Git.

This is especially relevant to OAuth credentials. Bitbucket.org access tokens expire after 2 hours, after that the refresh token must be used to get a new access token.

Enabling this option will improve performance when using Oauth2 and interacting with Bitbucket.org if, on average, commits are done less frequently than every 2 hours.

Enabling this option will decrease performance when using Basic Auth by requiring the user the re-enter credentials everytime.

Value|Refresh Credentials Before Returning
-|-
`true`, `1`, `yes`, `on` |Always
`false`, `0`, `no`, `off`_(default)_|Only when the credentials are found to be invalid

#### Example

```shell
git config --global credential.bitbucketAlwaysRefreshCredentials 1
```

Defaults to false/disabled.

**Also see: [GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS](environment.md#GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS)**

---

### credential.gitHubAuthModes

Override the available authentication modes presented during GitHub authentication.
If this option is not set, then the available authentication modes will be automatically detected.

**Note:** This setting supports multiple values separated by commas.

Value|Authentication Mode
-|-
_(unset)_|Automatically detect modes
`oauth`|Expands to: `browser, device`
`browser`|OAuth authentication via a web browser _(requires a GUI)_
`device`|OAuth authentication with a device code
`basic`|Basic authentication using username and password
`pat`|Personal Access Token (pat)-based authentication

#### Example

```shell
git config --global credential.gitHubAuthModes "oauth,basic"
```

**Also see: [GCM_GITHUB_AUTHMODES](environment.md#GCM_GITHUB_AUTHMODES)**

---

### credential.gitLabAuthModes

Override the available authentication modes presented during GitLab authentication.
If this option is not set, then the available authentication modes will be automatically detected.

**Note:** This setting supports multiple values separated by commas.

Value|Authentication Mode
-|-
_(unset)_|Automatically detect modes
`browser`|OAuth authentication via a web browser _(requires a GUI)_
`basic`|Basic authentication using username and password
`pat`|Personal Access Token (pat)-based authentication

#### Example

```shell
git config --global credential.gitLabAuthModes "browser"
```

**Also see: [GCM_GITLAB_AUTHMODES](environment.md#GCM_GITLAB_AUTHMODES)**

---

### credential.namespace

Use a custom namespace prefix for credentials read and written in the OS credential store.
Credentials will be stored in the format `{namespace}:{service}`.

Defaults to the value `git`.

#### Example

```shell
git config --global credential.namespace "my-namespace"
```

**Also see: [GCM_NAMESPACE](environment.md#GCM_NAMESPACE)**

---

### credential.credentialStore

Select the type of credential store to use on supported platforms.

Default value on Windows is `wincredman`, on macOS is `keychain`, and is unset on Linux.

**Note:** See more information about configuring secret stores [here](credstores.md).

Value|Credential Store|Platforms
-|-|-
_(unset)_|Windows: `wincredman`, macOS: `keychain`, Linux: _(none)_|-
`wincredman`|Windows Credential Manager (not available over SSH).|Windows
`dpapi`|DPAPI protected files. Customize the DPAPI store location with [credential.dpapiStorePath](#credentialdpapistorepath)|Windows
`keychain`|macOS Keychain.|macOS
`secretservice`|[freedesktop.org Secret Service API](https://specifications.freedesktop.org/secret-service/) via [libsecret](https://wiki.gnome.org/Projects/Libsecret) (requires a graphical interface to unlock secret collections).|Linux
`gpg`|Use GPG to store encrypted files that are compatible with the [`pass` utility](https://www.passwordstore.org/) (requires GPG and `pass` to initialize the store).|macOS, Linux
`cache`|Git's built-in [credential cache](https://git-scm.com/docs/git-credential-cache).|Windows, macOS, Linux
`plaintext`|Store credentials in plaintext files (**UNSECURE**). Customize the plaintext store location with [`credential.plaintextStorePath`](#credentialplaintextstorepath).|Windows, macOS, Linux

#### Example

```bash
git config --global credential.credentialStore gpg
```

**Also see: [GCM_CREDENTIAL_STORE](environment.md#GCM_CREDENTIAL_STORE)**

---

### credential.cacheOptions

Pass [options](https://git-scm.com/docs/git-credential-cache#_options)
to the Git credential cache when
[`credential.credentialStore`](#credentialcredentialstore)
is set to `cache`. This allows you to select a different amount
of time to cache credentials (the default is 900 seconds) by passing
`"--timeout <seconds>"`. Use of other options like `--socket` is untested
and unsupported, but there's no reason it shouldn't work.

Defaults to empty.

#### Example

```shell
git config --global credential.cacheOptions "--timeout 300"
```

**Also see: [GCM_CREDENTIAL_CACHE_OPTIONS](environment.md#GCM_CREDENTIAL_CACHE_OPTIONS)**

---

### credential.plaintextStorePath

Specify a custom directory to store plaintext credential files in when [`credential.credentialStore`](#credentialcredentialstore) is set to `plaintext`.

Defaults to the value `~/.gcm/store` or `%USERPROFILE%\.gcm\store`.

#### Example

```shell
git config --global credential.plaintextStorePath /mnt/external-drive/credentials
```

**Also see: [GCM_PLAINTEXT_STORE_PATH](environment.md#GCM_PLAINTEXT_STORE_PATH)**

---

### credential.dpapiStorePath

Specify a custom directory to store DPAPI protected credential files in when [`credential.credentialStore`](#credentialcredentialstore) is set to `dpapi`.

Defaults to the value `%USERPROFILE%\.gcm\dpapi_store`.

#### Example

```batch
git config --global credential.dpapiStorePath D:\credentials
```

**Also see: [GCM_DPAPI_STORE_PATH](environment.md#GCM_DPAPI_STORE_PATH)**

---

### credential.msauthFlow

Specify which authentication flow should be used when performing Microsoft authentication and an interactive flow is required.

Defaults to `auto`.

**Note:** If [`credential.msauthUseBroker`](#credentialmsauthusebroker-experimental) is set
to `true` and the operating system authentication broker is available, all flows
will be delegated to the broker. If both of those things are true, then the
value of `credential.msauthFlow` has no effect.

Value|Authentication Flow
-|-
`auto` _(default)_|Select the best option depending on the current environment and platform.
`embedded`|Show a window with embedded web view control.
`system`|Open the user's default web browser.
`devicecode`|Show a device code.

#### Example

```shell
git config --global credential.msauthFlow devicecode
```

**Also see: [GCM_MSAUTH_FLOW](environment.md#GCM_MSAUTH_FLOW)**

---

### credential.msauthUseBroker _(experimental)_

Use the operating system account manager where available.

Defaults to `false`. This default is subject to change in the future.

_**Note:** before you enable this option on Windows, please [review the details](windows-broker.md) about what this means to your local Windows user account._

Value|Description
-|-
`true`|Use the operating system account manager as an authentication broker.
`false` _(default)_|Do not use the broker.

#### Example

```shell
git config --global credential.msauthUseBroker true
```

**Also see: [GCM_MSAUTH_USEBROKER](environment.md#GCM_MSAUTH_USEBROKER-experimental)**

---

### credential.useHttpPath

Tells Git to pass the entire repository URL, rather than just the hostname, when calling out to a credential provider. (This setting [comes from Git itself](https://git-scm.com/docs/gitcredentials/#Documentation/gitcredentials.txt-useHttpPath), not GCM.)

Defaults to `false`.

**Note:** GCM sets this value to `true` for `dev.azure.com` (Azure Repos) hosts after installation by default.

This is because `dev.azure.com` alone is not enough information to determine the correct Azure authentication authority - we require a part of the path. The fallout of this is that for `dev.azure.com` remote URLs we do not support storing credentials against the full-path. We always store against the `dev.azure.com/org-name` stub.

In order to use Azure Repos and store credentials against a full-path URL, you must use the `org-name.visualstudio.com` remote URL format instead.

Value|Git Behavior
-|-
`false` _(default)_|Git will use only `user` and `hostname` to look up credentials.
`true`|Git will use the full repository URL to look up credentials.

#### Example

On Windows using GitHub, for a user whose login is `alice`, and with `credential.useHttpPath` set to `false` (or not set), the following remote URLs will use the same credentials:

```text
Credential: "git:https://github.com" (user = alice)

   https://github.com/foo/bar
   https://github.com/contoso/widgets
   https://alice@github.com/contoso/widgets
```

```text
Credential: "git:https://bob@github.com" (user = bob)

   https://bob@github.com/foo/bar
   https://bob@github.com/example/myrepo
```

Under the same user but with `credential.useHttpPath` set to `true`, these credentials would be used:

```text
Credential: "git:https://github.com/foo/bar" (user = alice)

   https://github.com/foo/bar
```

```text
Credential: "git:https://github.com/contoso/widgets" (user = alice)

   https://github.com/contoso/widgets
   https://alice@github.com/contoso/widgets
```

```text
Credential: "git:https://bob@github.com/foo/bar" (user = bob)

   https://bob@github.com/foo/bar
```

```text
Credential: "git:https://bob@github.com/example/myrepo" (user = bob)

   https://bob@github.com/example/myrepo
```

---

### credential.azreposCredentialType

Specify the type of credential the Azure Repos host provider should return.

Defaults to the value `pat`.

Value|Description
-|-
`pat` _(default)_|Azure DevOps personal access tokens
`oauth`|Microsoft identity OAuth tokens (AAD or MSA tokens)

More information about Azure Access tokens can be found [here](azrepos-users-and-tokens.md).

#### Example

```shell
git config --global credential.azreposCredentialType oauth
```

**Also see: [GCM_AZREPOS_CREDENTIALTYPE](environment.md#GCM_AZREPOS_CREDENTIALTYPE)**
