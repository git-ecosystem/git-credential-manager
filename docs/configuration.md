# Configuration options

[Git Credential Manager Core](usage.md) works out of the box for most users.

Git Credential Manager Core (GCM Core) can be configured using Git's configuration files, and follows all of the same rules Git does when consuming the files.
Global configuration settings override system configuration settings, and local configuration settings override global settings; and because the configuration details exist within Git's configuration files you can use Git's `git config` utility to set, unset, and alter the setting values. All of GCM Core's configuration settings begin with the term `credential`.

GCM Core honors several levels of settings, in addition to the standard local \> global \> system tiering Git uses.
URL-specific settings or overrides can be applied to any value in the `credential` namespace with the syntax below.

Additionally, GCM Core respects several GCM-specific [environment variables](environment.md) **which take precedence over configuration options.**

GCM Core will only be used by Git if it is installed and configured (`credential.helper`).

**Example:**

> `credential.microsoft.visualstudio.com.namespace` is more specific than `credential.visualstudio.com.namespace`, which is more specific than `credential.namespace`.

In the examples above, the `credential.namespace` setting would affect any remote repository; the `credential.visualstudio.com.namespace` would affect any remote repository in the domain, and/or any subdomain (including `www.`) of, 'visualstudio.com'; where as the `credential.microsoft.visualstudio.com.namespace` setting would only be applied to remote repositories hosted at 'microsoft.visualstudio.com'.

For the complete list of settings GCM Core understands, see the list below.

## Available settings

### credential.interactive

Permit or disable GCM Core from interacting with the user (showing GUI or TTY prompts). If interaction is required but has been disabled, an error is returned.

This can be helpful when using GCM Core in headless and unattended environments, such as build servers, where it would be preferable to fail than to hang indefinitely waiting for a non-existent user.

To disable interactivity set this to `false` or `0`.

#### Compatibility

In previous versions of GCM this setting had a different behavior and accepted other values.
The following table summarizes the change in behavior and the mapping of older values such as `never`:

Value(s)|Old meaning|New meaning
-|-|-
`auto`|Prompt if required – use cached credentials if possible|_(unchanged)_
`never`,<br/>`false`| Never prompt – fail if interaction is required|_(unchanged)_
`always`,<br/>`force`,<br/>`true`|Always prompt – don't use cached credentials|Prompt if required (same as the old `auto` value)

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
`auto` _(default)_|_\[automatic\]_
`azure-repos`|Azure Repos
`github`|GitHub
`bitbucket`|Bitbucket
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
> Click [here](https://aka.ms/gcmcore-authority) for more information.

Select the host provider to use when authenticating by which authority is supported by the providers.

Authority|Provider(s)
-|-
`auto` _(default)_|_\[automatic\]_
`msa`, `microsoft`, `microsoftaccount`,<br/>`aad`, `azure`, `azuredirectory`,</br>`live`, `liveconnect`, `liveid`|Azure Repos<br/>_(supports Microsoft Authentication)_
`github`|GitHub<br/>_(supports GitHub Authentication)_
`bitbucket`|Bitbucket.org<br/>_(supports Basic Authentication and OAuth)_<br/>Bitbucket Server<br/>_(supports Basic Authentication)_
`basic`, `integrated`, `windows`, `kerberos`, `ntlm`,<br/>`tfs`, `sso`|Generic<br/>_(supports Basic and Windows Integrated Authentication)_

#### Example

```shell
git config --global credential.ghe.contoso.com.authority github
```

**Also see: [GCM_AUTHORITY](environment.md#GCM_AUTHORITY-deprecated)**

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
> Click [here](https://aka.ms/gcmcore-httpproxy) for more information.

Configure GCM Core to use the a proxy for network operations.

**Note:** Git itself does _not_ respect this setting; this affects GCM _only_.

#### Example

```shell
git config --global credential.httpsProxy http://john.doe:password@proxy.contoso.com
```

**Also see: [GCM_HTTP_PROXY](environment.md#GCM_HTTP_PROXY-deprecated)**

---

### credential.gitHubAuthModes

Override the available authentication modes presented during GitHub authentication.
If this option is not set, then the available authentication modes will be automatically detected.

**Note:** This setting supports multiple values separated by spaces.

Value|Authentication Mode
-|-
_(unset)_|Automatically detect modes
`oauth`|OAuth-based authentication
`basic`|Basic/PAT-based authentication

#### Example

```shell
git config --global credential.gitHubAuthModes "oauth basic"
```

**Also see: [GCM_GITHUB_AUTHMODES](environment.md#GCM_GITHUB_AUTHMODES)**
