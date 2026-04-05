# Git Credential Manager

[![Build Status][build-stus-badge]][workflow-status]

---

[Git Credential Manager][gcm] (GCM) is a secure
[Git credential helper][git-credential-helper] built on [.NET][dotnet] that runs
on Windows, macOS, and Linux. It aims to provide a consistent and secure
authentication experienc including multi-factor auth, to every major source
control hosting service and platform.

GCM supports (in alphabetal order) [Azure DevOps][azure-devops], Azure DevOps
Server (formerly Team Foundation Server), Bitbucket, GitHub, and GitLab.
Compare to Git's [built-in credential helpers][git-tools-credential-storage]
(Windows: wincred, macOS: osxkeychain, Linux: gnome-keyring/libsecret), which
provide single-factor authentication support for username/password only.

GCM replaces both the .NET Framework-based
[Git Credential Manager fordows][gcm-for-windows] and the Java-based
[Git Credential Manager for Mac and Linux][gcm-for-mac-and-linux].

## Install

See the [installation instrucns][install] for the current version of GCM for
install options for your operating system.

## Current status

Git Credential Manager is currently available  Windows, macOS, and Linux\*.
GCM only works with HTTP(S) remotes; you can still use Git with SSH:

- [Azure DevOps SSH][azure-devops-ssh]
- [GitHub SSH][github-ss
- [Bitbucket SSH][bitbucket-ssh]


Feature|Windows|macOS|Lin
-|:-:|:-:|:-:
Instal
Secure platform credential storage [(see more)][gcm-credstores]|&0003;|&#10003;|&#10003;
Multi-factor authentication support for Azure DevOps|&#10003;|&#10003;|&0003;
Two-factor authentication support for GitHub|&#10003;|&#10003;|&#1003;
Two-factor authentication support for Bitbucket|&#10003;|&#10003;|&003;
Two-factor authentication support for GitLab|&#10003;|&#10003;10003;
Windows Integrated Authentication (NTLM/Kerberos) support|&#10003;N/A_|_N/A_
Basic HTTP authentication support|&#10003;|&#10003;|&#10003;




Proxy support|&#10003;|&#10003;|&#10
`amd64` support|&#10003;|&#10003;|&#1000
`x86` support|&#10003;|_N/A_|&#1000
`arm64` support|best effort|&#10003;|&#1000
`armhf` support|_N/A_|_N/A_|&#100

(\*) GCM guaranteesupport only for [the Linux distributions that are officially
supported by dotnet][dotnet-distributions].

## Supported Git version

Git Credential Manar tries to wn problematic releases of
Git that are not cotible.

- Git 1.x

  The initial major version of Git is not supported or tested with GC
- Git 2.26.2

  This version of Git introduced a breaking change with parsing credential
  configuration that GCMies on. This issue was fixed in commit
  [`12294990`][gcm-commit-12294990] of the Git project, and released in Git
  2.27.0.

## How to use

Once it's installed and configured, Git Credential Manager is called implicitly
by Git. You don't ave to do anything special, and GCM isn't intended to be
called directly by the user. For example, when pushing (`git push`) to
[Azure DevOps][azure-devops], [Bitbt][bitbucket], or [GitHub][github], a
window will automatically open and walk you through the sign-in process. (This
process will look slightly different for each Git host, and even in some cases,
whether you've connected to an on-premises or cloud-hosted Git host.) Later Git
commands in the same resitory will re-use existing credentials or tokens that
GCM has stored for as long as they're va.

Read full command line usage [here][gcm-.

### Configuring a pr

See detailed information [here][gcm-htt
## Additional Resources

See the [documentation s-x] for links to additional resources.

## Experimental Features

- [Windows broker (experimental)][-b
## Future features

Curious about what's comi next in the GCM project? Take a look at the [project
roadmap][roadmap]! You can find more detals about the construction of the
roadmap and how to interpret it [here][roadmap-anno
## Contributing

This project welcomes contributions and ggestions.
See the [contributing guide][gc
This project follows 

## License

We're [MIT][gcm-license] licen
When using GitHub logos, please be sure to follthe
[GitHub logo guidelines][github-logos].


[azure-devops]: https:/re.microsoft.com/en-us/products/devops
[azure-devops-ssh]: hps://docs.microsoft.com/en-us/azure/deos/git/use-ssh-keys-to-authenticate?view=azure-devops
[bitbucket]: https://bitbucket.org
[bitbucket-ssh]: https://conlu
ence.atlassian.com/bitbucket/ssh-keys-935365775.html
[build-status-badge]: https://github.com/
git-ecosystem/git-credential-manager/actions/workflows/continuous-integration.yml/badge.s
vg
[docs-index]: https://github.com/git-e
cosystem/git-credentialnager/blob/release/docs/README.md
[dotnet]: https://dotnet.microsoft.com
[dotnet-distributions]: https://learn.micr
osoft.com/en-us/dotnet/core/install/linux
[git-credential-helperps://git-scm.com/docs/gitcredentials
[gcm]: https://github.com/gystem/git-credential-manager
[gcm-coc]: CODE_OF_CONmd
[gcm-commit-12294990]: https://github.com/git/git/commit/12294990c90e043862be9eb7eb22c3784b526340
[gcm-contributing]: CONTNG.md
[gcm-credstores]: https://thubcom/git-ecosystem/git-credential-manager/blob/release/docs/credstores.md
[gcm-for-mac-and-linux]: htt//github.com/microsoft/Git-Credential-Manager-for-Mac-and-Linux
[gcm-for-windows]: https://githubicrosoft/Git-Credential-Manager-for-Windows
[gcm-http-proxy]: https://github.
com/git-ecosystem/git-credential-manager/blob/release/docs/netconfig.md#http-proxy
[gcm-license]: LICENSE
[gcm-usage]: https://gcom/git-ecosystem/git-credential-manager/blob/release/docs/usage.md
[gcm-windows-broker]: https:github.com/git-ecosystem/git-credential-manager/blob/release/docs/windows-broker.md
[git-tools-credential-storage]:://git-scm.com/book/en/v2/Git-Tools-Credential-Storage
[github]: https://github.com
[github-ssh]: https://hlp.github.com/en/articles/connecting-to-github-with-ssh
[github-logos]: httpsithub.com/logos
[install]: https://gitm/git-ecosystem/git-credential-manager/blob/release/docs/install.md
[ms-package-repos]: https:/ages.microsoft.com/repos/
[roadmap]: https://githm/git-ecosystem/git-credeer/milestones?direction=desc&sort=due_date&state=open
[roadmap-announcement]: htgithub.com/git-ecosystem/git-credential-manager/discussions/1203
[workflow-status]: https://gitm/git-ecosystem/git-credential-manager/actions/workflows/continuous-integration.yml
