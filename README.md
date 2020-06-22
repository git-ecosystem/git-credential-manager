# Git Credential Manager Core

Branch|Status
-|-
master|[![Build Status](https://mseng.visualstudio.com/AzureDevOps/_apis/build/status/Teams/VCDesktop/Git-Credential-Manager-Core/GCM-CI?branchName=master)](https://mseng.visualstudio.com/AzureDevOps/_build/latest?definitionId=7861&branchName=master)

---

[Git Credential Manager Core](https://github.com/Microsoft/Git-Credential-Manager-Core) (GCM Core) is a secure Git credential helper built on [.NET Core](https://microsoft.com/dotnet) that runs on Windows and macOS. Linux support is planned, but not yet scheduled.

Compared to Git's [built-in credential helpers]((https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage)) (Windows: wincred, macOS: osxkeychain, Linux: gnome-keyring) which provides single-factor authentication support working on any HTTP-enabled Git repository, GCM Core provides multi-factor authentication support for [Azure DevOps](https://dev.azure.com/), Azure DevOps Server (formerly Team Foundation Server), GitHub, and Bitbucket.

## Public preview

The long-term goal of Git Credential Manager Core (GCM Core) is to converge the .NET Framework-based [Git Credential Manager for Windows](https://github.com/Microsoft/Git-Credential-Manager-for-Windows) (GCM), and the Java-based [Git Credential Manager for Mac and Linux](https://github.com/Microsoft/Git-Credential-Manager-for-Mac-and-Linux) (Java GCM), providing a consistent authentication experience across all platforms.

### Current status

Git Credential Manager Core is currently in preview for macOS and Windows. Linux support is planned, but not yet scheduled. For now, we recommend [SSH for authentication to Azure DevOps](https://docs.microsoft.com/en-us/azure/devops/repos/git/use-ssh-keys-to-authenticate?view=azure-devops) for Linux users.

Feature|Windows|macOS|Linux
-|:-:|:-:|:-:
Installer/uninstaller|&#10003;|&#10003;|
Secure platform credential storage|&#10003;<br/>Windows Credential Manager|&#10003;<br/>macOS Keychain|
Multi-factor authentication support for Azure DevOps|&#10003;|&#10003;|&#10003;\*
Two-factor authentication support for GitHub|&#10003;|&#10003;\*|&#10003;\*
Two-factor authentication support for Bitbucket|&#10003;|&#10003;\*|&#10003;\*
Windows Integrated Authentication (NTLM/Kerberos) support|&#10003;|_N/A_|_N/A_
Basic HTTP authentication support|&#10003;|&#10003;|&#10003;
Proxy support|&#10003;|&#10003;|

**Notes:**

(\*) Currently only supported when using Git from the terminal or command line. A platform-native UI experience is not yet available, but planned.

### Planned features

- [ ] Linux support ([#135](https://github.com/microsoft/Git-Credential-Manager-Core/issues/135))
- [ ] macOS/Linux native UI ([#136](https://github.com/microsoft/Git-Credential-Manager-Core/issues/136))

## Download and Install

### macOS Homebrew

The preferred installation mechanism is using Homebrew; we offer a Cask in our custom Tap.

To install, run the following:

```shell
brew tap microsoft/git
brew cask install git-credential-manager-core
```

#### Git Credential Manager for Mac and Linux (Java-based GCM)

If you have an existing installation of the 'Java GCM' on macOS and you have installed this using Homebrew, this installation will be unlinked (`brew unlink git-credential-manager`) when GCM Core is installed.

#### Uninstall

To uninstall, run the following:

```shell
brew cask uninstall git-credential-manager-core
```

---

### macOS Package

We also provide a [.pkg installer](https://github.com/Microsoft/Git-Credential-Manager-Core/releases/latest) with each release. To install, double-click the installation package and follow the instructions presented.

#### Uninstall

To uninstall, run the following:

```shell
sudo /usr/local/share/gcm-core/uninstall.sh
```

---

### Windows

You can download the [latest installer](https://github.com/Microsoft/Git-Credential-Manager-Core/releases/latest) for Windows. To install, double-click the installation package and follow the instructions presented.

#### Git Credential Manager for Windows

GCM Core installs side-by-side any existing Git Credential Manager for Windows installation and will take precedence over it and use any existing credentials so you shouldn't need to re-authenticate.

#### Uninstall (Windows 10)

To uninstall, open the Settings app and navigate to the Apps section. Select "Git Credential Manager Core" and click "Uninstall".

#### Uninstall (Windows 7-8.1)

To uninstall, open Control Panel and navigate to the Programs and Features screen. Select "Git Credential Manager Core" and click "Remove".

## How to use

Git Credential Manager Core is called implicitly by Git, when so configured. It is not intended to be called directly by the user.
For example, when pushing (`git push`) to [Azure DevOps](https://dev.azure.com), a window is automatically opened and an OAuth2 flow is started to get your personal access token.

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
