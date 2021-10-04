# Git Credential Manager Core

[![Build Status](https://github.com/microsoft/Git-Credential-Manager-Core/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/microsoft/Git-Credential-Manager-Core/actions/workflows/continuous-integration.yml)

---

[Git Credential Manager Core](https://github.com/microsoft/Git-Credential-Manager-Core) (GCM Core) is a secure Git credential helper built on [.NET](https://dotnet.microsoft.com) that runs on Windows, macOS, and Linux.

Compared to Git's [built-in credential helpers]((https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage)) (Windows: wincred, macOS: osxkeychain, Linux: gnome-keyring/libsecret) which provides single-factor authentication support working on any HTTP-enabled Git repository, GCM Core provides multi-factor authentication support for [Azure DevOps](https://dev.azure.com/), Azure DevOps Server (formerly Team Foundation Server), GitHub, and Bitbucket.

Git Credential Manager Core (GCM Core) replaces the .NET Framework-based [Git Credential Manager for Windows](https://github.com/microsoft/Git-Credential-Manager-for-Windows) (GCM), and the Java-based [Git Credential Manager for Mac and Linux](https://github.com/microsoft/Git-Credential-Manager-for-Mac-and-Linux) (Java GCM), providing a consistent authentication experience across all platforms.

## Current status

Git Credential Manager Core is currently available for Windows, macOS, and Linux. GCM only works with HTTP(S) remotes; you can still use Git with SSH:

- [Azure DevOps SSH](https://docs.microsoft.com/en-us/azure/devops/repos/git/use-ssh-keys-to-authenticate?view=azure-devops)
- [GitHub SSH](https://help.github.com/en/articles/connecting-to-github-with-ssh)
- [Bitbucket SSH](https://confluence.atlassian.com/bitbucket/ssh-keys-935365775.html)

Feature|Windows|macOS|Linux
-|:-:|:-:|:-:
Installer/uninstaller|&#10003;|&#10003;|&#10003;\*
Secure platform credential storage|&#10003;<br/>[(see more)](docs/credstores.md)|&#10003;<br/>[(see more)](docs/credstores.md)|&#10003;<br/>[(see more)](docs/credstores.md)
Multi-factor authentication support for Azure DevOps|&#10003;|&#10003;|&#10003;
Two-factor authentication support for GitHub|&#10003;|&#10003;|&#10003;
Two-factor authentication support for Bitbucket|&#10003;|&#10003;|&#10003;
Windows Integrated Authentication (NTLM/Kerberos) support|&#10003;|_N/A_|_N/A_
Basic HTTP authentication support|&#10003;|&#10003;|&#10003;
Proxy support|&#10003;|&#10003;|&#10003;
`amd64` support|&#10003;|&#10003;|&#10003;
`x86` support|&#10003;|_N/A_|&#10007;
`arm64` support|best effort|via Rosetta 2|best effort, no packages
`armhf` support|_N/A_|_N/A_|best effort, no packages

**Notes:**

(\*) Debian package offered but not yet available on an official Microsoft feed.
Fedora packages planned but not yet available.

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

<!-- this explicit anchor should stay stable so that external docs can link here -->
<a name="linux-install-instructions"></a>
### Linux

#### Debian package (.deb)

`apt-get` support is available for Ubuntu Bionic Beaver (18.04) and Hirsute 
Hippo (21.04). Take the following steps to set up and install based on the
version you are running:

##### Ubuntu 18.04 (Bionic)

```shell
curl -sSL https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
sudo apt-add-repository https://packages.microsoft.com/ubuntu/18.04/prod
sudo apt-get update
sudo apt-get install gcmcore
git-credential-manager-core configure
```

##### Ubuntu 21.04 (Hirsute)

```shell
curl -sSL https://packages.microsoft.com/config/ubuntu/21.04/prod.list | sudo tee /etc/apt/sources.list.d/microsoft-prod.list
curl -sSL https://packages.microsoft.com/keys/microsoft.asc | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc
sudo apt-get update
sudo apt-get install gcmcore
git-credential-manager-core configure
```

##### Other Ubuntu/Debian distributions

Download the latest [.deb package](https://github.com/microsoft/Git-Credential-Manager-Core/releases/latest), and run the following:

```shell
sudo dpkg -i <path-to-package>
git-credential-manager-core configure
```

#### Other distributions

Download the latest [tarball](https://github.com/microsoft/Git-Credential-Manager-Core/releases/latest), and run the following:

```shell
tar -xvf <path-to-tarball> -C /usr/local/bin
git-credential-manager-core configure
```

**Note:** all Linux distributions [require additional configuration](https://aka.ms/gcmcore-credstores) to use GCM Core.

---

### Windows

You can download the [latest installer](https://github.com/microsoft/Git-Credential-Manager-Core/releases/latest) for Windows to install GCM Core standalone.

**:warning: Important :warning:**

Installing GCM Core as a standalone package on Windows will forcibly override the version of GCM Core that is bundled with Git for Windows, **even if the version bundled with Git for Windows is a later version**.

There are two flavors of standalone installation on Windows:

- User (preferred) (`gcmcoreuser-win*`):

  Does not require administrator rights. Will install only for the current user and updates only the current user's Git configuration.

- System (`gcmcore-win*`):

  Requires administrator rights. Will install for all users on the system and update the system-wide Git configuration.

To install, double-click the desired installation package and follow the instructions presented.

#### Uninstall (Windows 10)

To uninstall, open the Settings app and navigate to the Apps section. Select "Git Credential Manager Core" and click "Uninstall".

#### Uninstall (Windows 7-8.1)

To uninstall, open Control Panel and navigate to the Programs and Features screen. Select "Git Credential Manager Core" and click "Remove".

#### Windows Subsystem for Linux (WSL)

Git Credential Manager Core can be used with the [Windows Subsystem for Linux
(WSL)](https://aka.ms/wsl) to enable secure authentication of your remote Git
repositories from inside of WSL.

[Please see the GCM Core on WSL docs](docs/wsl.md) for more information.

## How to use

Once it's installed and configured, Git Credential Manager Core is called implicitly by Git.
You don't have to do anything special, and GCM Core isn't intended to be called directly by the user.
For example, when pushing (`git push`) to [Azure DevOps](https://dev.azure.com), [Bitbucket](https://bitbucket.org), or [GitHub](https://github.com), a window will automatically open and walk you through the sign-in process.
(This process will look slightly different for each Git host, and even in some cases, whether you've connected to an on-premises or cloud-hosted Git host.)
Later Git commands in the same repository will re-use existing credentials or tokens that GCM Core has stored for as long as they're valid.

Read full command line usage [here](docs/usage.md).

### Configuring a proxy

See detailed information [here](https://aka.ms/gcmcore-httpproxy).

## Additional Resources

- [Frequently asked questions](docs/faq.md)
- [Development and debugging](docs/development.md)
- [Command-line usage](docs/usage.md)
- [Configuration options](docs/configuration.md)
- [Environment variables](docs/environment.md)
- [Enterprise configuration](docs/enterprise-config.md)
- [Network and HTTP configuration](docs/netconfig.md)
- [Credential stores](docs/credstores.md)
- [Architectural overview](docs/architecture.md)
- [Host provider specification](docs/hostprovider.md)

## Experimental Features

- [Windows broker (experimental)](docs/windows-broker.md)
- [Azure Repos OAuth tokens (experimental)](docs/azrepos-users-and-tokens.md)

## Contributing

This project welcomes contributions and suggestions.  
See the [contributing guide](CONTRIBUTING.md) to get started.

This project follows [GitHub's Open Source Code of Conduct](CODE_OF_CONDUCT.md).

## License

We're [MIT](LICENSE) licensed.
When using GitHub logos, please be sure to follow the [GitHub logo guidelines](https://github.com/logos).
