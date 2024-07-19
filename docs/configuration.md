# Configuration options

[Git Credential Manager][usage] works out of the box for most users.

Git Credential Manager (GCM) can be configured using Git's configuration files,
and follows all of the same rules Git does when consuming the files.

Global configuration settings override system configuration settings, and local
configuration settings override global settings; and because the configuration
details exist within Git's configuration files you can use Git's `git config`
utility to set, unset, and alter the setting values. All of GCM's configuration
settings begin with the term `credential`.

GCM honors several levels of settings, in addition to the standard local
\> global \> system tiering Git uses. URL-specific settings or overrides can be
applied to any value in the `credential` namespace with the syntax below.

Additionally, GCM respects several GCM-specific [environment variables][envars]
**which take precedence over configuration options**. System administrators may
also configure [default values][enterprise-config] for many settings used by GCM.

GCM will only be used by Git if it is installed and configured. Use
`git config --global credential.helper manager` to assign GCM as your
credential helper. Use `git config credential.helper` to see the current
configuration.

**Example:**

> `credential.microsoft.visualstudio.com.namespace` is more specific than
> `credential.visualstudio.com.namespace`, which is more specific than
> `credential.namespace`.

In the examples above, the `credential.namespace` setting would affect any
remote repository; the `credential.visualstudio.com.namespace` would affect any
remote repository in the domain, and/or any subdomain (including `www.`) of,
'visualstudio.com'; where as the
`credential.microsoft.visualstudio.com.namespace` setting would only be applied
to remote repositories hosted at 'microsoft.visualstudio.com'.

For the complete list of settings GCM understands, see the list below.

## Available settings

### credential.interactive

Permit or disable GCM from interacting with the user (showing GUI or TTY
prompts). If interaction is required but has been disabled, an error is returned.

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

```shell
git config --global credential.interactive false
```

Defaults to enabled.

**Also see: [GCM_INTERACTIVE][gcm-interactive]**

---

### credential.trace

Enables trace logging of all activities.
Configuring Git and GCM to trace to the same location is often desirable, and
GCM is compatible and cooperative with `GIT_TRACE`.

#### Example

```shell
git config --global credential.trace /tmp/git.log
```

If the value of `credential.trace` is a full path to a file in an existing
directory, logs are appended to the file.

If the value of `credential.trace` is `true` or `1`, logs are written to
standard error.

Defaults to disabled.

**Also see: [GCM_TRACE][gcm-trace]**

---

### credential.traceSecrets

Enables tracing of secret and sensitive information, which is by default masked
in trace output. Requires that `credential.trace` is also enabled.

#### Example

```shell
git config --global credential.traceSecrets true
```

If the value of `credential.traceSecrets` is `true` or `1`, trace logs will include
secret information.

Defaults to disabled.

**Also see: [GCM_TRACE_SECRETS][gcm-trace-secrets]**

---

### credential.traceMsAuth

Enables inclusion of Microsoft Authentication library (MSAL) logs in GCM trace
output. Requires that `credential.trace` is also enabled.

#### Example

```shell
git config --global credential.traceMsAuth true
```

If the value of `credential.traceMsAuth` is `true` or `1`, trace logs will
include verbose MSAL logs.

Defaults to disabled.

**Also see: [GCM_TRACE_MSAUTH][gcm-trace-msauth]**

---

### credential.debug

Pauses execution of GCM at launch to wait for a debugger to be attached.

#### Example

```shell
git config --global credential.debug true
```

Defaults to disabled.

**Also see: [GCM_DEBUG][gcm-debug]**

---

### credential.provider

Define the host provider to use when authenticating.

ID|Provider
-|-
`auto` _(default)_|_\[automatic\]_ ([learn more][autodetect])
`azure-repos`|Azure Repos
`github`|GitHub
`bitbucket`|Bitbucket
`gitlab`|GitLab _(supports OAuth in browser, personal access token and Basic Authentication)_
`generic`|Generic (any other provider not listed above)

Automatic provider selection is based on the remote URL.

This setting is typically used with a scoped URL to map a particular set of
remote URLs to providers, for example to mark a host as a GitHub Enterprise
instance.

#### Example

```shell
git config --global credential.ghe.contoso.com.provider github
```

