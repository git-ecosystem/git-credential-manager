# Environment variables

[Git Credential Manager][gcm] works out of the box for most users. Configuration
options are available to customize or tweak behavior.

Git Credential Manager (GCM) can be configured using environment variables.
**Environment variables take precedence over [configuration][configuration]
options and enterprise system administrator [default values][default-values]**.

For the complete list of environment variables GCM understands, see the list
below.

## Available settings

### GCM_TRACE

Enables trace logging of all activities.
Configuring Git and GCM to trace to the same location is often desirable, and
GCM is compatible and cooperative with `GIT_TRACE`.

#### Example

##### Windows

```batch
SET GIT_TRACE=%UserProfile%\git.log
SET GCM_TRACE=%UserProfile%\git.log
```

##### macOS/Linux

```bash
export GIT_TRACE=$HOME/git.log
export GCM_TRACE=$HOME/git.log
```

If the value of `GCM_TRACE` is a full path to a file in an existing directory,
logs are appended to the file.

If the value of `GCM_TRACE` is `true` or `1`, logs are written to standard error.

Defaults to tracing disabled.

_No configuration equivalent._

---

### GCM_TRACE_SECRETS

Enables tracing of secret and sensitive information, which is by default masked
in trace output. Requires that `GCM_TRACE` is also enabled.

#### Example

##### Windows

```batch
SET GCM_TRACE=%UserProfile%\gcm.log
SET GCM_TRACE_SECRETS=1
```

##### macOS/Linux

```bash
export GCM_TRACE=$HOME/gcm.log
export GCM_TRACE_SECRETS=1
```

If the value of `GCM_TRACE_SECRETS` is `true` or `1`, trace logs will include
secret information.

Defaults to disabled.

_No configuration equivalent._

---

### GCM_TRACE_MSAUTH

Enables inclusion of Microsoft Authentication libraries (ADAL, MSAL) logs in GCM
trace output. Requires that `GCM_TRACE` is also enabled.

#### Example

##### Windows

```batch
SET GCM_TRACE=%UserProfile%\gcm.log
SET GCM_TRACE_MSAUTH=1
```

##### macOS/Linux

```bash
export GCM_TRACE=$HOME/gcm.log
export GCM_TRACE_MSAUTH=1
```

If the value of `GCM_TRACE_MSAUTH` is `true` or `1`, trace logs will include
verbose ADAL/MSAL logs.

Defaults to disabled.

_No configuration equivalent._

---

### GCM_DEBUG

Pauses execution of GCM at launch to wait for a debugger to be attached.

#### Example

##### Windows

```batch
SET GCM_DEBUG=1
```

##### macOS/Linux

```bash
export GCM_DEBUG=1
```

Defaults to disabled.

_No configuration equivalent._

---

### GCM_INTERACTIVE

Permit or disable GCM from interacting with the user (showing GUI or TTY
prompts). If interaction is required but has been disabled, an error is
returned.

This can be helpful when using GCM in headless and unattended environments, such
as build servers, where it would be preferable to fail than to hang indefinitely
waiting for a non-existent user.

To disable interactivity set this to `false` or `0`.

#### Compatibility

In previous versions of GCM this setting had a different behavior and accepted
other values. The following table summarizes the change in behavior and the
mapping of older values such as `never`:

Value(s)|Old meaning|New meaning
-|-|-
`auto`|Prompt if required – use cached credentials if possible|_(unchanged)_
`never`, `false`| Never prompt – fail if interaction is required|_(unchanged)_
`always`, `force`, `true`|Always prompt – don't use cached credentials|Prompt if required (same as the old `auto` value)

#### Example

##### Windows

```batch
SET GCM_INTERACTIVE=0
```

##### macOS/Linux

```bash
export GCM_INTERACTIVE=0
```

Defaults to enabled.

**Also see: [credential.interactive][credential-interactive]**

---

### GCM_PROVIDER

Define the host provider to use when authenticating.

ID|Provider
-|-
`auto` _(default)_|_\[automatic\]_ ([learn more][autodetect])
`azure-repos`|Azure Repos
`github`|GitHub
`gitlab`|GitLab _(supports OAuth in browser, personal access token and Basic Authentication)_
`generic`|Generic (any other provider not listed above)

