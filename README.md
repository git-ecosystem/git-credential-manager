# Git Credential Manager

[![Build Status][build-status-badge]][workflow-status]

---

[Git Credential Manager][gcm] (GCM) is a secure Git credential helper built on
[.NET][dotnet] that runs on Windows, macOS, and Linux.

Compared to Git's [built-in credential helpers][git-tools-credential-storage]
(Windows: wincred, macOS: osxkeychain, Linux: gnome-keyring/libsecret) which
provides single-factor authentication support working on any HTTP-enabled Git
repository, GCM provides multi-factor authentication support for
[Azure DevOps][azure-devops], Azure DevOps Server (formerly Team Foundation
Server), GitHub, Bitbucket, and GitLab.

Git Credential Manager (GCM) replaces the .NET Framework-based
[Git Credential Manager for Windows][gcm-for-windows] (GCM), and the Java-based
[Git Credential Manager for Mac and Linux][gcm-for-mac-and-linux] (Java GCM),
providing a consistent authentication experience across all platforms.

## Current status

Git Credential Manager is currently available for Windows, macOS, and Linux\*.
GCM only works with HTTP(S) remotes; you can still use Git with SSH:

- [Azure DevOps SSH][azure-devops-ssh]
- [GitHub SSH][github-ssh]
- [Bitbucket SSH][bitbucket-ssh]

Feature|Windows|macOS|Linux\*
-|:-:|:-:|:-:
Installer/uninstaller|&#10003;|&#10003;|&#10003;
Secure platform credential storage [(see more)][gcm-credstores]|&#10003;|&#10003;|&#10003;
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

(\*) GCM guarantees support for the below Linux distributions. GCM maintainers
also monitor and evaluate issues opened against other distributions to determine
community interest/engagement and whether an emerging platform should become
fully-supported.

- Debian/Ubuntu/Linux Mint
- Fedora/CentOS/RHEL
- Alpine

## Download and Install

### macOS Homebrew

The preferred installation mechanism is using Homebrew; we offer a Cask in our
custom Tap.

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

If you have an existing installation of the 'Java GCM' on macOS and you have
installed this using Homebrew, this installation will be unlinked
(`brew unlink git-credential-manager`) when GCM is installed.

#### Uninstall

To uninstall, run the following:

```shell
brew uninstall --cask git-credential-manager-core
```

---

### macOS Package

We also provide a [.pkg installer][latest-release] with each release. To install,
double-click the installation package and follow the instructions presented.

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

Download the latest [.deb package][latest-release], and run the following:

```shell
sudo dpkg -i <path-to-package>
git-credential-manager-core configure
```

**Note:** Although packages were previously offered on certain
[Microsoft Ubuntu package feeds][ms-package-repos],
GCM no longer publishes to these repositories. Please install the
Debian package using the above instructions instead.

To uninstall:

```shell
git-credential-manager-core unconfigure
sudo dpkg -r gcmcore
```

#### Other distributions

##### Option 1: Tarball

Download the latest [tarball][latest-release], and run the following:

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

[Follow these instructions][linux-uninstall] for your distribution.

**Note:** all Linux distributions
[require additional configuration][gcm-credstores] to use GCM.

---

### Windows

GCM is included with [Git for Windows][git-for-windows], and the latest version
is included in each new Git for Windows release. This is the preferred way to
install GCM on Windows. During installation you will be asked to select a
credential helper, with GCM being set as the default.

![image][git-for-windows-screenshot]

#### Standalone installation

You can also download the [latest installer][latest-release] for Windows to
install GCM standalone.

**:warning: Important :warning:**

Installing GCM as a standalone package on Windows will forcibly override the
version of GCM that is bundled with Git for Windows, **even if the version
bundled with Git for Windows is a later version**.

There are two flavors of standalone installation on Windows:

- User (preferred) (`gcmuser-win*`):

  Does not require administrator rights. Will install only for the current user
  and updates only the current user's Git configuration.

- System (`gcm-win*`):

  Requires administrator rights. Will install for all users on the system and
  update the system-wide Git configuration.

To install, double-click the desired installation package and follow the
instructions presented.

#### Uninstall (Windows 10)

To uninstall, open the Settings app and navigate to the Apps section. Select
"Git Credential Manager" and click "Uninstall".

#### Uninstall (Windows 7-8.1)

To uninstall, open Control Panel and navigate to the Programs and Features
screen. Select "Git Credential Manager" and click "Remove".

#### Windows Subsystem for Linux (WSL)

Git Credential Manager can be used with the [Windows Subsystem for Linux
(WSL)][ms-wsl] to enable secure authentication of your remote Git
repositories from inside of WSL.

[Please see the GCM on WSL docs][gcm-wsl] for more information.

## Supported Git versions

