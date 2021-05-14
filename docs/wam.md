# Web Account Manager integration

## Running as administrator

The Windows broker ("WAM") makes heavy use of
[COM](https://docs.microsoft.com/en-us/windows/win32/com/the-component-object-model),
an IPC and RPC technology built-in to the Windows operating system. In order to
integrate with WAM, Git Credential Manager and the underlying
[Microsoft Authentication Library (MSAL)](https://aka.ms/msal-net)
must use COM interfaces and remote procedure calls (RPC).

When you run Git Credential Manager as an elevated process, such as when you run
a `git` command from an Administrator command-prompt or perform Git operations
from Visual Studio running as Administrator, some of the calls made between GCM
and WAM may fail due to differing process security levels.

If you have enabled using the broker and GCM detects it is running in an
elevated process, it will automatically attempt to modify the COM security
settings for the running process so that GCM and WAM can work together.

However, this automatic process security change is not guaranteed to succeed
depending on various external factors like registry or system-wide COM settings.
If GCM fails to modify the process COM security settings, a warning message is
printed and use of the broker is disabled for this invocation of GCM:

```text
warning: broker initialization failed
Failed to set COM process security to allow Windows broker from an elevated process (0x80010119).
See https://aka.ms/gcmcore-wamadmin for more information.
```

### Possible solutions

In order to fix the problem there are a few options:

1. Do not run Git or Git Credential Manager as elevated processes.
2. Disable the broker by setting the
   [`GCM_MSAUTH_USEBROKER`](environment.md#gcm_msauth_usebroker)
   environment variable or the
   [`credential.msauthUseBroker`](configuration.md#credentialmsauthusebroker)
   Git configuration setting to `false`.