Automatic provider selection is based on the remote URL.

This setting is typically used with a scoped URL to map a particular set of
remote URLs to providers, for example to mark a host as a GitHub Enterprise
instance.

#### Example

##### Windows

```batch
SET GCM_PROVIDER=github
```

##### macOS/Linux

```bash
export GCM_PROVIDER=github
```

**Also see: [credential.provider][credential-provider]**

---

### GCM_AUTHORITY _(deprecated)_

> This setting is deprecated and should be replaced by `GCM_PROVIDER` with the
> corresponding provider ID value.
>
> See the [migration guide][migration-guide] for more information.

Select the host provider to use when authenticating by which authority is
supported by the providers.

Authority|Provider(s)
-|-
`auto` _(default)_|_\[automatic\]_
`msa`, `microsoft`, `microsoftaccount`, `aad`, `azure`, `azuredirectory`, `live`, `liveconnect`, `liveid`|Azure Repos _(supports Microsoft Authentication)_
`github`|GitHub _(supports GitHub Authentication)_
`gitlab`|GitLab _(supports OAuth in browser, personal access token and Basic Authentication)_
`basic`, `integrated`, `windows`, `kerberos`, `ntlm`, `tfs`, `sso`|Generic _(supports Basic and Windows Integrated Authentication)_

#### Example

##### Windows

```batch
SET GCM_AUTHORITY=github
```

##### macOS/Linux

```bash
export GCM_AUTHORITY=github
```

**Also see: [credential.authority][credential-authority]**

---

### GCM_GUI_PROMPT

Permit or disable GCM from presenting GUI prompts. If an equivalent terminal/
text-based prompt is available, that will be shown instead.

To disable all interactivity see [GCM_INTERACTIVE][gcm-interactive].

#### Example

##### Windows

```batch
SET GCM_GUI_PROMPT=0
```

##### macOS/Linux

```bash
export GCM_GUI_PROMPT=0
```

Defaults to enabled.

**Also see: [credential.guiPrompt][credential-guiprompt]**

---

### GCM_AUTODETECT_TIMEOUT

Set the maximum length of time, in milliseconds, that GCM should wait for a
network response during host provider auto-detection probing.

See [autodetection][autodetect] for more information.

**Note:** Use a negative or zero value to disable probing altogether.

Defaults to 2000 milliseconds (2 seconds).

#### Example

##### Windows

```batch
SET GCM_AUTODETECT_TIMEOUT=-1
```

##### macOS/Linux

```bash
export GCM_AUTODETECT_TIMEOUT=-1
```

**Also see: [credential.autoDetectTimeout][credential-autodetecttimeout]**

---

### GCM_ALLOW_WINDOWSAUTH

Allow detection of Windows Integrated Authentication (WIA) support for generic
host providers. Setting this value to `false` will prevent the use of WIA and
force a basic authentication prompt when using the Generic host provider.

**Note:** WIA is only supported on Windows.

**Note:** WIA is an umbrella term for NTLM and Kerberos (and Negotiate).

Value|WIA detection
-|-
`true`, `1`, `yes`, `on` _(default)_|Permitted
`false`, `0`, `no`, `off`|Not permitted

#### Example

##### Windows

```batch
SET GCM_ALLOW_WINDOWSAUTH=0
```

##### macOS/Linux

```bash
export GCM_ALLOW_WINDOWSAUTH=0
```

**Also see: [credential.allowWindowsAuth][credential-allowwindowsauth]**

---

### GCM_HTTP_PROXY _(deprecated)_

> This setting is deprecated and should be replaced by the [standard `http.proxy`
> Git configuration option][git-httpproxy].
>
> See the [HTTP proxy configuration][network-http-proxy] for more information.

Configure GCM to use the a proxy for network operations.

**Note:** Git itself does _not_ respect this setting; this affects GCM _only_.

#### Windows

```batch
SET GCM_HTTP_PROXY=http://john.doe:password@proxy.contoso.com
```

#### macOS/Linux

```bash
export GCM_HTTP_PROXY=http://john.doe:password@proxy.contoso.com
```

**Also see: [credential.httpProxy][credential-httpproxy]**

---

### GCM_BITBUCKET_AUTHMODES

