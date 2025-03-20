# Git Credential Manager

[![Build Status][build-status-badge]][workflow-status]

---

[Git Credential Manager][gcm] (GCM) is a secure
[Git credential helper][git-credential-helper] built on that runs
on Windows. It aims to provide a consistent and secure
authentication experience, including multi-factor auth, to every major source
control hosting service and platform.

GCM supports (in order) [Azure DevOps][azure-devops], Azure DevOps
Server (formerly Team Foundation ), Bitbucket, GitHub.
Compare to Git's [built-in credential helpers][git-tools-credential-storage]which
provide single-factor authentication support for username/password only.

GCM replaces both the based
[Git Credential Manager for Windows][gcm-for-windows] and the based




See the [installation instructions] for the current version of GCM for
options for your operating system.

## Current status

Git Credential Manager is currently available for Windows, macOS, and Linux\*.
GCM only works with http(s) remotes; you can still use Git with SSH:

- [Azure DevOps SSH][azure-devops-ssh]
- [GitHub SSH][github-ssh]
- [Bitbucket SSH][bitbucket-ssh]

Feature|Windows|macOS\*
-|:-:|:-:|:-:
Installer/uninstaller|&#10003;|&#10003;|&#10003;
Secure platform credential storage [(see more)][gcm-credstores]|&#10003;|&#10003;|&#10003;
Multi-factor authentication support for Azure DevOps|&#10003;|&#10003;|&#10003;
Two-factor authentication support for GitHub|&#10003;|&#10003;|&#10003;
Two-factor authentication support for Bitbucket|&#10003;|&#10003;|&#10003;
Two-factor authentication support for GitLab|&#10003;|&#10003;|&#10003;
Windows Integrated Authentication (NTLM/Kerberos) support|&#10003;|✓|✓
Basic HTTP authentication support|&#10003;|&#10003;|&#10003;
Proxy support|&#10003;|&#10003;|&#10003;
`amd64` support|&#10003;|&#10003;|&#10003;
`x86` support|&#10003;|✓|&#10007;
`arm64` support|✓|&#10003;|&#10003;
`armhf` support|✓|✓|&#10003;


## Supported Git versions

Git Credential Manager tries to be compatible with the broadest set of Git
versions (within reason). However there are some know problematic releases of
Git that are not compatible.

- Git 1.x

  The initial major version of Git is not supported or tested with GCM.

- Git 2.26.2

  This version of Git introduced a breaking change with parsing credential
  configuration that GCM relies on. This issue was fixed in commit
[gcm-commit] of the Git project, and released in Git
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

- [Windows access (experimental)][gcm-windows-access]

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
