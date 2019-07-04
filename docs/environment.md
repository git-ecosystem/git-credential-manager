# Environment variables

[Git Credential Manager Core](usage.md) work out of the box for most users. Configuration options are available to customize or tweak behavior.

Git Credential Manager Core (GCM Core) can be configured using environment variables. **Environment variables take precedence over [configuration](configuration.md) options.**

For the complete list of environment variables GCM Core understands, see the list below.

## Available settings

### GCM_TRACE

Enables trace logging of all activities.
Configuring Git and GCM to trace to the same location is often desirable, and GCM is compatible and cooperative with `GIT_TRACE`.

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

If the value of `GCM_TRACE` is a full path to a file in an existing directory, logs are appended to the file.

If the value of `GCM_TRACE` is `true` or `1`, logs are written to standard error.

Defaults to tracing disabled.

_No configuration equivalent._

---

### GCM_TRACE_SECRETS

Enables tracing of secret and senstive information, which is by default masked in trace output.
Requires that `GCM_TRACE` is also enabled.

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

If the value of `GCM_TRACE_SECRETS` is `true` or `1`, trace logs will include secret information.

Defaults to disabled.

_No configuration equivalent._

---

### GCM_TRACE_MSAUTH

Enables inclusion of Microsoft Authentication libraries (ADAL, MSAL) logs in GCM trace output.
Requires that `GCM_TRACE` is also enabled.

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

If the value of `GCM_TRACE_MSAUTH` is `true` or `1`, trace logs will include verbose ADAL/MSAL logs.

Defaults to disabled.

_No configuration equivalent._

---

### GCM_DEBUG

Pauses execution of GCM Core at launch to wait for a debugger to be attached.

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
### GCM_PROVIDER

Define the host provider to use when authenticating.

ID|Provider
-|-
`auto` _(default)_|_\[automatic\]_
`azure-repos`|Azure Repos
`github`|GitHub
`generic`|Generic (any other provider not listed above)

Automatic provider selection is based on the remote URL.

This setting is typically used with a scoped URL to map a particular set of remote URLs to providers, for example to mark a host as a GitHub Enterprise instance.

#### Example

##### Windows

```batch
SET GCM_PROVIDER=github
```

##### macOS/Linux

```bash
export GCM_PROVIDER=github
```

**Also see: [credential.provider](configuration.md#credentialprovider)**

---

### GCM_AUTHORITY _(deprecated)_

> This setting is deprecated and should be replaced by `GCM_PROVIDER` with the corresponding provider ID value.
>
> Click [here](https://aka.ms/gcmcore-authority) for more information.

Select the host provider to use when authenticating by which authority is supported by the providers.

Authority|Provider(s)
-|-
`auto` _(default)_|_\[automatic\]_
`msa`, `microsoft`, `microsoftaccount`,<br/>`aad`, `azure`, `azuredirectory`,</br>`live`, `liveconnect`, `liveid`|Azure Repos<br/>_(supports Microsoft Authentication)_
`github`|GitHub<br/>_(supports GitHub Authentication)_
`basic`, `integrated`, `windows`, `kerberos`, `ntlm`,<br/>`tfs`, `sso`|Generic<br/>_(supports Basic and Windows Integrated Authentication)_

#### Example

##### Windows

```batch
SET GCM_AUTHORITY=github
```

##### macOS/Linux

```bash
export GCM_AUTHORITY=github
```

**Also see: [credential.authority](configuration.md#credentialauthority-deprecated)**

---

### GCM_ALLOW_WINDOWSAUTH

Allow detection of Windows Integrated Authentication (WIA) support for generic host providers. Setting this value to `false` will prevent the use of WIA and force a basic authentication prompt when using the Generic host provider.

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

**Also see: [credential.allowWindowsAuth](environment.md#credentialallowWindowsAuth)**
