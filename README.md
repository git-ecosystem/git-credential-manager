# Git Credential Manager

[![Build Status][build-status-badge]][workflow-status]

---

[Git Credential Manager][gcm] (GCM) is a secure
[Git credential helper][git-credential-helper] built on [.NET][dotnet] that runs
on Windows, macOS, and Linux. It aims to provide a consistent and secure
authentication experience, including multi-factor auth, to every major source
control hosting service and platform.

GCM supports (in alphabetical order) [Azure DevOps][azure-devops], Azure DevOps
Server (formerly Team Foundation Server), Bitbucket, GitHub, and GitLab.
Compare to Git's [built-in credential helpers][git-tools-credential-storage]
(Windows: wincred, macOS: osxkeychain, Linux: gnome-keyring/libsecret), which
provide single-factor authentication support for username/password only.

GCM replaces both the .NET Framework-based
[Git Credential Manager for Windows][gcm-for-windows] and the Java-based
[Git Credential Manager for Mac and Linux][gcm-for-mac-and-linux].

## Install

See the [installation instructions][install] for the current version of GCM for
install options for your operating system.

## Current status

Git Credential Manager is currently available for Windows, macOS, and Linux and
works with HTTP(S) remotes.

You can still use Git with SSH - see the specific documentation for your host on
how to set up SSH: [Azure DevOps][azure-devops-ssh], [GitHub][github-ssh],
[Bitbucket][bitbucket-ssh]

Feature|Windows|macOS|Linux
-|:-:|:-:|:-:
Installer/uninstaller|&#10003;|&#10003;|&#10003;
Secure platform credential storage [(see more)][gcm-credstores]|&#10003;|&#10003;|&#10003;
Entra authentication with broker support|[opt-in][gcm-windows-broker]|&#10007;|&#10007;
Azure DevOps authentication|&#10003;|&#10003;|&#10003;
GitHub & GHES authentication|&#10003;|&#10003;|&#10003;
Bitbucket Cloud & DC authentication|&#10003;|&#10003;|&#10003;
GitLab authentication|&#10003;|&#10003;|&#10003;
Windows Integrated Authentication (NTLM/Kerberos)|&#10003;|_N/A_|_N/A_
Generic OAuth authentication|&#10003;|&#10003;|&#10003;
Basic HTTP authentication|&#10003;|&#10003;|&#10003;
Network proxies|&#10003;|&#10003;|&#10003;
`amd64` support|&#10003;|&#10003;|&#10003;
`x86` support|&#10003;|_N/A_|&#10007;
`arm64` support|best effort|&#10003;|&#10003;
`armhf` support|_N/A_|_N/A_|&#10003;

## Supported Environments

We aim to support the broadest set of operating systems and distributions,
within reason. We aim to target and update to the latest current .NET LTS
version as they become generally available.

### Windows

GCM supports Windows 10 and later, including Windows Server 2016 and later.
This is the same [support matrix][dotnet-os-support] as the .NET runtime on
Windows.

#### Windows Subsystem for Linux (WSL)

See detailed WSL information [here][gcm-wsl].

#### Windows 7 and Windows 8.x

As of GCM version 3.x, Windows 7 and Windows 8.x are no longer supported.
We continue to provide minimal, security-only support for GCM 2.9.x on Windows 7
and Windows 8.x, but we recommend upgrading to a supported version of Windows.

The [`maint-v2`][maint-v2] and [`releases/v2`][releases-v2] branches are
maintained to allow for security patches and releases for GCM v2.x for Windows 7
and Windows 8.x only. These branches will **not** receive new features.

### macOS

GCM supports macOS 14 and later.
This is the same [support matrix][dotnet-os-support] as the .NET runtime on
macOS.

### Linux

GCM provides support only for [the Linux distributions that are officially
supported by dotnet][dotnet-os-support].

### Git compatibility

Git Credential Manager tries to be compatible with the broadest set of Git
versions (within reason). However there are some known problematic releases of
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
window will automatically open and walk you through the sign-in process.

This process will look slightly different for each Git host, and even in some
cases, whether you've connected to an on-premises or cloud-hosted Git host.
Later Git commands in the same repository will re-use existing credentials or
tokens that GCM has stored for as long as they're valid.

Read full command line usage [here][gcm-usage].

### Configuring a proxy

See detailed information [here][gcm-http-proxy].

## Additional Resources

See the [documentation index][docs-index] for links to additional resources.

## Experimental Features

- (None at this time)

## Future features

Curious about what's coming next in the GCM project? Take a look at the [project
roadmap][roadmap]! You can find more details about the construction of the
roadmap and how to interpret it [here][roadmap-announcement].

## Contributing

This project welcomes contributions and suggestions.
See the [contributing guide][gcm-contributing] to get started.

This project follows [GitHub's Open Source Code of Conduct][gcm-coc].

## License

We're [MIT][gcm-license] licensed.
When using GitHub logos, please be sure to follow the
[GitHub logo guidelines][github-logos].

[azure-devops]: https://azure.microsoft.com/en-us/products/devops
[azure-devops-ssh]: https://docs.microsoft.com/en-us/azure/devops/repos/git/use-ssh-keys-to-authenticate?view=azure-devops
[bitbucket]: https://bitbucket.org/product
[bitbucket-ssh]: https://confluence.atlassian.com/bitbucket/ssh-keys-935365775.html
[build-status-badge]: https://github.com/git-ecosystem/git-credential-manager/actions/workflows/continuous-integration.yml/badge.svg
[docs-index]: docs/README.md
[dotnet]: https://dotnet.microsoft.com
[dotnet-os-support]: https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md
[git-credential-helper]: https://git-scm.com/docs/gitcredentials
[gcm]: https://github.com/git-ecosystem/git-credential-manager
[gcm-coc]: CODE_OF_CONDUCT.md
[gcm-commit-12294990]: https://github.com/git/git/commit/12294990c90e043862be9eb7eb22c3784b526340
[gcm-contributing]: CONTRIBUTING.md
[gcm-credstores]: docs/credstores.md
[gcm-for-mac-and-linux]: https://github.com/microsoft/Git-Credential-Manager-for-Mac-and-Linux
[gcm-for-windows]: https://github.com/microsoft/Git-Credential-Manager-for-Windows
[gcm-http-proxy]: docs/netconfig.md#http-proxy
[gcm-license]: LICENSE
[gcm-usage]: docs/usage.md
[gcm-wsl]: docs/wsl.md
[gcm-windows-broker]: docs/windows-broker.md
[git-tools-credential-storage]: https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage
[github]: https://github.com
[github-ssh]: https://help.github.com/en/articles/connecting-to-github-with-ssh
[github-logos]: https://github.com/logos
[install]: docs/install.md
[maint-v2]: https://github.com/git-ecosystem/git-credential-manager/tree/maint-v2
[releases-v2]: https://github.com/git-ecosystem/git-credential-manager/tree/releases/v2
[roadmap]: https://github.com/git-ecosystem/git-credential-manager/milestones?direction=desc&sort=due_date&state=open
[roadmap-announcement]: https://github.com/git-ecosystem/git-credential-manager/discussions/1203
[workflow-status]: https://github.com/git-ecosystem/git-credential-manager/actions/workflows/continuous-integration.yml
