# Git Credential Manager Core

Branch|Status
-|-
master|[![Build Status](https://mseng.visualstudio.com/AzureDevOps/_apis/build/status/Teams/VCDesktop/Git-Credential-Manager-Core/GCM-CI?branchName=master)](https://mseng.visualstudio.com/AzureDevOps/_build/latest?definitionId=7861&branchName=master)

---

[Git Credential Manager Core](https://github.com/Microsoft/Git-Credential-Manager-Core) (GCM Core) is a secure Git credential helper built on [.NET Core](https://microsoft.com/dotnet) that runs on Windows and macOS. Linux support is planned, but not yet scheduled.

Compared to Git's [built-in credential helpers]((https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage)) (Windows: wincred, macOS: osxkeychain, Linux: gnome-keyring) which provides single-factor authentication support working on any HTTP-enabled Git repository, GCM Core provides multi-factor authentication support for [Azure DevOps](https://dev.azure.com/), Azure DevOps Server (formerly Team Foundation Server), and GitHub.

## Public preview for macOS

The long-term goal of GCM Core is to converge the .NET Framework-based [Git Credential Manager for Windows](https://github.com/Microsoft/Git-Credential-Manager-for-Windows) (GCM Windows), and the Java-based [Git Credential Manager for Mac and Linux](https://github.com/Microsoft/Git-Credential-Manager-for-Mac-and-Linux) (Java GCM), providing a consistent authentication experience across all platforms.

### Current status

GCM Core is currently in preview only for macOS, with a Windows preview following soon. Until that time we recommend Windows users to continue to use GCM Windows. Linux support is planned, but not yet scheduled. For now, we recommend [SSH for authentication to Azure DevOps](https://docs.microsoft.com/en-us/azure/devops/repos/git/use-ssh-keys-to-authenticate?view=azure-devops).

Feature|Windows|macOS|Linux
-|:-:|:-:|:-:
Installer/uninstaller||&#10003;|
Secure platform credential storage|&#10003;<br/>Windows Credential Manager|&#10003;<br/>macOS Keychain|
Multi-factor authentication support for Azure DevOps|&#10003;|&#10003;|
Two-factor authentication support for GitHub|&#10003;|&#10003;\*|&#10003;\*
Two-factor authentication support for BitBucket|||
Windows Integrated Authentication (NTLM/Kerberos) support|&#10003;|_N/A_|_N/A_
Basic HTTP authentication support|&#10003;|&#10003;|&#10003;

**Notes:**

(\*) Currently only supported when using Git from the terminal or command line. A platform-native UI experience for GitHub is not yet available on macOS, but planned.

### Planned features

- [ ] Proxy support
- [ ] Windows installer
- [ ] Improved GitHub support (platform-native UI)
- [ ] BitBucket
- [ ] Linux installer

## Download and Install

To use GCM Core, you can download the [latest installer](https://github.com/Microsoft/Git-Credential-Manager-Core/releases/latest) for your platform. To install, double-click installation package and follow the instructions presented.

### Git Credential Manager for Mac and Linux (Java-based GCM)

If you have an existing installation of the 'Java GCM' on macOS and you have installed this using Homebrew, this installation will be unlinked (`brew unlink git-credential-manager`) when GCM Core is installed. Additionally any symlinks or files previously located at `/usr/local/bin/git-credential-manager` will be replaced with a GCM Core symlink.

## How to use

You don't! GCM gets called when Git needs credentials for an operation. For example, when pushing (`git push`) to [Azure DevOps](https://dev.azure.com), it automatically opens a window and initializes an OAuth2 flow to get your personal access token.

## Additional Resources

- [Frequently asked questions](docs/faq.md)
- [Development and debugging](docs/development.md)
- [Command-line usage](docs/usage.md)
- [Configuration options](docs/configuration.md)
- [Environment variables](docs/environment.md)

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
