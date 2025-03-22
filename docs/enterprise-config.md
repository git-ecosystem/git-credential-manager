# Enterprise configuration defaults

Git Credential Manager (GCM) can be configured using multiple
different mechanisms. In order of preference, those mechanisms are:

1. [Environment variables][environment]
1. Standard [Git configuration][config] files
   1. Repository/local configuration (`.git/config`)
   1. User/global configuration (`$HOME/.gitconfig` or `%HOME%\.gitconfig`)
   1. Installation/system configuration (`etc/gitconfig`)
1. Enterprise system administrator defaults
1. Compiled default values

This model largely matches what Git itself supports, namely environment
variables that take precedence over Git configuration files.

The addition of the enterprise system administrator defaults enables those
administrators to configure many GCM settings using familiar MDM tooling, rather
than having to modify the Git installation configuration files.

## User Freedom

We believe the user should _always_ be at liberty to configure
Git and GCM exactly as they wish. By preferring environment variables and Git
configuration files over system admin values, these only act as _default values_
that can always be overridden by the user in the usual ways.

## Windows

Default setting values come from the Windows Registry, specifically the
following keys:

### 32-bit Windows

```text
HKEY_LOCAL_MACHINE\SOFTWARE\GitCredentialManager\Configuration
```

### 64-bit Windows

```text
HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GitCredentialManager\Configuration
```

> GCM is a 32-bit executable on Windows. When running on a 64-bit
installation of Windows registry access is transparently redirected to the
`WOW6432Node` node.

By using the Windows Registry, system administrators can use Group Policy to
easily set defaults for GCM's settings.

The names and possible values of all settings under this key are the same as
those of the [Git configuration][config] settings.

The type of each registry key can be either `REG_SZ` (string) or `REG_DWORD`
(integer).

## macOS/Linux

Default configuration setting stores has not been implemented.

[environment]: environment.md
[config]: configuration.md
