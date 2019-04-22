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
