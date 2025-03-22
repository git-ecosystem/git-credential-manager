# Credential stores

There are several options for storing credentials that GCM supports:

- Windows Credential Manager
- DPAPI protected files
- macOS Keychain
- [freedesktop.org Secret Service API][freedesktop-secret-service]
- GPG/[`pass`][passwordstore] compatible files
- Git's built-in [credential cache][credential-cache]
- Plaintext files

The default credential stores on macOS and Windows are the macOS Keychain and
the Windows Credential Manager, respectively.

GCM comes without a default store on Linux distributions.

You can select which credential store to use by setting the [`GCM_CREDENTIAL_STORE`][gcm-credential-store]
environment variable, or the [`credential.credentialStore`][credential-store]
Git configuration setting. For example:

```shell
git config --global credential.credentialStore gpg
```

Some credential stores have limitations, or further configuration required
depending on your particular setup. See more detailed information below for each
credential store.

## Windows Credential Manager

**Available on:** _Windows_

**This is the default store on Windows.**

**:warning: Does not work over a network/SSH session.**

```batch
SET GCM_CREDENTIAL_STORE="wincredman"
```

or

```shell
git config --global credential.credentialStore wincredman
```

This credential store uses the Windows Credential APIs (`wincred.h`) to store
data securely in the Windows Credential Manager (also known as the Windows
Credential Vault in earlier versions of Windows).

You can [access and manage data in the credential manager][access-windows-credential-manager]
from the control panel, or via the [`cmdkey` command-line tool][cmdkey].

When connecting to a Windows machine over a network session (such as SSH), GCM
is unable to persist credentials to the Windows Credential Manager due to
limitations in Windows. Connecting by Remote Desktop doesn't suffer from this
limitation.

## DPAPI protected files

**Available on:** _Windows_

```batch
SET GCM_CREDENTIAL_STORE="dpapi"
```

or

```shell
git config --global credential.credentialStore dpapi
```

This credential store uses Windows DPAPI to encrypt credentials which are stored
as files in your file system. The file structure is the same as the
[plaintext files credential store][plaintext-files] except the first line (the
secret value) is protected by DPAPI.

By default files are stored in `%USERPROFILE%\.gcm\dpapi_store`. This can be
configured using the environment variable `GCM_DPAPI_STORE_PATH` environment
variable.

If the directory doesn't exist it will be created.

## macOS Keychain

**Available on:** _macOS_

**This is the default store on macOS.**

```shell
export GCM_CREDENTIAL_STORE=keychain
# or
git config --global credential.credentialStore keychain
```

This credential store uses the default macOS Keychain, which is typically the
`login` keychain.

You can [manage data stored in the keychain][mac-keychain-management]
using the Keychain Access application.

## [freedesktop.org Secret Service API][freedesktop-secret-service]

**Available on:** _Linux_

**:warning: Requires a graphical user interface session.**

```shell
export GCM_CREDENTIAL_STORE=secretservice
# or
git config --global credential.credentialStore secretservice
```

This credential store uses the `libsecret` library to interact with the Secret
Service. It stores credentials securely in 'collections', which can be viewed by
tools such as `secret-tool` and `seahorse`.

A graphical user interface is required in order to show a secure prompt to
request a secret collection be unlocked.

## GPG/[`pass`][passwordstore] compatible files

**Available on:** _macOS, Linux_

**:warning: Requires `gpg`, `pass`, and a GPG key pair.**

```shell
export GCM_CREDENTIAL_STORE=gpg
# or
git config --global credential.credentialStore gpg
```

This credential store uses GPG to encrypt files containing credentials which are
stored in your file system. The file structure is compatible with the popular
[`pass`][passwordstore] tool. By default files are stored in
`~/.password-store` but this can be configured using the `pass` environment
variable `PASSWORD_STORE_DIR`.

Before you can use this credential store, it must be initialized by the `pass`
utility, which in-turn requires a valid GPG key pair. To initalize the store,
run:

```shell
pass init <gpg-id>
```

..where `<gpg-id>` is the user ID of a GPG key pair on your system. To create a
new GPG key pair, run:

```shell
gpg --gen-key
```

..and follow the prompts.

### Headless/TTY-only sessions