Override the available authentication modes presented during Bitbucket
authentication. If this option is not set, then the available authentication
modes will be automatically detected.

**Note:** This setting only applies to Bitbucket.org, and not Server or DC
instances.

**Note:** This setting supports multiple values separated by commas.

Value|Authentication Mode
-|-
_(unset)_|Automatically detect modes
`oauth`|OAuth-based authentication
`basic`|Basic/PAT-based authentication

#### Windows

```batch
SET GCM_BITBUCKET_AUTHMODES="oauth,basic"
```

#### macOS/Linux

```bash
export GCM_BITBUCKET_AUTHMODES="oauth,basic"
```

**Also see: [credential.bitbucketAuthModes][credential-bitbucketauthmodes]**

---

### GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS

Forces GCM to ignore any existing stored Basic Auth or OAuth access tokens and
always run through the process to refresh the credentials before returning them
to Git.

This is especially relevant to OAuth credentials. Bitbucket.org access tokens
expire after 2 hours, after that the refresh token must be used to get a new
access token.

Enabling this option will improve performance when using Oauth2 and interacting
with Bitbucket.org if, on average, commits are done less frequently than every 2
hours.

Enabling this option will decrease performance when using Basic Auth by
requiring the user the re-enter credentials every time.

Value|Refresh Credentials Before Returning
-|-
`true`, `1`, `yes`, `on` |Always
`false`, `0`, `no`, `off`_(default)_|Only when the credentials are found to be invalid

#### Windows

```batch
SET GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS=1
```

#### macOS/Linux

```bash
export GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS=1
```

Defaults to false/disabled.

**Also see: [credential.bitbucketAlwaysRefreshCredentials][credential-bitbucketalwaysrefreshcredentials]**

---

### GCM_GITHUB_AUTHMODES

Override the available authentication modes presented during GitHub
authentication. If this option is not set, then the available authentication
modes will be automatically detected.

**Note:** This setting supports multiple values separated by commas.

Value|Authentication Mode
-|-
_(unset)_|Automatically detect modes
`oauth`|Expands to: `browser, device`
`browser`|OAuth authentication via a web browser _(requires a GUI)_
`device`|OAuth authentication with a device code
`basic`|Basic authentication using username and password
`pat`|Personal Access Token (pat)-based authentication

#### Windows

```batch
SET GCM_GITHUB_AUTHMODES="oauth,basic"
```

#### macOS/Linux

```bash
export GCM_GITHUB_AUTHMODES="oauth,basic"
```

**Also see: [credential.gitHubAuthModes][credential-githubauthmodes]**

---

### GCM_GITLAB_AUTHMODES

Override the available authentication modes presented during GitLab
authentication. If this option is not set, then the available authentication
modes will be automatically detected.

**Note:** This setting supports multiple values separated by commas.

Value|Authentication Mode
-|-
_(unset)_|Automatically detect modes
`browser`|OAuth authentication via a web browser _(requires a GUI)_
`basic`|Basic authentication using username and password
`pat`|Personal Access Token (pat)-based authentication

#### Windows

```batch
SET GCM_GITLAB_AUTHMODES="browser"
```

#### macOS/Linux

```bash
export GCM_GITLAB_AUTHMODES="browser"
```

**Also see: [credential.gitLabAuthModes][credential-gitlabauthmodes]**

---

### GCM_NAMESPACE

Use a custom namespace prefix for credentials read and written in the OS
credential store. Credentials will be stored in the format `{namespace}:{service}`.

Defaults to the value `git`.

#### Windows

```batch
SET GCM_NAMESPACE="my-namespace"
```

#### macOS/Linux

```bash
export GCM_NAMESPACE="my-namespace"
```

**Also see: [credential.namespace][credential-namespace]**

---

### GCM_CREDENTIAL_STORE

Select the type of credential store to use on supported platforms.

Default value on Windows is `wincredman`, on macOS is `keychain`, and is unset
on Linux.

**Note:** For more information about configuring secret stores see the
[credential stores documentation][credential-stores].

