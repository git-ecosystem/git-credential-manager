# Git Credential Manager Rename

In November 2021, _"Git Credential Manager Core"_ was [renamed][rename-pr] to
simply _"Git Credential Manager"_, dropping the "Core" moniker. We announced the
new name in a [GitHub blog post][rename-blog], along with the new home for the
project in its own [organization][gcm-org].

![Git Credential Manager Core renamed](img/gcmcore-rename.png)

At the time, the actual exectuable name was not updated and continued to be
`git-credential-manager-core`. As of [2.0.877][rename-ver], the executable has
been renamed to `git-credential-manager`, matching the new project name.

---

:warning: **Update:** :warning:

As of [2.3.0][no-symlink-ver] the `git-credential-manager-core` symlinks have been
removed.

If you have not updated your configuration you will see error messages similar to:

```console
git: 'credential-manager-core' is not a git command. See 'git --help'.
```

To fix your configuration, please follow the [instructions][instructions] below.

---

## Rename transition

If you continue to use the `git-credential-manager-core` executable name you may
see warning messages like below:

```console
warning: git-credential-manager-core was renamed to git-credential-manager
warning: see https://aka.ms/gcm/rename for more information
```

Since the executable was renamed in 2.0.877, GCM has also included symlinks
using the old name in order to ensure no one's setups would immediately break.

These links will remain until _two_ major Git versions are released after GCM
2.0.877, _**at which point the symlinks will no longer be included**_.

It is recommended to update your Git configuration to use the new executable
name as soon as possible to prevent any issues in the future.

## How to update

### Git for Windows

If you are using GCM bundled with Git for Windows (recommended), you should make
sure you have updated to the latest version.

[Download the latest Git for Windows ⬇️][git-windows]

### Windows standalone installer

If you are using GCM installed either by the user (`gcmuser-*.exe`) or system
(`gcm-*.exe`) installers on Windows, you should uninstall the current version
first and then download and install the [latest version][gcm-latest].

Uninstall instructions for your Windows version can be found
[here][win-standalone-instr].

### macOS Homebrew

> **Note:** As of October 2022 the old `git-credential-manager-core` cask name
> is still used. In the future we plan to rename the package to drop the `-core`
> suffix.

If you use Homebrew to install GCM on macOS you should use `brew upgrade` to
install the latest version.

```sh
brew upgrade git-credential-manager-core
```

### macOS package

If you use the .pkg file to install GCM on macOS, you should first uninstall the
current version, and then install the [latest package][gcm-latest].

```sh
sudo /usr/local/share/gcm-core/uninstall.sh
installer -pkg <path-to-new-package> -target /
```

### Linux Debian package

If you use the .deb Debian package to install GCM on Linux, you should first
`unconfigure` the current version, uninstall the package, and then install and
`configure` the [latest version][gcm-latest].

```sh
git-credential-manager-core unconfigure
sudo dpkg -r gcmcore
sudo dpkg -i <path-to-new-package>
git-credential-manager configure
```

### Linux tarball

If you are using the pre-built GCM binaries on Linux from our tarball, you
should first `unconfigure` the current version before extracting the [latest
binaries][gcm-latest].

```sh
git-credential-manager-core unconfigure
rm $(command -v git-credential-manager-core)
tar -xvf <path-to-new-tarball> -C /usr/local/bin
git-credential-manager configure
```

### Troubleshooting

If after updating your GCM installations if you are still seeing the
[warning][warnings] messages you can try manually editing your Git configuration
to point to the correct GCM executable name.

Start by listing all Git configuration for `credential.helper`, including which
files the particular config entries are located in, using the following command:

```sh
git config --show-origin --get-all credential.helper
```

On Mac or Linux you should see something like this:

<!-- markdownlint-disable MD010 -->
```shell-session
$ git config --show-origin --get-all credential.helper
file:/opt/homebrew/etc/gitconfig	credential.helper=osxkeychain
file:/Users/jdoe/.gitconfig	credential.helper=
file:/Users/jdoe/.gitconfig	credential.helper=/usr/local/share/gcm-core/git-credential-manager-core
```

On Windows you should see something like this:

```shell-session
> git config --show-origin --get-all credential.helper
file:C:/Program Files/Git/etc/gitconfig	credential.helper=manager-core
```
<!-- markdownlint-enable MD010 -->

Look out for entries that include `git-credential-manager-core` or
`manager-core`; these should be replaced and updated to `git-credential-manager`
or `manager` respectively.

> **Note:** When updating the Git configuration file in your home directory
> (`$HOME/.gitconfig` or `%USERPROFILE%\.gitconfig`) you should ensure there are
> is an additional blank entry for `credential.helper` before the GCM entry.
>
> **Mac/Linux**
>
> ```ini
> [credential]
>     helper =
>     helper = /usr/local/share/gcm-core/git-credential-manager
> ```
>
> **Windows**
>
> ```ini
> [credential]
>     helper =
>     helper = C:/Program\\ Files\\ \\(x86\\)/Git\\ Credential\\ Manager/git-credential-manager.exe
> ```
>
> The blank entry is important as it makes sure GCM is the only credential
> helper that is configured, and overrides any helpers configured at the system/
> machine-wide level.

[rename-pr]: https://github.com/git-ecosystem/git-credential-manager/pull/541
[rename-blog]: https://github.blog/2022-04-07-git-credential-manager-authentication-for-everyone/#universal-git-authentication
[gcm-org]: https://github.com/git-ecosystem
[rename-ver]: https://github.com/git-ecosystem/git-credential-manager/releases/tag/v2.0.877
[git-windows]: https://git-scm.com/download/win
[gcm-latest]: https://aka.ms/gcm/latest
[warnings]: #rename-transition
[win-standalone-instr]: ../README.md#standalone-installation
[instructions]: #how-to-update
[no-symlink-ver]: https://github.com/git-ecosystem/git-credential-manager/releases/tag/v2.3.0
