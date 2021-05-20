# Git Credential Manager Core

[![Build Status](https://github.com/microsoft/Git-Credential-Manager-Core/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/microsoft/Git-Credential-Manager-Core/actions/workflows/continuous-integration.yml)

---

[Git Credential Manager Core](https://github.com/microsoft/Git-Credential-Manager-Core) (GCM Core) is a secure Git credential helper built on [.NET](https://dotnet.microsoft.com) that runs on Windows and macOS. Linux support is in an early preview.

Compared to Git's [built-in credential helpers]((https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage)) (Windows: wincred, macOS: osxkeychain, Linux: gnome-keyring) which provides single-factor authentication support working on any HTTP-enabled Git repository, GCM Core provides multi-factor authentication support for [Azure DevOps](https://dev.azure.com/), Azure DevOps Server (formerly Team Foundation Server), GitHub, and Bitbucket.

Git Credential Manager Core (GCM Core) replaces the .NET Framework-based [Git Credential Manager for Windows](https://github.com/microsoft/Git-Credential-Manager-for-Windows) (GCM), and the Java-based [Git Credential Manager for Mac and Linux](https://github.com/microsoft/Git-Credential-Manager-for-Mac-and-Linux) (Java GCM), providing a consistent authentication experience across all platforms.

## Current status

Git Credential Manager Core is currently available for macOS and Windows, with Linux support in preview. If the Linux version of GCM Core is insufficient then SSH still remains an option:

- [Azure DevOps SSH](https://docs.microsoft.com/en-us/azure/devops/repos/git/use-ssh-keys-to-authenticate?view=azure-devops)
- [GitHub SSH](https://help.github.com/en/articles/connecting-to-github-with-ssh)
- [Bitbucket SSH](https://confluence.atlassian.com/bitbucket/ssh-keys-935365775.html)

Feature|Windows|macOS|Linux
-|:-:|:-:|:-:
Installer/uninstaller|&#10003;|&#10003;|&#10003;\*\*
Secure platform credential storage|&#10003;<br/>Windows<br/>Credential<br/>Manager|&#10003;<br/>macOS Keychain|&#10003;<br/>1. Secret Service<br/>2. `pass`/GPG<br/>3. Plaintext files
Multi-factor authentication support for Azure DevOps|&#10003;|&#10003;|&#10003;\*
Two-factor authentication support for GitHub|&#10003;|&#10003;\*|&#10003;\*
Two-factor authentication support for Bitbucket|&#10003;|&#10003;\*|&#10003;\*
Windows Integrated Authentication (NTLM/Kerberos) support|&#10003;|_N/A_|_N/A_
Basic HTTP authentication support|&#10003;|&#10003;|&#10003;
Proxy support|&#10003;|&#10003;|&#10003;

**Notes:**

(\*) Currently only supported when using Git from the terminal or command line. A platform-native UI experience is not yet available, but planned.

(\*\*) Debian package offered but not yet available on an official Microsoft feed.

### Planned features

- [ ] macOS/Linux native UI ([#136](https://github.com/microsoft/Git-Credential-Manager-Core/issues/136))

## Download and Install

### macOS Homebrew

The preferred installation mechanism is using Homebrew; we offer a Cask in our custom Tap.

To install, run the following:

```shell
brew tap microsoft/git
brew install --cask git-credential-manager-core
```

After installing you can stay up-to-date with new releases by running:

```shell
brew upgrade git-credential-manager-core
```

#### Git Credential Manager for Mac and Linux (Java-based GCM)

If you have an existing installation of the 'Java GCM' on macOS and you have installed this using Homebrew, this installation will be unlinked (`brew unlink git-credential-manager`) when GCM Core is installed.

#### Uninstall

To uninstall, run the following:

```shell
brew uninstall --cask git-credential-manager-core
```

---

### macOS Package

We also provide a [.pkg installer](https://github.com/microsoft/Git-Credential-Manager-Core/releases/latest) with each release. To install, double-click the installation package and follow the instructions presented.

#### Uninstall

To uninstall, run the following:

```shell
sudo /usr/local/share/gcm-core/uninstall.sh
```

---

### Linux Debian package (.deb)

Download the latest [.deb package](https://github.com/microsoft/Git-Credential-Manager-Core/releases/latest), and run the following:

```shell
sudo dpkg -i <path-to-package>
git-credential-manager-core configure
```

Note that Linux distributions [require additional configuration](https://aka.ms/gcmcore-linuxcredstores) to use GCM Core.

---

### Linux tarball (.tar.gz)

Download the latest [tarball](https://github.com/microsoft/Git-Credential-Manager-Core/releases/latest), and run the following:

```shell
tar -xvf <path-to-tarball> -C /usr/local/bin
git-credential-manager-core configure
```

---

### Windows

You can download the [latest installer](https://github.com/microsoft/Git-Credential-Manager-Core/releases/latest) for Windows to install GCM Core standalone.

**:warning: Important :warning:**

Installing GCM Core as a standalone package on Windows will forcably override the version of GCM Core that is bundled with Git for Windows, **even if the version bundled with Git for Windows is a later version**.

There are two flavors of standalone installation on Windows:

- User (preferred) (`gcmcoreuser-win*`):

  Does not require administrator rights. Will install only for the current user and updates only the current user's Git configuration.

- System (`gcmcore-win*`):

  Requires administrator rights. Will install for all users on the system and update the system-wide Git configuration.

To install, double-click the desired installation package and follow the instructions presented.

#### Git Credential Manager for Windows

GCM Core installs side-by-side any existing Git Credential Manager for Windows installation and will take precedence over it and use any existing credentials so you shouldn't need to re-authenticate.

#### Uninstall (Windows 10)

To uninstall, open the Settings app and navigate to the Apps section. Select "Git Credential Manager Core" and click "Uninstall".

#### Uninstall (Windows 7-8.1)

To uninstall, open Control Panel and navigate to the Programs and Features screen. Select "Git Credential Manager Core" and click "Remove".

## How to use

Git Credential Manager Core is called implicitly by Git, when so configured. It is not intended to be called directly by the user.
For example, when pushing (`git push`) to [Azure DevOps](https://dev.azure.com), a window is automatically opened and an OAuth2 flow is started to get your personal access token.

Read full command line usage [here](docs/usage.md).

### Configuring a proxy

See detailed information [here](https://aka.ms/gcmcore-httpproxy).

## Additional Resources

- [Frequently asked questions](docs/faq.md)
- [Development and debugging](docs/development.md)
- [Command-line usage](docs/usage.md)
- [Configuration options](docs/configuration.md)
- [Environment variables](docs/environment.md)
- [Network and HTTP configuration](docs/netconfig.md)
- [Architectural overview](docs/architecture.md)
- [Host provider specification](docs/hostprovider.md)

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit <https://cla.microsoft.com.>

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
