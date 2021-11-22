# Multiple users

We're sometimes asked, "Does GCM support multiple users?" The answer is a bit complex (though ultimately, it's "yes").

## Foundations: Git and Git hosts

Git itself doesn't have a strong concept of "user". There's the `user.name` and `user.email` which get embedded into commit headers/trailers, but these are arbitrary strings. GCM doesn't interact with this notion of a user at all. You can put whatever you want into your `user.*` config, and nothing in GCM will change at all.

Git hosting providers (like GitHub or Bitbucket) _do_ have a concept of "user". Typically it's an identity like a username or email address, plus a password or other credential to perform actions as that user. You may have guessed by now that GCM (the Git **Credential** Manager) does work with this notion of a user.

## People, identities, credentials, oh my!

You (a physical person) may have one or more user accounts (identities) with one or more Git hosting providers. Since Git doesn't really understand what a "user" is, it's not particularly natural to work with multiple identities against a single hosting provider. By default, Git naturally assumes one identity per domain. If you have multiple identites on one domain (or a single identity on multiple domains!), this one-to-one assumption doesn't hold.

There are good reasons for having multiple identities on one domain. You might use one GitHub identity for your personal work, another for your open source work, and a third for your employer's work. You can "fool" Git into letting you assign a different credential to different repositories hosted on the same provider. HTTPS URLs include an optional "name" part before an `@` sign in the domain name, and you can use this to force Git to distiguish multiple users. This name doesn't need to be your actual username on the hosting service, though it might help you remember which credential you're using for that repository.

## Setting it up

As an example, let's say you're working on multiple repositories hosted at the same domain name.

| Repo URL | Identity |
|----------|----------|
| `https://example.com/open-source/library.git` | `contrib123` |
| `https://example.com/more-open-source/app.git` | `contrib123` |
| `https://example.com/big-company/secret-repo.git` | `employee9999` |

When you clone these repos, include the identity and an `@` before the domain name in order to force Git and GCM to use different identities. If you've already cloned the repos, you can update the remote URL to incude the identity.

(Reminder: the name you choose _does not_ have to be related to your identity on the Git hosting service. GCM will not use it directly; this is only a way to have Git request distinct identities when it calls GCM.)

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

[Azure DevOps has some additional, optional complexity](azrepos-users-and-tokens.md) which you should also be aware of if you're using it.