If you are using the `gpg` credential store in a headless/TTY-only environment,
you must ensure you have configured the GPG Agent (`gpg-agent`) with a suitable
pin-entry program for the terminal such as `pinentry-tty` or `pinentry-curses`.

If you are connecting to your system via SSH, then the `SSH_TTY` variable should
automatically be set. GCM will pass the value of `SSH_TTY` to GPG/GPG Agent
as the TTY device to use for prompting for a passphrase.

If you are not connecting via SSH, or otherwise do not have the `SSH_TTY`
environment variable set, you must set the `GPG_TTY` environment variable before
running GCM. The easiest way to do this is by adding the following to your
profile (`~/.bashrc`, `~/.profile` etc):

```shell
export GPG_TTY=$(tty)
```

**Note:** Using `/dev/tty` does not appear to work here - you must use the real
TTY device path, as returned by the `tty` utility.

## Git's built-in [credential cache][credential-cache]

**Available on:** _macOS, Linux_

```shell
export GCM_CREDENTIAL_STORE=cache
# or
git config --global credential.credentialStore cache
```

This credential store uses Git's built-in ephemeral
in-memory [credential cache][credential-cache].
This helps you reduce the number of times you have to authenticate but
doesn't require storing credentials on persistent storage. It's good for
scenarios like [Azure Cloud Shell][azure-cloudshell]
or [AWS CloudShell][aws-cloudshell], where you don't want to
leave credentials on disk but also don't want to re-authenticate on every Git
operation.

By default, `git credential-cache` stores your credentials for 900 seconds.
That, and any other [options it accepts][git-credential-cache-options],
may be altered by setting them in the environment variable
`GCM_CREDENTIAL_CACHE_OPTIONS` or the Git config value
`credential.cacheOptions`. (Using the `--socket` option is untested
and unsupported, but there's no reason it shouldn't work.)

```shell
export GCM_CREDENTIAL_CACHE_OPTIONS="--timeout 300"
# or
git config --global credential.cacheOptions "--timeout 300"
```

## Plaintext files

**Available on:** _Windows, macOS, Linux_

**:warning: This is not a secure method of credential storage!**

```shell
export GCM_CREDENTIAL_STORE=plaintext
# or
git config --global credential.credentialStore plaintext
```

This credential store saves credentials to plaintext files in your file system.
By default files are stored in `~/.gcm/store` or `%USERPROFILE%\.gcm\store`.
This can be configured using the environment variable `GCM_PLAINTEXT_STORE_PATH`
environment variable.

If the directory doesn't exist it will be created.

On POSIX platforms the newly created store directory will have permissions set
such that only the owner can `r`ead/`w`rite/e`x`ecute (`700` or `drwx---`).
Permissions on existing directories will not be modified.

NB. GCM's plaintext store is distinct from [git-credential-store][git-credential-store],
though the formats are similar. The default paths differ.

---

:warning: **WARNING** :warning:

**This storage mechanism is NOT secure!**

**Secrets and credentials are stored in plaintext files _without any security_!**

It is **HIGHLY RECOMMENDED** to always use one of the other credential store
options above. This option is only provided for compatibility and use in
environments where no other secure option is available.

If you chose to use this credential store, it is recommended you set the
permissions on this directory such that no other users or applications can
access files within. If possible, use a path that exists on an external volume
that you take with you and use full-disk encryption.

[access-windows-credential-manager]: https://support.microsoft.com/en-us/windows/accessing-credential-manager-1b5c916a-6a16-889f-8581-fc16e8165ac0
[aws-cloudshell]: https://aws.amazon.com/cloudshell/
[azure-cloudshell]: https://docs.microsoft.com/azure/cloud-shell/overview
[cmdkey]: https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/cmdkey
[credential-store]: configuration.md#credentialcredentialstore
[credential-cache]: https://git-scm.com/docs/git-credential-cache
[freedesktop-secret-service]: https://specifications.freedesktop.org/secret-service/
[gcm-credential-store]: environment.md#GCM_CREDENTIAL_STORE
[git-credential-store]: https://git-scm.com/docs/git-credential-store
[mac-keychain-management]: https://support.apple.com/en-gb/guide/mac-help/mchlf375f392/mac
[git-credential-cache-options]: https://git-scm.com/docs/git-credential-cache#_options
[passwordstore]: https://www.passwordstore.org/
[plaintext-files]: #plaintext-files