Git Credential Manager tries to be compatible with the broadest set of Git
versions (within reason). However there are some know problematic releases of
Git that are not compatible.

- Git 1.x

  The initial major version of Git is not supported or tested with GCM.

- Git 2.26.2

  This version of Git introduced a breaking change with parsing credential
  configuration that GCM relies on. This issue was fixed in commit
  [`12294990`][gcm-commit-12294990] of the Git project, and released in Git
  2.27.0.

## How to use

Once it's installed and configured, Git Credential Manager is called implicitly
by Git. You don't have to do anything special, and GCM isn't intended to be
called directly by the user. For example, when pushing (`git push`) to
[Azure DevOps][azure-devops], [Bitbucket][bitbucket], or [GitHub][github], a
window will automatically open and walk you through the sign-in process. (This
process will look slightly different for each Git host, and even in some cases,
whether you've connected to an on-premises or cloud-hosted Git host.) Later Git
commands in the same repository will re-use existing credentials or tokens that
GCM has stored for as long as they're valid.

Read full command line usage [here][gcm-usage].

### Configuring a proxy

See detailed information [here][gcm-http-proxy].

## Additional Resources

- [Frequently asked questions][gcm-faq]
- [Development and debugging][gcm-dev]
- [Command-line usage][gcm-usage]
- [Configuration options][gcm-config]
- [Environment variables][gcm-env]
- [Enterprise configuration][gcm-enterprise-config]
- [Network and HTTP configuration][gcm-net-config]
- [Credential stores][gcm-credstores]
- [Architectural overview][gcm-arch]
- [Host provider specification][gcm-host-provider]
- [Azure Repos OAuth tokens][gcm-azure-tokens]
- [GitLab support][gcm-gitlab]

## Experimental Features

- [Windows broker (experimental)][gcm-windows-broker]

## Contributing

This project welcomes contributions and suggestions.
See the [contributing guide][gcm-contributing] to get started.

This project follows [GitHub's Open Source Code of Conduct][gcm-coc].

## License

We're [MIT][gcm-license] licensed.
When using GitHub logos, please be sure to follow the
[GitHub logo guidelines][github-logos].

[azure-devops]: https://dev.azure.com/
[azure-devops-ssh]: https://docs.microsoft.com/en-us/azure/devops/repos/git/use-ssh-keys-to-authenticate?view=azure-devops
[bitbucket]: https://bitbucket.org
[bitbucket-ssh]: https://confluence.atlassian.com/bitbucket/ssh-keys-935365775.html
[build-status-badge]: https://github.com/GitCredentialManager/git-credential-manager/actions/workflows/continuous-integration.yml/badge.svg
[dotnet]: https://dotnet.microsoft.com
[gcm]: https://github.com/GitCredentialManager/git-credential-manager
[gcm-arch]: docs/architecture.md
[gcm-azure-tokens]: docs/azrepos-users-and-tokens.md
[gcm-coc]: CODE_OF_CONDUCT.md
[gcm-commit-12294990]: https://github.com/git/git/commit/12294990c90e043862be9eb7eb22c3784b526340
[gcm-config]: docs/configuration.md
[gcm-contributing]: CONTRIBUTING.md
[gcm-credstores]: docs/credstores.md
[gcm-dev]: docs/development.md
[gcm-enterprise-config]: docs/enterprise-config.md
[gcm-env]: docs/environment.md
[gcm-faq]: docs/faq.md
[gcm-for-mac-and-linux]: https://github.com/microsoft/Git-Credential-Manager-for-Mac-and-Linux
[gcm-for-windows]: https://github.com/microsoft/Git-Credential-Manager-for-Windows
[gcm-gitlab]: docs/gitlab.md
[gcm-host-provider]: docs/hostprovider.md
[gcm-http-proxy]: docs/netconfig.md#http-proxy
[gcm-license]: LICENSE
[gcm-net-config]: docs/netconfig.md
[gcm-usage]: docs/usage.md
[gcm-windows-broker]: docs/windows-broker.md
[gcm-wsl]: docs/wsl.md
[git-for-windows]: https://gitforwindows.org/
[git-for-windows-screenshot]: https://user-images.githubusercontent.com/5658207/140082529-1ac133c1-0922-4a24-af03-067e27b3988b.png
[git-tools-credential-storage]: https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage
[github]: https://github.com
[github-ssh]: https://help.github.com/en/articles/connecting-to-github-with-ssh
[github-logos]: https://github.com/logos
[latest-release]: https://github.com/GitCredentialManager/git-credential-manager/releases/latest
[linux-uninstall]: docs/linux-fromsrc-uninstall.md
[ms-package-repos]: https://packages.microsoft.com/repos/
[ms-wsl]: https://aka.ms/wsl#
[workflow-status]: https://github.com/GitCredentialManager/git-credential-manager/actions/workflows/continuous-integration.yml
