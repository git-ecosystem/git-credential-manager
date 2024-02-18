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

(\*) GCM guarantees support only for [the Linux distributions that are officially
supported by dotnet][dotnet-distributions].

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

See the [documentation index][docs-index] for links to additional resources.

## Experimental Features

- [Windows broker (experimental)][gcm-windows-broker]

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
[bitbucket]: https://bitbucket.org
[bitbucket-ssh]: https://confluence.atlassian.com/bitbucket/ssh-keys-935365775.html
[build-status-badge]: https://github.com/git-ecosystem/git-credential-manager/actions/workflows/continuous-integration.yml/badge.svg
[docs-index]: https://github.com/git-ecosystem/git-credential-manager/blob/release/docs/README.md
[dotnet]: https://dotnet.microsoft.com
[dotnet-distributions]: https://learn.microsoft.com/en-us/dotnet/core/install/linux
[git-credential-helper]: https://git-scm.com/docs/gitcredentials
[gcm]: https://github.com/git-ecosystem/git-credential-manager
[gcm-coc]: CODE_OF_CONDUCT.md
[gcm-commit-12294990]: https://github.com/git/git/commit/12294990c90e043862be9eb7eb22c3784b526340
[gcm-contributing]: CONTRIBUTING.md
[gcm-credstores]: https://github.com/git-ecosystem/git-credential-manager/blob/release/docs/credstores.md
[gcm-for-mac-and-linux]: https://github.com/microsoft/Git-Credential-Manager-for-Mac-and-Linux
[gcm-for-windows]: https://github.com/microsoft/Git-Credential-Manager-for-Windows
[gcm-http-proxy]: https://github.com/git-ecosystem/git-credential-manager/blob/release/docs/netconfig.md#http-proxy
[gcm-license]: LICENSE
[gcm-usage]: https://github.com/git-ecosystem/git-credential-manager/blob/release/docs/usage.md
[gcm-windows-broker]: https://github.com/git-ecosystem/git-credential-manager/blob/release/docs/windows-broker.md
[git-tools-credential-storage]: https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage
[github]: https://github.com
[github-ssh]: https://help.github.com/en/articles/connecting-to-github-with-ssh
[github-logos]: https://github.com/logos
[install]: https://github.com/git-ecosystem/git-credential-manager/blob/release/docs/install.md
[ms-package-repos]: https://packages.microsoft.com/repos/
[roadmap]: https://github.com/git-ecosystem/git-credential-manager/milestones?direction=desc&sort=due_date&state=open
[roadmap-announcement]: https://github.com/git-ecosystem/git-credential-manager/discussions/1203
[workflow-status]: https://github.com/git-ecosystem/git-credential-manager/actions/workflows/continuous-integration.yml
