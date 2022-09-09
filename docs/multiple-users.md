# Multiple users

If you work with multiple different identities on a single Git hosting service,
you may be wondering if Git Credential Manager (GCM) supports this workflow. The
answer is yes, with a bit of complexity due to how it interoperates with Git.

## Foundations: Git and Git hosts

Git itself doesn't have a single, strong concept of "user". There's the
`user.name` and `user.email` which get embedded into commit headers/trailers,
but these are arbitrary strings. GCM doesn't interact with this notion of a user
at all. You can put whatever you want into your `user.*` config, and nothing in
GCM will change at all.

Separate from the user strings in commits, Git recognizes the "user" part of a
remote URL or a credential. These are not often used, at least by default, in
the web UI of major Git hosts.

Git hosting providers (like GitHub or Bitbucket) _do_ have a concept of "user".
Typically it's an identity like a username or email address, plus a password or
other credential to perform actions as that user. You may have guessed by now
that GCM (the Git **Credential** Manager) does work with this notion of a user.

## People, identities, credentials, oh my

You (a physical person) may have one or more user accounts (identities) with one
or more Git hosting providers. Since most Git hosts don't put a "user" part in
their URLs, by default, Git will treat the user part for a remote as the empty
string. If you have multiple identities on one domain, you'll need to insert a
unique user part per-identity yourself.

There are good reasons for having multiple identities on one domain. You might
use one GitHub identity for your personal work, another for your open source
work, and a third for your employer's work. You can ask Git to assign a
different credential to different repositories hosted on the same provider.
HTTPS URLs include an optional "name" part before an `@` sign in the domain
name, and you can use this to force Git to distinguish multiple users. This
should likely be your username on the Git hosting service, since there are
cases where GCM will use it like a username.

## Setting it up

As an example, let's say you're working on multiple repositories hosted at the
same domain name.

| Repo URL | Identity |
|----------|----------|
| `https://example.com/open-source/library.git` | `contrib123` |
| `https://example.com/more-open-source/app.git` | `contrib123` |
| `https://example.com/big-company/secret-repo.git` | `employee9999` |

When you clone these repos, include the identity and an `@` before the domain
name in order to force Git and GCM to use different identities. If you've
already cloned the repos, you can update the remote URL to include the identity.

### Example: fresh clones

```shell
# instead of `git clone https://example.com/open-source/library.git`, run:
git clone https://contrib123@example.com/open-source/library.git

# instead of `git clone https://example.com/big-company/secret-repo.git`, run:
git clone https://employee9999@example.com/big-company/secret-repo.git
```

### Example: existing clones

```shell
# in the `library` repo, run:
git remote set-url origin https://contrib123@example.com/open-source/library.git

# in the `secret-repo` repo, run:
git remote set-url origin https://employee9999@example.com/big-company/secret-repo.git
```

## Azure DevOps

[Azure DevOps has some additional, optional complexity][azure-access-tokens]
which you should also be aware of if you're using it.

[azure-access-tokens]: azrepos-users-and-tokens.md