Value|Credential Store|Platforms
-|-|-
_(unset)_|Windows: `wincredman`, macOS: `keychain`, Linux: _(none)_|-
`wincredman`|Windows Credential Manager (not available over SSH).|Windows
`dpapi`|DPAPI protected files. Customize the DPAPI store location with [`GCM_DPAPI_STORE_PATH`][gcm-dpapi-store-path]|Windows
`keychain`|macOS Keychain.|macOS
`secretservice`|[freedesktop.org Secret Service API][freedesktop-ss] via [libsecret][libsecret] (requires a graphical interface to unlock secret collections).|Linux
`gpg`|Use GPG to store encrypted files that are compatible with the [`pass` utility][passwordstore] (requires GPG and `pass` to initialize the store).|macOS, Linux
`cache`|Git's built-in [credential cache][git-credential-cache].|Windows, macOS, Linux
`plaintext`|Store credentials in plaintext files (**UNSECURE**). Customize the plaintext store location with [`GCM_PLAINTEXT_STORE_PATH`][gcm-plaintext-store-path].|Windows, macOS, Linux

#### Windows

```batch
SET GCM_CREDENTIAL_STORE="gpg"
```

#### macOS/Linux

```bash
export GCM_CREDENTIAL_STORE="gpg"
```

**Also see: [credential.credentialStore][credential-credentialstore]**

---

### GCM_CREDENTIAL_CACHE_OPTIONS

Pass [options][git-cache-options]
to the Git credential cache when [`GCM_CREDENTIAL_STORE`][gcm-credential-store]
is set to `cache`. This allows you to select a different amount
of time to cache credentials (the default is 900 seconds) by passing
`"--timeout <seconds>"`. Use of other options like `--socket` is untested
and unsupported, but there's no reason it shouldn't work.

Defaults to empty.

#### Windows

```batch
SET GCM_CREDENTIAL_CACHE_OPTIONS="--timeout 300"
```

#### macOS/Linux

```shell
export GCM_CREDENTIAL_CACHE_OPTIONS="--timeout 300"
```

**Also see: [credential.cacheOptions][credential-cacheoptions]**

---

### GCM_PLAINTEXT_STORE_PATH

Specify a custom directory to store plaintext credential files in when
[`GCM_CREDENTIAL_STORE`][gcm-credential-store] is set to `plaintext`.

Defaults to the value `~/.gcm/store` or `%USERPROFILE%\.gcm\store`.

#### Windows

```batch
SETX GCM_PLAINTEXT_STORE_PATH=D:\credentials
```

#### macOS/Linux

```shell
export GCM_PLAINTEXT_STORE_PATH=/mnt/external-drive/credentials
```

**Also see: [credential.plaintextStorePath][credential-plain-text-store]**

---

### GCM_DPAPI_STORE_PATH

Specify a custom directory to store DPAPI protected credential files in when
[`GCM_CREDENTIAL_STORE`][gcm-credential-store] is set to `dpapi`.

Defaults to the value `%USERPROFILE%\.gcm\dpapi_store`.

#### Windows

```batch
SETX GCM_DPAPI_STORE_PATH=D:\credentials
```

**Also see: [credential.dpapiStorePath][credential-dpapi-store-path]**

---

### GCM_GPG_PATH

Specify the path (_including_ the executable name) to the version of `gpg` used
by `pass` (`gpg2` if present, otherwise `gpg`). This is primarily meant to allow
manual resolution of the conflict that occurs on legacy Linux systems with
parallel installs of `gpg` and `gpg2`.

If not specified, GCM defaults to using the version of `gpg2` on the `$PATH`,
falling back on `gpg` if `gpg2` is not found.

#### macOS/Linux

```bash
export GCM_GPG_PATH="/usr/local/bin/gpg2"
```

_No configuration equivalent._

---

### GCM_MSAUTH_FLOW

Specify which authentication flow should be used when performing Microsoft
authentication and an interactive flow is required.

Defaults to `auto`.

**Note:** If [`GCM_MSAUTH_USEBROKER`][gcm-msauth-usebroker] is set to `true`
and the operating system authentication broker is available, all flows will be
delegated to the broker. If both of those things are true, then the value of
`GCM_MSAUTH_FLOW` has no effect.

Value|Authentication Flow
-|-
`auto` _(default)_|Select the best option depending on the current environment and platform.
`embedded`|Show a window with embedded web view control.
`system`|Open the user's default web browser.
`devicecode`|Show a device code.

#### Windows

