# Windows Subsystem for Linux (WSL)

GCM can be used with the
[Windows Subsystem for Linux (WSL)][wsl], both WSL1 and WSL2, by
following these instructions.

In order to use GCM with WSL you must be on Windows 10 Version 1903 or later.
This is the first version of Windows that includes the required `wsl.exe` tool
that GCM uses to interoperate with Git in your WSL distributions.

It is highly recommended that you install Git for Windows to both install GCM
and enable the best experience sharing credentials & settings between WSL and
the Windows host. Alternatively, you must be using GCM version 2.0.XXX or later
and configure the `WSLENV` environment variable as
[described below][configuring-wsl-without-git-for-windows].

## Configuring WSL with Git for Windows (recommended)

Start by installing the [latest Git for Windows ⬇️][latest-git-for-windows]

_Inside your WSL installation_, run the following command to set GCM as the Git
credential helper:

```shell
git config --global credential.helper "/mnt/c/Program\ Files/Git/mingw64/bin/git-credential-manager-core.exe"
```

If you intend to use Azure DevOps you must _also_ set the following Git
configuration _inside of your WSL installation_.

```shell
git config --global credential.https://dev.azure.com.useHttpPath true
```

## Configuring WSL without Git for Windows

If you wish to use GCM inside of WSL _without installing Git for Windows_
you must complete additional configuration so that GCM can callback to Git
inside of your WSL installation.

Start by installing the [latest GCM ⬇️][latest-gcm]

_Inside your WSL installation_, run the following command to set GCM as the Git
credential helper:

```shell
git config --global credential.helper "/mnt/c/Program\ Files\ \(x86\)/Git\ Credential\ Manager/git-credential-manager-core.exe"

# For Azure DevOps support only
git config --global credential.https://dev.azure.com.useHttpPath true
```

In **_Windows_** you need to update the `WSLENV` environment variable to include
the value `GIT_EXEC_PATH/wp`. From an _Administrator_ Command Prompt run the
following:

```batch
SETX WSLENV %WSLENV%:GIT_EXEC_PATH/wp
```

After updating the `WSLENV` environment variable, restart your WSL installation.

### Using the user-only GCM installer?

If you have installed GCM using the user-only installer (i.e, the `gcmuser-*.exe`
installer and not the system-wide/admin required installer), you need to modify
the above instructions to point to
`/mnt/c/Users/<USERNAME>/AppData/Local/Programs/Git\ Credential\ Manager\ Core/git-credential-manager-core.exe`
instead.

## How it works

GCM leverages the built-in interoperability between Windows and WSL, provided by
Microsoft. You can read more about Windows/WSL interop [here][wsl-interop].

Git inside of a WSL installation can launch the GCM _Windows_ application
transparently to acquire credentials. Running GCM as a Windows application
allows it to take full advantage of the host operating system for storing
credentials securely, and presenting GUI prompts for authentication.

Using the host operating system (Windows) to store credentials also means that
your Windows applications and WSL distributions can all share those credentials,
removing the need to sign-in multiple times.

## Shared configuration

Using GCM as a credential helper for a WSL Git installation means that any
configuration set in WSL Git is NOT respected by GCM (by default). This is
because GCM is running as a Windows application, and therefore will use the Git
for Windows installation to query configuration.

This means things like proxy settings for GCM need to be set in Git for Windows
as well as WSL Git as they are stored in different files
(`%USERPROFILE%\.gitconfig` vs `\\wsl$\distro\home\$USER\.gitconfig`).

You can configure WSL such that GCM will use the WSL Git configuration following
the [instructions above][configuring-wsl-without-git-for-windows]. However,
this then means that things like proxy settings are unique to the specific WSL
installation, and not shared with others or the Windows host.

## Can I install Git Credential Manager directly inside of WSL?

Yes. Rather than install GCM as a Windows application (and have WSL Git invoke
the Windows GCM), can you install GCM as a Linux application instead.

To do this, simply follow the
[GCM installation instructions for Linux][linux-installation].

**Note:** In this scenario, because GCM is running as a Linux application
it cannot utilize authentication or credential storage features of the host
Windows operating system.

[wsl]: https://aka.ms/wsl
[configuring-wsl-without-git-for-windows]: #configuring-wsl-without-git-for-windows
[latest-git-for-windows]: https://github.com/git-for-windows/git/releases/latest
[latest-gcm]: https://aka.ms/gcm/latest
[wsl-interop]: https://docs.microsoft.com/en-us/windows/wsl/interop
[linux-installation]: ../README.md#linux