**Also see: [GCM_PROVIDER][gcm-provider]**

---

### credential.authority _(deprecated)_

> This setting is deprecated and should be replaced by `credential.provider`
> with the corresponding provider ID value.
>
> See the [migration guide][provider-migrate] for more information.

Select the host provider to use when authenticating by which authority is
supported by the providers.

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

**Also see: [GCM_AUTHORITY][gcm-authority]**

---

### credential.guiPrompt

Permit or disable GCM from presenting GUI prompts. If an equivalent terminal/
text-based prompt is available, that will be shown instead.

To disable all interactivity see [credential.interactive][credential-interactive].

#### Example

```shell
git config --global credential.guiPrompt false
```

Defaults to enabled.

**Also see: [GCM_GUI_PROMPT][gcm-gui-prompt]**

---

### credential.guiSoftwareRendering

Force the use of software rendering for GUI prompts.

This is currently only applicable on Windows.

#### Example

```shell
git config --global credential.guiSoftwareRendering true
```

Defaults to false (use hardware acceleration where available).

> [!NOTE]
> Windows on ARM devices defaults to using software rendering to work around a
> known Avalonia issue: <https://github.com/AvaloniaUI/Avalonia/issues/10405>

**Also see: [GCM_GUI_SOFTWARE_RENDERING][gcm-gui-software-rendering]**

---

### credential.autoDetectTimeout

Set the maximum length of time, in milliseconds, that GCM should wait for a
network response during host provider auto-detection probing.

See [auto-detection][auto-detection] for more information.

**Note:** Use a negative or zero value to disable probing altogether.

Defaults to 2000 milliseconds (2 seconds).

#### Example

```shell
git config --global credential.autoDetectTimeout -1
```

**Also see: [GCM_AUTODETECT_TIMEOUT][gcm-autodetect-timeout]**

---

### credential.allowWindowsAuth

Allow detection of Windows Integrated Authentication (WIA) support for generic
host providers. Setting this value to `false` will prevent the use of WIA and
force a basic authentication prompt when using the Generic host provider.

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

**Also see: [GCM_ALLOW_WINDOWSAUTH][gcm-allow-windowsauth]**

---

### credential.httpProxy _(deprecated)_

> This setting is deprecated and should be replaced by the
> [standard `http.proxy` Git configuration option][git-config-http-proxy].
>
> See [HTTP Proxy][http-proxy] for more information.

Configure GCM to use the a proxy for network operations.

**Note:** Git itself does _not_ respect this setting; this affects GCM _only_.

#### Example

```shell
git config --global credential.httpsProxy http://john.doe:password@proxy.contoso.com
```

**Also see: [GCM_HTTP_PROXY][gcm-http-proxy]**

---

### credential.bitbucketAuthModes

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

#### Example

```shell
git config --global credential.bitbucketAuthModes "oauth,basic"
```

**Also see: [GCM_BITBUCKET_AUTHMODES][gcm-bitbucket-authmodes]**

---

### credential.bitbucketAlwaysRefreshCredentials

Forces GCM to ignore any existing stored Basic Auth or OAuth access tokens and
always run through the process to refresh the credentials before returning them
to Git.

This is especially relevant to OAuth credentials. Bitbucket.org access tokens
expire after 2 hours, after that the refresh token must be used to get a new
access token.

Enabling this option will improve performance when using Oauth2 and interacting
with Bitbucket.org if, on average, commits are done less frequently than every
2 hours.

Enabling this option will decrease performance when using Basic Auth by
requiring the user the re-enter credentials every time.

Value|Refresh Credentials Before Returning
-|-
`true`, `1`, `yes`, `on` |Always
`false`, `0`, `no`, `off`_(default)_|Only when the credentials are found to be invalid

#### Example

```shell
git config --global credential.bitbucketAlwaysRefreshCredentials true
```

Defaults to false/disabled.

**Also see: [GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS][gcm-bitbucket-always-refresh-credentials]**

---

### credential.bitbucketValidateStoredCredentials

Forces GCM to validate any stored credentials before returning them to Git. It
does this by calling a REST API resource that requires authentication.

