# Configuration options

[Git Credential Manager Core](usage.md) works out of the box for most users.

Git Credential Manager Core (GCM Core) can be configured using Git's configuration files, and follows all of the same rules Git does when consuming the files.
Global configuration settings override system configuration settings, and local configuration settings override global settings; and because the configuration details exist within Git's configuration files you can use Git's `git config` utility to set, unset, and alter the setting values. All of GCM Core's configuration settings begin with the term `credential`.

GCM Core honors several levels of settings, in addition to the standard local \> global \> system tiering Git uses.
URL-specific settings or overrides can be applied to any value in the `credential` namespace with the syntax below.

Additionally, GCM Core respects several GCM-specific [environment variables](environment.md) **which take precedence over configuration options.**

GCM Core will only be used by Git if it is installed and configured (`credential.helper`).

**Example:**

> `credential.microsoft.visualstudio.com.namespace` is more specific than `credential.visualstudio.com.namespace`, which is more specific than `credential.namespace`.

In the examples above, the `credential.namespace` setting would affect any remote repository; the `credential.visualstudio.com.namespace` would affect any remote repository in the domain, and/or any subdomain (including `www.`) of, 'visualstudio.com'; where as the the `credential.microsoft.visualstudio.com.namespace` setting would only be applied to remote repositories hosted at 'microsoft.visualstudio.com'.

For the complete list of settings GCM Core understands, see the list below.

## Available settings

_Currently there are no available settings. This is a placeholder for the settings on the backlog._