```batch
SET GCM_MSAUTH_FLOW="devicecode"
```

#### macOS/Linux

```bash
export GCM_MSAUTH_FLOW="devicecode"
```

**Also see: [credential.msauthFlow][credential-msauth-flow]**

---

### GCM_MSAUTH_USEBROKER _(experimental)_

Use the operating system account manager where available.

Defaults to `false`. This default is subject to change in the future.

_**Note:** before you enable this option on Windows, please
[review the details][windows-broker] about what this means to your local Windows
user account._

Value|Description
-|-
`true`|Use the operating system account manager as an authentication broker.
`false` _(default)_|Do not use the broker.

#### Windows

```batch
SET GCM_MSAUTH_USEBROKER="true"
```

#### macOS/Linux

```bash
export GCM_MSAUTH_USEBROKER="false"
```

**Also see: [credential.msauthUseBroker][credential-msauth-usebroker]**

---

### GCM_AZREPOS_CREDENTIALTYPE

Specify the type of credential the Azure Repos host provider should return.

Defaults to the value `pat`.

Value|Description
-|-
`pat` _(default)_|Azure DevOps personal access tokens
`oauth`|Microsoft identity OAuth tokens (AAD or MSA tokens)

More information about Azure Access tokens can be found [here][azure-access-tokens].

#### Windows

```batch
SET GCM_AZREPOS_CREDENTIALTYPE="oauth"
```

#### macOS/Linux

```bash
export GCM_AZREPOS_CREDENTIALTYPE="oauth"
```

**Also see: [credential.azreposCredentialType][credential-azrepos-credential-type]**

[autodetect]: autodetect.md
[azure-access-tokens]: azrepos-users-and-tokens.md
[configuration]: configuration.md
[credential-allowwindowsauth]: environment.md#credentialallowWindowsAuth
[credential-authority]: configuration.md#credentialauthority-deprecated
[credential-autodetecttimeout]: configuration.md#credentialautodetecttimeout
[credential-azrepos-credential-type]: configuration.md#azreposcredentialtype
[credential-bitbucketalwaysrefreshcredentials]: configuration.md#bitbucketAlwaysRefreshCredentials
[credential-bitbucketauthmodes]: configuration.md#credentialbitbucketAuthModes
[credential-cacheoptions]: configuration.md#credentialcacheoptions
[credential-credentialstore]: configuration.md#credentialcredentialstore
[credential-dpapi-store-path]: configuration.md#credentialdpapistorepath
[credential-githubauthmodes]: configuration.md#credentialgitHubAuthModes
[credential-gitlabauthmodes]: configuration.md#credentialgitLabAuthModes
[credential-guiprompt]: configuration.md#credentialguiprompt
[credential-httpproxy]: configuration.md#credentialhttpProxy-deprecated
[credential-interactive]: configuration.md#credentialinteractive
[credential-namespace]: configuration.md#credentialnamespace
[credential-msauth-flow]: configuration.md#credentialmsauthflow
[credential-msauth-usebroker]: configuration.md#credentialmsauthusebroker-experimental
[credential-plain-text-store]: configuration.md#credentialplaintextstorepath
[credential-provider]: configuration.md#credentialprovider
[credential-stores]: credstores.md
[default-values]: enterprise-config.md
[freedesktop-ss]: https://specifications.freedesktop.org/secret-service/
[gcm]: usage.md
[gcm-interactive]: #gcm_interactive
[gcm-credential-store]: #GCM_CREDENTIAL_STORE
[gcm-dpapi-store-path]: #gcm_dpapi_store_path
[gcm-plaintext-store-path]: #gcm_plaintext_store_path
[gcm-msauth-usebroker]: #gcm_msauth_usebroker-experimental
[git-cache-options]: https://git-scm.com/docs/git-credential-cache#_options
[git-credential-cache]: https://git-scm.com/docs/git-credential-cache
[git-httpproxy]: https://git-scm.com/docs/git-config#Documentation/git-config.txt-httpproxy
[network-http-proxy]: netconfig.md#http-proxy
[libsecret]: https://wiki.gnome.org/Projects/Libsecret
[migration-guide]: migration.md#gcm_authority
[passwordstore]: https://www.passwordstore.org/
[windows-broker]: windows-broker.md
