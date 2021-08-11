# Enterprise configuration defaults

Git Credential Manager Core (GCM Core) can be configured using multiple
different mechanisms. In order of preference, those mechanisms are:

1. [Environment variables](environment.md)
2. [Standard Git configuration files](configuration.md)
   1. Repository/local configuration (`.git/config`)
   2. User/global configuration (`$HOME/.gitconfig` or `%HOME%\.gitconfig`)
   3. Installation/system configuration (`etc/gitconfig`)
3. Enterprise system administrator defaults
4. Compiled default values

This model largely matches what Git itself supports, namely environment
variables that take precedence over Git configuration files.

The addition of the enterprise system administrator defaults enables those
administrators to configure many GCM settings using familiar MDM tooling, rather
than having to modify the Git installation configuration files.

### User Freedom

We believe the user should _always_ be at liberty to configure
Git and GCM exactly as they wish. By prefering environment variables and Git
configuration files over system admin values, these only act as _default values_
that can always be overriden by the user in the usual ways.

## Windows

Default setting values come from the Windows Registry, specifically the
following keys:

**32-bit Windows**

```text
HKEY_LOCAL_MACHINE\SOFTWARE\GitCredentialManager\Configuration
```

**64-bit Windows**

```text
HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GitCredentialManager\Configuration
```

> GCM Core is a 32-bit executable on Windows. When running on a 64-bit
installation of Windows registry access is transparently redirected to the
`WOW6432Node` node.

By using the Windows Registry, system administrators can use Group Policy to
easily set defaults for GCM Core's settings.

The names and possible values of all settings under this key are the same as
those of the [Git configuration](configuration.md) settings.

The type of each registry key can be either `REG_SZ` (string) or `REG_DWORD`
(integer).


## macOS/Linux

Default configuration setting stores has not been implemented.
