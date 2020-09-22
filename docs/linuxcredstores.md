# Credential stores on Linux

There are currently three options for storing credentials that Git Credential
Manager Core (GCM Core) manages on Linux platforms:

1. [freedesktop.org Secret Service API](https://specifications.freedesktop.org/secret-service/)
2. GPG/[`pass`](https://www.passwordstore.org/) compatible files
3. Plaintext files

By default, GCM Core comes unconfigured. You can select which credential store
to use by setting the [`GCM_CREDENTIAL_STORE`](environment.md#GCM_CREDENTIAL_STORE)
environment variable, or the [`credential.credentialStore`](configuration.md#credentialcredentialstore)
Git configuration setting.

Some credential stores have limitations, or further configuration required
depending on your particular setup.

## 1. [freedesktop.org Secret Service API](https://specifications.freedesktop.org/secret-service/)

```shell
export GCM_CREDENTIAL_STORE=secretservice
# or
git config --global credential.credentialStore secretservice
```

**:warning: Requires a graphical user interface session.**

This credential store uses the `libsecret` library to interact with the Secret
Service. It stores credentials securely in 'collections', which can be viewed by
tools such as `secret-tool` and `seahorse`.

A graphical user interface is required in order to show a secure prompt to
request a secret collection be unlocked.

## 2. GPG/[`pass`](https://www.passwordstore.org/) compatible files

```shell
export GCM_CREDENTIAL_STORE=gpg
# or
git config --global credential.credentialStore gpg
```

**:warning: Requires `gpg`, `pass`, and a GPG key pair.**

This credential store uses GPG to encrypt files containing credentials which are
stored in your file system. The file structure is compatible with the popular
[`pass`](https://www.passwordstore.org/) tool. By default files are stored in
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
automatically be set. GCM Core will pass the value of `SSH_TTY` to GPG/GPG Agent
as the TTY device to use for prompting for a passphrase.

If you are not connecting via SSH, or otherwise do not have the `SSH_TTY`
environment variable set, you must set the `GPG_TTY` environment variable before
running GCM Core. The easiest way to do this is by adding the following to your
profile (`~/.bashrc`, `~/.profile` etc):

```shell
export GPG_TTY=$(tty)
```

**Note:** Using `/dev/tty` does not appear to work here - you must use the real
TTY device path, as returned by the `tty` utility.

## 3. Plaintext files

```shell
export GCM_CREDENTIAL_STORE=plaintext
# or
git config --global credential.credentialStore plaintext
```

**:warning: This is not a secure method of credential storage!**

This credential store saves credentials to plaintext files in your file system.
By default files are stored in `~/.gcm/store` but this can be configured using
the environment variable `GCM_PLAINTEXT_STORE_PATH` environment variable.

If the directory does not exist is will be created.

---

<p align="center">

:warning: **WARNING** :warning:

**This storage mechanism is NOT secure!**

**Secrets and credentials are stored in plaintext files _without any security_!<br/>
Git Credential Manager Core takes no liability for the safety of these
credentials.**

It is **HIGHLY RECOMMENDED** to always use one of the other credential store
options above. This option is only provided for compatibility and use in
environments where no other secure option is available.

If you chose to use this credential store, it is recommended you set the
permissions on this directory such that no other users or applications can
access files within. If possible, use a path that exists on an external volume
that you take with you and use full-disk encryption.

</p>

---
