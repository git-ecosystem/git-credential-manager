# Windows Subsystem for Linux (WSL)

Git Credential Manager Core can be used with the [Windows Subsystem for Linux
(WSL)](https://aka.ms/wsl) to enable secure authentication of your remote Git
repositories from inside of WSL.

## Supported versions

GCM Core supports being called from both WSL1 and WSL2 installations.

**Note:** In order to use GCM with WSL, _without_ a Git for Windows installation
you must be using GCM version 2.0.XXX or later.
You must also be on Windows 10 Version 1903 and later. This is the first version
of Windows that includes the required `wsl.exe` command-line tool that GCM uses
to interoperate with Git in your WSL distributions.

## Set up WSL & Git Credential Manager

### Step 1

[Install the latest Git for Windows ⬇️](https://github.com/git-for-windows/git/releases/latest)

### Step 2

_Inside your WSL installation_, run the following command to set GCM as the Git
credential helper:

```shell
git config --global credential.helper /mnt/c/Program\ Files/Git/mingw64/libexec/git-core/git-credential-manager-core.exe
```

### Step 3 (Azure DevOps only)

If you intend to use Azure DevOps you must _also_ set the following Git
configuration _inside of your WSL installation_.

```shell
git config --global credential.https://dev.azure.com.useHttpPath true
```

## Using GCM & WSL without Git for Windows

If you wish to use GCM Core inside of WSL _without installing Git for Windows_
you must complete additional configuration so that GCM can callback to Git
inside of your WSL installation.

In **_Windows_** you need to update the `WSLENV` environment variable to include
the value `GIT_EXEC_PATH/wp`. From an _Administrator_ Command Prompt run the
following:

```batch
SETX WSLENV=%WSLENV%:GIT_EXEC_PATH/wp
```

..and then restart your WSL installation.

## How it works

GCM leverages the built-in interoperability between Windows and WSL, provided by
Microsoft. You can read more about Windows/WSL interop [here](https://docs.microsoft.com/en-us/windows/wsl/interop).

Git inside of a WSL installation can launch the GCM _Windows_ application
transparently to acquire credentials. Running GCM as a Windows application
allows it to take full advantage of the host operating system for storing
credentials securely, and presenting GUI prompts for authentication.

By using the host operating system (Windows) to store credentials also means
that your Windows applications and WSL distributions can all share those
credentials, removing the need to sign-in multiple times.

## Caveats

Using GCM as a credential helper for a WSL Git installation means that any
configuration set in WSL Git is NOT respected by GCM (by default). This is
because GCM is running as a Windows application, and therefore will use the Git
for Windows installation to query configuration.

This means things like proxy settings for GCM need to be set in Git for Windows
as well as WSL Git as they are stored in different files
(`%USERPROFILE%\.gitconfig` vs `\\wsl$\distro\home\$USER\.gitconfig`).

You can configure WSL such that GCM will use the WSL Git configuration following
the [instructions above](#using-gcm--wsl-without-git-for-windows). However, this
then means that things like proxy settings are unique to the specific WSL
installation, and not shared with others or the Windows host.

## Can I install Git Credential Manager directly inside of WSL?

Yes. Rather than install GCM as a Windows application (and have WSL Git invoke
the Windows GCM), can you install GCM as a Linux application instead.

To do this, simply follow the [GCM installation instructions for Linux](../README.md#linux-install-instructions).

**Note:** In this scenario, because GCM is running as a Linux application
it cannot utilize authentication or credential storage features of the host
Windows operating system.
