# Install instructions

There are multiple ways to install GCM on macOS, Windows, and Linux. Preferred
installation methods for each OS are designated with a :star:.

## macOS

### Homebrew :star:

**Note:** If you have an existing installation of the 'Java GCM' on macOS and
you have installed this using Homebrew, this installation will be unlinked
(`brew unlink git-credential-manager`) when GCM is installed.

#### Install

```shell
brew tap microsoft/git
brew install --cask git-credential-manager-core
```

After installing you can stay up-to-date with new releases by running:

```shell
brew upgrade git-credential-manager-core
```

#### Uninstall

To uninstall, run the following:

```shell
brew uninstall --cask git-credential-manager-core
```

---

### macOS Package

#### Install

Download and double-click the [installation package][latest-release] and follow
the instructions presented.

#### Uninstall

To uninstall, run the following:

```shell
sudo /usr/local/share/gcm-core/uninstall.sh
```

---

<!-- this explicit anchor should stay stable so that external docs can link here -->
<!-- markdownlint-disable-next-line no-inline-html -->
<a name="linux-install-instructions"></a>

## Linux

**Note:** all Linux distributions
[require additional configuration][gcm-credstores] to use GCM.

---

### .NET tool :star:

See the [.NET tool](#net-tool) section below for instructions on this
installation method.

---

### Debian package

#### Install

Download the latest [.deb package][latest-release], and run the following:

```shell
sudo dpkg -i <path-to-package>
git-credential-manager configure
```

#### Uninstall

```shell
git-credential-manager unconfigure
sudo dpkg -r gcm
```

---

### Tarball

#### Install

Download the latest [tarball][latest-release], and run the following:

```shell
tar -xvf <path-to-tarball> -C /usr/local/bin
git-credential-manager configure
```

#### Uninstall

```shell
git-credential-manager unconfigure
rm $(command -v git-credential-manager)
```

---

### Install from source helper script

#### Install

Ensure `curl` is installed:

```shell
curl --version
```

If `curl` is not installed, please use your distribution's package manager
to install it.

Download and run the script:

```shell
curl -LO https://aka.ms/gcm/linux-install-source.sh &&
sh ./linux-install-source.sh &&
git-credential-manager-core configure
```

**Note:** You will be prompted to enter your credentials so that the script
can download GCM's dependencies using your distribution's package
manager.

#### Uninstall

[Follow these instructions][linux-uninstall] for your distribution.

---

## Windows

### Git for Windows :star:

GCM is included with [Git for Windows][git-for-windows]. During installation
you will be asked to select a credential helper, with GCM listed as the default.

![image][git-for-windows-screenshot]

---

### Standalone installation

You can also download the [latest installer][latest-release] for Windows to
install GCM standalone.

**:warning: Important :warning:**

Installing GCM as a standalone package on Windows will forcibly override the
version of GCM that is bundled with Git for Windows, **even if the version
bundled with Git for Windows is a later version**.

There are two flavors of standalone installation on Windows:

- User (`gcmuser-win*`):

  Does not require administrator rights. Will install only for the current user
  and updates only the current user's Git configuration.

- System (`gcm-win*`):

  Requires administrator rights. Will install for all users on the system and
  update the system-wide Git configuration.

To install, double-click the desired installation package and follow the
instructions presented.

### Uninstall (Windows 10)

To uninstall, open the Settings app and navigate to the Apps section. Select
"Git Credential Manager" and click "Uninstall".

### Uninstall (Windows 7-8.1)

To uninstall, open Control Panel and navigate to the Programs and Features
screen. Select "Git Credential Manager" and click "Remove".

### Windows Subsystem for Linux (WSL)

Git Credential Manager can be used with the [Windows Subsystem for Linux
(WSL)][ms-wsl] to enable secure authentication of your remote Git
repositories from inside of WSL.

[Please see the GCM on WSL docs][gcm-wsl] for more information.

---

## .NET tool

GCM is available to install as a cross-platform [.NET
tool][dotnet-tool]. This is
the preferred install method for Linux because you can use it to install on any
[.NET-supported
distribution][dotnet-supported-distributions]. You
can also use this method on macOS or Windows if you so choose.

**Note:** Make sure you have installed .NET before attempting to run the
following `dotnet tool` commands.

#### Install

```shell
dotnet tool install -g git-credential-manager
git-credential-manager configure
```

#### Update

```shell
dotnet tool update -g git-credential-manager
```

#### Uninstall

```shell
git-credential-manager unconfigure
dotnet tool uninstall -g git-credential-manager
```

[dotnet-supported-distributions]: https://learn.microsoft.com/en-us/dotnet/core/install/linux
[dotnet-tool]: https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools
[gcm-credstores]: credstores.md
[gcm-wsl]: wsl.md
[git-for-windows]: https://gitforwindows.org/
[git-for-windows-screenshot]: https://user-images.githubusercontent.com/5658207/140082529-1ac133c1-0922-4a24-af03-067e27b3988b.png
[latest-release]: https://github.com/GitCredentialManager/git-credential-manager/releases/latest
[linux-uninstall]: linux-fromsrc-uninstall.md
[ms-wsl]: https://aka.ms/wsl#
