# Git Credential Manager

[![Build Status](https://github.com/GitCredentialManager/git-credential-manager/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/GitCredentialManager/git-credential-manager/actions/workflows/continuous-integration.yml)

---

[Git Credential Manager](https://github.com/GitCredentialManager/git-credential-manager) (GCM) is a secure Git credential helper built on [.NET](https://dotnet.microsoft.com) that runs on Windows, macOS, and Linux.

Compared to Git's [built-in credential helpers]((https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage)) (Windows: wincred, macOS: osxkeychain, Linux: gnome-keyring/libsecret) which provides single-factor authentication support working on any HTTP-enabled Git repository, GCM provides multi-factor authentication support for [Azure DevOps](https://dev.azure.com/), Azure DevOps Server (formerly Team Foundation Server), GitHub, Bitbucket, and GitLab.

Git Credential Manager (GCM) replaces the .NET Framework-based [Git Credential Manager for Windows](https://github.com/microsoft/Git-Credential-Manager-for-Windows) (GCM), and the Java-based [Git Credential Manager for Mac and Linux](https://github.com/microsoft/Git-Credential-Manager-for-Mac-and-Linux) (Java GCM), providing a consistent authentication experience across all platforms.

## Current status

Git Credential Manager is currently available for Windows, macOS, and Linux\*. GCM only works with HTTP(S) remotes; you can still use Git with SSH:

- [Azure DevOps SSH](https://docs.microsoft.com/en-us/azure/devops/repos/git/use-ssh-keys-to-authenticate?view=azure-devops)
- [GitHub SSH](https://help.github.com/en/articles/connecting-to-github-with-ssh)
- [Bitbucket SSH](https://confluence.atlassian.com/bitbucket/ssh-keys-935365775.html)

Feature|Windows|macOS|Linux\*
-|:-:|:-:|:-:
Installer/uninstaller|&#10003;|&#10003;|&#10003;
Secure platform credential storage [(see more)](docs/credstores.md)|&#10003;|&#10003;|&#10003;
Multi-factor authentication support for Azure DevOps|&#10003;|&#10003;|&#10003;
Two-factor authentication support for GitHub|&#10003;|&#10003;|&#10003;
Two-factor authentication support for Bitbucket|&#10003;|&#10003;|&#10003;
Two-factor authentication support for GitLab|&#10003;|&#10003;|&#10003;
Windows Integrated Authentication (NTLM/Kerberos) support|&#10003;|_N/A_|_N/A_
Basic HTTP authentication support|&#10003;|&#10003;|&#10003;
Proxy support|&#10003;|&#10003;|&#10003;
`amd64` support|&#10003;|&#10003;|&#10003;
`x86` support|&#10003;|_N/A_|&#10007;
`arm64` support|best effort|&#10003;|best effort, no packages
`armhf` support|_N/A_|_N/A_|best effort, no packages

(\*) GCM guarantees support for the below Linux distributions. GCM maintainers also monitor and evaluate issues opened against other distributions to determine community interest/engagement and whether an emerging platform should become fully-supported.

- Debian/Ubuntu/Linux Mint
- Fedora/CentOS/RHEL
- Alpine

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

If you have an existing installation of the 'Java GCM' on macOS and you have installed this using Homebrew, this installation will be unlinked (`brew unlink git-credential-manager`) when GCM is installed.

#### Uninstall

To uninstall, run the following:

```shell
brew uninstall --cask git-credential-manager-core
```

---

### macOS Package

We also provide a [.pkg installer](https://github.com/GitCredentialManager/git-credential-manager/releases/latest) with each release. To install, double-click the installation package and follow the instructions presented.

#### Uninstall

To uninstall, run the following:

```shell
sudo /usr/local/share/gcm-core/uninstall.sh
```

---

<!-- this explicit anchor should stay stable so that external docs can link here -->
<!-- markdownlint-disable-next-line no-inline-html -->
<a name="linux-install-instructions"></a>

### Linux

#### Ubuntu/Debian distributions

Download the latest [.deb package](https://github.com/GitCredentialManager/git-credential-manager/releases/latest), and run the following:

```shell
sudo dpkg -i <path-to-package>
git-credential-manager-core configure
```

**Note:** Although packages were previously offered on certain
[Microsoft Ubuntu package feeds](https://packages.microsoft.com/repos/),
GCM no longer publishes to these repositories. Please install the
Debian package using the above instructions instead.

To uninstall:

```shell
git-credential-manager-core unconfigure
sudo dpkg -r gcmcore
```

#### Other distributions

##### Option 1: Tarball

Download the latest [tarball](https://github.com/GitCredentialManager/git-credential-manager/releases/latest), and run the following:

```shell
tar -xvf <path-to-tarball> -C /usr/local/bin
git-credential-manager-core configure
```

To uninstall:

```shell
git-credential-manager-core unconfigure
rm $(command -v git-credential-manager-core)
```

#### Option 2: Install from source helper script

1. Ensure `curl` is installed:

   ```shell
   curl --version
   ```

   If `curl` is not installed, please use your distribution's package manager
   to install it.

1. Download and run the script:

   ```shell
   curl -LO https://raw.githubusercontent.com/GitCredentialManager/git-credential-manager/main/src/linux/Packaging.Linux/install-from-source.sh &&
   sh ./install-from-source.sh &&
   git-credential-manager-core configure
   ```

   **Note:** You will be prompted to enter your credentials so that the script
   can download GCM's dependencies using your distribution's package
   manager.

To uninstall:

[Follow these instructions](docs/linux-fromsrc-uninstall.md) for your distribution.

**Note:** all Linux distributions [require additional configuration](https://aka.ms/gcm/credstores) to use GCM.

---

### Windows

GCM is included with [Git for Windows](https://gitforwindows.org/), and the latest version is included in each new Git for Windows release. This is the preferred way to install GCM on Windows. During installation you will be asked to select a credential helper, with GCM being set as the default.

![image](https://user-images.githubusercontent.com/5658207/140082529-1ac133c1-0922-4a24-af03-067e27b3988b.png)

#### Standalone installation

You can also download the [latest installer](https://github.com/GitCredentialManager/git-credential-manager/releases/latest) for Windows to install GCM standalone.

**:warning: Important :warning:**

Installing GCM as a standalone package on Windows will forcibly override the version of GCM that is bundled with Git for Windows, **even if the version bundled with Git for Windows is a later version**.

There are two flavors of standalone installation on Windows:

- User (preferred) (`gcmuser-win*`):

  Does not require administrator rights. Will install only for the current user and updates only the current user's Git configuration.

- System (`gcm-win*`):

  Requires administrator rights. Will install for all users on the system and update the system-wide Git configuration.

To install, double-click the desired installation package and follow the instructions presented.

#### Uninstall (Windows 10)

To uninstall, open the Settings app and navigate to the Apps section. Select "Git Credential Manager" and click "Uninstall".

#### Uninstall (Windows 7-8.1)

To uninstall, open Control Panel and navigate to the Programs and Features screen. Select "Git Credential Manager" and click "Remove".

#### Windows Subsystem for Linux (WSL)

Git Credential Manager can be used with the [Windows Subsystem for Linux
(WSL)](https://aka.ms/wsl) to enable secure authentication of your remote Git
repositories from inside of WSL.

[Please see the GCM on WSL docs](docs/wsl.md) for more information.

## Supported Git versions

Git Credential Manager tries to be compatible with the broadest set of Git
versions (within reason). However there are some know problematic releases of
Git that are not compatible.

- Git 1.x

  The initial major version of Git is not supported or tested with GCM.

- Git 2.26.2

  This version of Git introduced a breaking change with parsing credential
  configuration that GCM relies on. This issue was fixed in commit [`12294990`](https://github.com/git/git/commit/12294990c90e043862be9eb7eb22c3784b526340)
  of the Git project, and released in Git 2.27.0.

## How to use

Once it's installed and configured, Git Credential Manager is called implicitly by Git.
You don't have to do anything special, and GCM isn't intended to be called directly by the user.
For example, when pushing (`git push`) to [Azure DevOps](https://dev.azure.com), [Bitbucket](https://bitbucket.org), or [GitHub](https://github.com), a window will automatically open and walk you through the sign-in process.
(This process will look slightly different for each Git host, and even in some cases, whether you've connected to an on-premises or cloud-hosted Git host.)
Later Git commands in the same repository will re-use existing credentials or tokens that GCM has stored for as long as they're valid.

Read full command line usage [here](docs/usage.md).

### Configuring a proxy

See detailed information [here](https://aka.ms/gcm/httpproxy).

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
- [Azure Repos OAuth tokens](docs/azrepos-users-and-tokens.md)
- [GitLab support](docs/gitlab.md)

## Experimental Features

- [Windows broker (experimental)](docs/windows-broker.md)

## Contributing

This project welcomes contributions and suggestions.
See the [contributing guide](CONTRIBUTING.md) to get started.

This project follows [GitHub's Open Source Code of Conduct](CODE_OF_CONDUCT.md).

## License

We're [MIT](LICENSE) licensed.
When using GitHub logos, please be sure to follow the [GitHub logo guidelines](https://github.com/logos).