Disabling this option reduces the HTTP traffic within GCM when it is retrieving
credentials. This may improve user performance, but will increase the number of
times Git remote calls fail to authenticate with the host and therefore require
the user to re-try the Git remote call.

Enabling this option helps ensure Git is always provided with valid credentials.

Value|Validate credentials
-|-
`true`, `1`, `yes`, `on`_(default)_|Always
`false`, `0`, `no`, `off`|Never

#### Example

```shell
git config --global credential.bitbucketValidateStoredCredentials true
```

Defaults to true/enabled.

**Also see: [GCM_BITBUCKET_VALIDATE_STORED_CREDENTIALS](environment.md#GCM_BITBUCKET_VALIDATE_STORED_CREDENTIALS)**

---

### credential.bitbucketDataCenterOAuthClientId

To use OAuth with Bitbucket DC it is necessary to create an external, incoming
[AppLink](https://confluence.atlassian.com/bitbucketserver/configure-an-incoming-link-1108483657.html).

It is then necessary to configure the local GCM installation with both the OAuth
[ClientId](configuration.md#credential.bitbucketDataCenterOAuthClientId) and
[ClientSecret](configuration.md#credential.bitbucketDataCenterOauthSecret) from
the AppLink.

#### Example

```shell
git config --global credential.bitbucketDataCenterOAuthClientId 1111111111111111111
```

Defaults to undefined.

**Also see: [GCM_BITBUCKET_DATACENTER_CLIENTID](environment.md#GCM_BITBUCKET_DATACENTER_CLIENTID)**

---

### credential.bitbucketDataCenterOAuthClientSecret

To use OAuth with Bitbucket DC it is necessary to create an external, incoming
[AppLink](https://confluence.atlassian.com/bitbucketserver/configure-an-incoming-link-1108483657.html).

It is then necessary to configure the local GCM installation with both the OAuth
[ClientId](configuration.md#credential.bitbucketDataCenterOAuthClientId) and
[ClientSecret](configuration.md#credential.bitbucketDataCenterOauthSecret)
from the AppLink.

#### Example

```shell
git config --global credential.bitbucketDataCenterOAuthClientSecret 222222222222222222222
```

Defaults to undefined.

**Also see: [GCM_BITBUCKET_DATACENTER_CLIENTSECRET](environment.md#GCM_BITBUCKET_DATACENTER_CLIENTSECRET)**

---

### credential.gitHubAccountFiltering

Enable or disable automatic account filtering for GitHub based on server hints
when there are multiple available accounts. This setting is only applicable to
GitHub.com with [Enterprise Managed Users][github-emu].

Value|Description
-|-
`true` _(default)_|Filter available accounts based on server hints.
`false`|Show all available accounts.

#### Example

```shell
git config --global credential.gitHubAccountFiltering "false"
```

**Also see: [GCM_GITHUB_ACCOUNTFILTERING][gcm-github-accountfiltering]**

---

### credential.gitHubAuthModes

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

#### Example

```shell
git config --global credential.gitHubAuthModes "oauth,basic"
```

**Also see: [GCM_GITHUB_AUTHMODES][gcm-github-authmodes]**

---

### credential.gitLabAuthModes

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

#### Example

```shell
git config --global credential.gitLabAuthModes "browser"
```

**Also see: [GCM_GITLAB_AUTHMODES][gcm-gitlab-authmodes]**

---

### credential.namespace

Use a custom namespace prefix for credentials read and written in the OS
credential store. Credentials will be stored in the format
`{namespace}:{service}`.

Defaults to the value `git`.

#### Example

```shell
git config --global credential.namespace "my-namespace"
```

**Also see: [GCM_NAMESPACE][gcm-namespace]**

---

### credential.credentialStore

Select the type of credential store to use on supported platforms.

Default value on Windows is `wincredman`, on macOS is `keychain`, and is unset
on Linux.

**Note:** See more information about configuring secret stores in
[cred-stores][cred-stores].

Value|Credential Store|Platforms
-|-|-
_(unset)_|Windows: `wincredman`, macOS: `keychain`, Linux: _(none)_|-
`wincredman`|Windows Credential Manager (not available over SSH).|Windows
`dpapi`|DPAPI protected files. Customize the DPAPI store location with [credential.dpapiStorePath][credential-dpapistorepath]|Windows
`keychain`|macOS Keychain.|macOS
`secretservice`|[freedesktop.org Secret Service API][freedesktop-ss] via [libsecret][libsecret] (requires a graphical interface to unlock secret collections).|Linux
`gpg`|Use GPG to store encrypted files that are compatible with the [pass][pass] (requires GPG and `pass` to initialize the store).|macOS, Linux
`cache`|Git's built-in [credential cache][credential-cache].|macOS, Linux
`plaintext`|Store credentials in plaintext files (**UNSECURE**). Customize the plaintext store location with [`credential.plaintextStorePath`][credential-plaintextstorepath].|Windows, macOS, Linux

#### Example

```bash
git config --global credential.credentialStore gpg
```

**Also see: [GCM_CREDENTIAL_STORE][gcm-credential-store]**

---

### credential.cacheOptions

Pass [options][cache-options] to the Git credential cache when
[`credential.credentialStore`][credential-credentialstore]
is set to `cache`. This allows you to select a different amount
of time to cache credentials (the default is 900 seconds) by passing
`"--timeout <seconds>"`. Use of other options like `--socket` is untested
and unsupported, but there's no reason it shouldn't work.

Defaults to empty.

#### Example

```shell
git config --global credential.cacheOptions "--timeout 300"
```

**Also see: [GCM_CREDENTIAL_CACHE_OPTIONS][gcm-credential-cache-options]**

---

### credential.plaintextStorePath

Specify a custom directory to store plaintext credential files in when
[`credential.credentialStore`][credential-credentialstore] is set to `plaintext`.

Defaults to the value `~/.gcm/store` or `%USERPROFILE%\.gcm\store`.

#### Example

```shell
git config --global credential.plaintextStorePath /mnt/external-drive/credentials
```

**Also see: [GCM_PLAINTEXT_STORE_PATH][gcm-plaintext-store-path]**

---

### credential.dpapiStorePath

Specify a custom directory to store DPAPI protected credential files in when
[`credential.credentialStore`][credential-credentialstore] is set to `dpapi`.

Defaults to the value `%USERPROFILE%\.gcm\dpapi_store`.

#### Example

```batch
git config --global credential.dpapiStorePath D:\credentials
```

**Also see: [GCM_DPAPI_STORE_PATH][gcm-dpapi-store-path]**

---

### credential.msauthFlow

Specify which authentication flow should be used when performing Microsoft
authentication and an interactive flow is required.

Defaults to `auto`.

**Note:** If [`credential.msauthUseBroker`][credential-msauthusebroker] is set
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

**Also see: [GCM_MSAUTH_FLOW][gcm-msauth-flow]**

---

### credential.msauthUseBroker _(experimental)_

Use the operating system account manager where available.

Defaults to `false`. In certain cloud hosted environments when using a work or
school account, such as [Microsoft DevBox][devbox], the default is `true`.

These defaults are subject to change in the future.

_**Note:** before you enable this option on Windows, please review the
[Windows Broker][wam] details for what this means to your local Windows user
account._

Value|Description
-|-
`true`|Use the operating system account manager as an authentication broker.
`false` _(default)_|Do not use the broker.

#### Example

```shell
git config --global credential.msauthUseBroker true
```

**Also see: [GCM_MSAUTH_USEBROKER][gcm-msauth-usebroker]**

---

### credential.msauthUseDefaultAccount _(experimental)_

Use the current operating system account by default when the broker is enabled.

Defaults to `false`. In certain cloud hosted environments when using a work or
school account, such as [Microsoft DevBox][devbox], the default is `true`.

These defaults are subject to change in the future.

Value|Description
-|-
`true`|Use the current operating system account by default.
`false` _(default)_|Do not assume any account to use by default.

#### Example

```shell
git config --global credential.msauthUseDefaultAccount true
```

**Also see: [GCM_MSAUTH_USEDEFAULTACCOUNT][gcm-msauth-usedefaultaccount]**

---

### credential.useHttpPath

Tells Git to pass the entire repository URL, rather than just the hostname, when
calling out to a credential provider. (This setting
[comes from Git itself][use-http-path], not GCM.)

Defaults to `false`.

**Note:** GCM sets this value to `true` for `dev.azure.com` (Azure Repos) hosts
after installation by default.

This is because `dev.azure.com` alone is not enough information to determine the
correct Azure authentication authority - we require a part of the path. The
fallout of this is that for `dev.azure.com` remote URLs we do not support
storing credentials against the full-path. We always store against the
`dev.azure.com/org-name` stub.

In order to use Azure Repos and store credentials against a full-path URL, you
must use the `org-name.visualstudio.com` remote URL format instead.

Value|Git Behavior
-|-
`false` _(default)_|Git will use only `user` and `hostname` to look up credentials.
`true`|Git will use the full repository URL to look up credentials.

#### Example

On Windows using GitHub, for a user whose login is `alice`, and with
`credential.useHttpPath` set to `false` (or not set), the following remote URLs
will use the same credentials:

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

Under the same user but with `credential.useHttpPath` set to `true`, these
credentials would be used:

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

Defaults to the value `pat`. In certain cloud hosted environments when using a
work or school account, such as [Microsoft DevBox][devbox], the default value is
`oauth`.

Value|Description
-|-
`pat`|Azure DevOps personal access tokens
`oauth`|Microsoft identity OAuth tokens (AAD or MSA tokens)

Here is more information about [Azure Access tokens][azure-tokens].

#### Example

```shell
git config --global credential.azreposCredentialType oauth
```

**Also see: [GCM_AZREPOS_CREDENTIALTYPE][gcm-azrepos-credentialtype]**

---

### credential.azreposManagedIdentity

Use a [Managed Identity][managed-identity] to authenticate with Azure Repos.

The value `system` will tell GCM to use the system-assigned Managed Identity.

To specify a user-assigned Managed Identity, use the format `id://{clientId}`
where `{clientId}` is the client ID of the Managed Identity. Alternatively any
GUID-like value will also be interpreted as a user-assigned Managed Identity
client ID.

To specify a Managed Identity associated with an Azure resource, you can use the
format `resource://{resourceId}` where `{resourceId}` is the ID of the resource.

For more information about managed identities, see the Azure DevOps
[documentation][azrepos-sp-mid].

Value|Description
-|-
`system`|System-Assigned Managed Identity
`[guid]`|User-Assigned Managed Identity with the specified client ID
`id://[guid]`|User-Assigned Managed Identity with the specified client ID
`resource://[guid]`|User-Assigned Managed Identity for the associated resource

```shell
git config --global credential.azreposManagedIdentity "id://11111111-1111-1111-1111-111111111111"
```

**Also see: [GCM_AZREPOS_MANAGEDIDENTITY][gcm-azrepos-credentialmanagedidentity]**

---

### credential.azreposServicePrincipal

Specify the client and tenant IDs of a [service principal][service-principal]
to use when performing Microsoft authentication for Azure Repos.

The value of this setting should be in the format: `{tenantId}/{clientId}`.

You must also set at least one authentication mechanism if you set this value:

- [credential.azreposServicePrincipalSecret][credential-azrepos-sp-secret]
- [credential.azreposServicePrincipalCertificateThumbprint][credential-azrepos-sp-cert-thumbprint]
- [credential.azreposServicePrincipalCertificateSendX5C][credential-azrepos-sp-cert-x5c]

For more information about service principals, see the Azure DevOps
[documentation][azrepos-sp-mid].

#### Example

```shell
git config --global credential.azreposServicePrincipal "11111111-1111-1111-1111-111111111111/22222222-2222-2222-2222-222222222222"
```

**Also see: [GCM_AZREPOS_SERVICE_PRINCIPAL][gcm-azrepos-service-principal]**

---

### credential.azreposServicePrincipalSecret

Specifies the client secret for the [service principal][service-principal] when
performing Microsoft authentication for Azure Repos with
[credential.azreposServicePrincipalSecret][credential-azrepos-sp] set.

#### Example

```shell
git config --global credential.azreposServicePrincipalSecret "da39a3ee5e6b4b0d3255bfef95601890afd80709"
```

**Also see: [GCM_AZREPOS_SP_SECRET][gcm-azrepos-sp-secret]**

---

### credential.azreposServicePrincipalCertificateThumbprint

Specifies the thumbprint of a certificate to use when authenticating as a
[service principal][service-principal] for Azure Repos when
[GCM_AZREPOS_SERVICE_PRINCIPAL][credential-azrepos-sp] is set.

#### Example

```shell
git config --global credential.azreposServicePrincipalCertificateThumbprint "9b6555292e4ea21cbc2ebd23e66e2f91ebbe92dc"
```

**Also see: [GCM_AZREPOS_SP_CERT_THUMBPRINT][gcm-azrepos-sp-cert-thumbprint]**

---

### credential.azreposServicePrincipalCertificateSendX5C

When using a certificate for [service principal][service-principal] authentication, this configuration
specifies whether the X5C claim should be should be sent to the STS. Sending the x5c
enables application developers to achieve easy certificate rollover in Azure AD:
this method will send the public certificate to Azure AD along with the token request,
so that Azure AD can use it to validate the subject name based on a trusted issuer
policy. This saves the application admin from the need to explicitly manage the
certificate rollover. For details see [https://aka.ms/msal-net-sni](https://aka.ms/msal-net-sni).

#### Example

```shell
git config --global credential.azreposServicePrincipalCertificateSendX5C true
```
**Also see: [GCM_AZREPOS_SP_CERT_SEND_X5C][gcm-azrepos-sp-cert-x5c]**

---

### trace2.normalTarget

Turns on Trace2 Normal Format tracing - see [Git's Trace2 Normal Format
documentation][trace2-normal-docs] for more details.

#### Example

```shell
git config --global trace2.normalTarget true
```

If the value of `trace2.normalTarget` is a full path to a file in an existing
directory, logs are appended to the file.

If the value of `trace2.normalTarget` is `true` or `1`, logs are written to
standard error.

Defaults to disabled.

**Also see: [GIT_TRACE2][trace2-normal-env]**

---

### trace2.eventTarget

Turns on Trace2 Event Format tracing - see [Git's Trace2 Event Format
documentation][trace2-event-docs] for more details.

#### Example

```shell
git config --global trace2.eventTarget true
```

If the value of `trace2.eventTarget` is a full path to a file in an existing
directory, logs are appended to the file.

If the value of `trace2.eventTarget` is `true` or `1`, logs are written to
standard error.

Defaults to disabled.

**Also see: [GIT_TRACE2_EVENT][trace2-event-env]**

---

### trace2.perfTarget

Turns on Trace2 Performance Format tracing - see [Git's Trace2 Performance
Format documentation][trace2-performance-docs] for more details.

#### Example

```shell
git config --global trace2.perfTarget true
```

If the value of `trace2.perfTarget` is a full path to a file in an existing
directory, logs are appended to the file.

If the value of `trace2.perfTarget` is `true` or `1`, logs are written to
standard error.

Defaults to disabled.

**Also see: [GIT_TRACE2_PERF][trace2-performance-env]**

[auto-detection]: autodetect.md
[azure-tokens]: azrepos-users-and-tokens.md
[use-http-path]: https://git-scm.com/docs/gitcredentials/#Documentation/gitcredentials.txt-useHttpPath
[credential-credentialstore]: #credentialcredentialstore
[credential-dpapistorepath]: #credentialdpapistorepath
[credential-interactive]: #credentialinteractive
[credential-msauthusebroker]: #credentialmsauthusebroker-experimental
[credential-plaintextstorepath]: #credentialplaintextstorepath
[credential-cache]: https://git-scm.com/docs/git-credential-cache
[cred-stores]: credstores.md
[devbox]: https://azure.microsoft.com/en-us/products/dev-box
[enterprise-config]: enterprise-config.md
[envars]: environment.md
[freedesktop-ss]: https://specifications.freedesktop.org/secret-service/
[gcm-allow-windowsauth]: environment.md#GCM_ALLOW_WINDOWSAUTH
[gcm-authority]: environment.md#GCM_AUTHORITY-deprecated
[gcm-autodetect-timeout]: environment.md#GCM_AUTODETECT_TIMEOUT
[gcm-azrepos-credentialtype]: environment.md#GCM_AZREPOS_CREDENTIALTYPE
[gcm-azrepos-credentialmanagedidentity]: environment.md#GCM_AZREPOS_MANAGEDIDENTITY
[gcm-bitbucket-always-refresh-credentials]: environment.md#GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS
[gcm-bitbucket-authmodes]: environment.md#GCM_BITBUCKET_AUTHMODES
[gcm-credential-cache-options]: environment.md#GCM_CREDENTIAL_CACHE_OPTIONS
[gcm-credential-store]: environment.md#GCM_CREDENTIAL_STORE
[gcm-debug]: environment.md#GCM_DEBUG
[gcm-dpapi-store-path]: environment.md#GCM_DPAPI_STORE_PATH
[gcm-github-accountfiltering]: environment.md#GCM_GITHUB_ACCOUNTFILTERING
[gcm-github-authmodes]: environment.md#GCM_GITHUB_AUTHMODES
[gcm-gitlab-authmodes]:environment.md#GCM_GITLAB_AUTHMODES
[gcm-gui-prompt]: environment.md#GCM_GUI_PROMPT
[gcm-gui-software-rendering]: environment.md#GCM_GUI_SOFTWARE_RENDERING
[gcm-http-proxy]: environment.md#GCM_HTTP_PROXY-deprecated
[gcm-interactive]: environment.md#GCM_INTERACTIVE
[gcm-msauth-flow]: environment.md#GCM_MSAUTH_FLOW
[gcm-msauth-usebroker]: environment.md#GCM_MSAUTH_USEBROKER-experimental
[gcm-msauth-usedefaultaccount]: environment.md#GCM_MSAUTH_USEDEFAULTACCOUNT-experimental
[gcm-namespace]: environment.md#GCM_NAMESPACE
[gcm-plaintext-store-path]: environment.md#GCM_PLAINTEXT_STORE_PATH
[gcm-provider]: environment.md#GCM_PROVIDER
[gcm-trace]: environment.md#GCM_TRACE
[gcm-trace-secrets]: environment.md#GCM_TRACE_SECRETS
[gcm-trace-msauth]: environment.md#GCM_TRACE_MSAUTH
[github-emu]: https://docs.github.com/en/enterprise-cloud@latest/admin/identity-and-access-management/using-enterprise-managed-users-for-iam/about-enterprise-managed-users
[usage]: usage.md
[git-config-http-proxy]: https://git-scm.com/docs/git-config#Documentation/git-config.txt-httpproxy
[http-proxy]: netconfig.md#http-proxy
[autodetect]: autodetect.md
[libsecret]: https://wiki.gnome.org/Projects/Libsecret
[managed-identity]: https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview
[provider-migrate]: migration.md#gcm_authority
[cache-options]: https://git-scm.com/docs/git-credential-cache#_options
[pass]: https://www.passwordstore.org/
[trace2-normal-docs]: https://git-scm.com/docs/api-trace2#_the_normal_format_target
[trace2-normal-env]: environment.md#GIT_TRACE2
[trace2-event-docs]: https://git-scm.com/docs/api-trace2#_the_event_format_target
[trace2-event-env]: environment.md#GIT_TRACE2_EVENT
[trace2-performance-docs]: https://git-scm.com/docs/api-trace2#_the_performance_format_target
[trace2-performance-env]: environment.md#GIT_TRACE2_PERF
[wam]: windows-broker.md
[service-principal]: https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals
[azrepos-sp-mid]: https://learn.microsoft.com/en-us/azure/devops/integrate/get-started/authentication/service-principal-managed-identity
[credential-azrepos-sp]: #credentialazreposserviceprincipal
[credential-azrepos-sp-secret]: #credentialazreposserviceprincipalsecret
[credential-azrepos-sp-cert-thumbprint]: #credentialazreposserviceprincipalcertificatethumbprint
[credential-azrepos-sp-cert-x5c]: #credentialazreposserviceprincipalcertificatesendx5c
[gcm-azrepos-service-principal]: environment.md#GCM_AZREPOS_SERVICE_PRINCIPAL
[gcm-azrepos-sp-secret]: environment.md#GCM_AZREPOS_SP_SECRET
[gcm-azrepos-sp-cert-thumbprint]: environment.md#GCM_AZREPOS_SP_CERT_THUMBPRINT
[gcm-azrepos-sp-cert-x5c]: environment.md#GCM_AZREPOS_SP_CERT_SEND_X5C
