# Git remotes and authentication

Git Credential Manager (GCM) handles authentication for Git remotes that use
HTTP or HTTPS. It is invoked by Git when a remote operation needs credentials;
you normally do not need to run GCM directly.

## HTTPS remotes

HTTPS remotes work through firewalls and proxies in many environments and are
the recommended choice when you want GCM to provide the sign-in experience.
For example:

```shell
git clone https://github.com/OWNER/REPOSITORY.git
```

When the server requests authentication, GCM opens the provider's sign-in
flow, including multi-factor authentication where supported. GCM stores the
result in the configured [credential store][credential-stores] and reuses it
for later Git operations while it remains valid.

Do not put passwords or access tokens directly in a remote URL. If a provider
requires a token for HTTPS authentication, enter it only when Git or GCM
prompts for credentials, and follow that provider's token and single sign-on
requirements.

## SSH remotes

SSH remotes do not use GCM. Configure an SSH key with your Git hosting provider
and use the provider's SSH URL, for example:

```shell
git clone git@github.com:OWNER/REPOSITORY.git
```

See your provider's documentation for creating, registering, and authorizing an
SSH key. If SSH connectivity is unavailable in your environment, use an HTTPS
remote instead.

## Changing a remote

To inspect the remotes configured for a repository:

```shell
git remote -v
```

To switch an existing repository between HTTPS and SSH, replace `origin` with
the URL you want:

```shell
git remote set-url origin https://github.com/OWNER/REPOSITORY.git
```

```shell
git remote set-url origin git@github.com:OWNER/REPOSITORY.git
```

[credential-stores]: credstores.md
