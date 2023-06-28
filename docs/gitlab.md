# GitLab support

Git Credential Manager supports [gitlab.com][gitlab] out the box.

## Using on another instance

To use on another instance, eg. `https://gitlab.example.com` requires setup and
configuration:

1. [Create an OAuth application][gitlab-oauth]. This can be at the user, group
or instance level. Specify a name and use a redirect URI of `http://127.0.0.1/`.
_Unselect_ the 'Confidential' option. Set the 'read_repository' and
'write_repository' scopes.
1. Copy the application ID and configure
`git config --global credential.https://gitlab.example.com.gitLabDevClientId <APPLICATION_ID>`
1. Copy the application secret and configure
`git config --global credential.https://gitlab.example.com.gitLabDevClientSecret
<APPLICATION_SECRET>`
1. Optional if you want to force browser auth:
`git config --global credential.https://gitlab.example.com.gitLabAuthModes browser`
1. For good measure, configure
`git config --global credential.https://gitlab.example.com.provider gitlab`.
This may be necessary to recognise the domain as a GitLab instance.
1. Verify the config is as expected
`git config --global --get-urlmatch credential https://gitlab.example.com`

### Clearing config

```console
git config --global --unset-all credential.https://gitlab.example.com.gitLabDevClientId
git config --global --unset-all credential.https://gitlab.example.com.gitLabDevClientSecret
git config --global --unset-all credential.https://gitlab.example.com.provider
```

### Config for popular instances

For convenience, here are the config commands for several popular GitLab
instances, provided by community member [hickford](https://github.com/hickford/):

```console
# https://gitlab.freedesktop.org/
git config --global credential.https://gitlab.freedesktop.org.gitLabDevClientId 6503d8c5a27187628440d44e0352833a2b49bce540c546c22a3378c8f5b74d45
git config --global credential.https://gitlab.freedesktop.org.gitLabDevClientSecret 2ae9343a034ff1baadaef1e7ce3197776b00746a02ddf0323bb34aca8bff6dc1
# https://gitlab.gnome.org/
git config --global credential.https://gitlab.gnome.org.gitLabDevClientId adf21361d32eddc87bf6baf8366f242dfe07a7d4335b46e8e101303364ccc470
git config --global credential.https://gitlab.gnome.org.gitLabDevClientSecret cdca4678f64e5b0be9febc0d5e7aab0d81d27696d7adb1cf8022ccefd0a58fc0
# https://invent.kde.org/
git config --global credential.https://invent.kde.org.gitLabDevClientId cd7cb4342c7cd83d8c2fcc22c87320f88d0bde14984432ffca07ee24d0bf0699
git config --global credential.https://invent.kde.org.gitLabDevClientSecret 9cc8440b280c792ac429b3615ae1c8e0702e6b2479056f899d314f05afd94211
# https://salsa.debian.org/
git config --global credential.https://salsa.debian.org.gitLabDevClientId 213f5fd32c6a14a0328048c0a77cc12c19138cc165ab957fb83d0add74656f89
git config --global credential.https://salsa.debian.org.gitLabDevClientSecret 3616b974b59451ecf553f951cb7b8e6e3c91c6d84dd3247dcb0183dac93c2a26
# https://gitlab.haskell.org/
git config --global credential.https://gitlab.haskell.org.gitLabDevClientId 57de5eaab72b3dc447fca8c19cea39527a08e82da5377c2d10a8ebb30b08fa5f
git config --global credential.https://gitlab.haskell.org.gitLabDevClientSecret 5170a480da8fb7341e0daac94223d4fff549c702efb2f8873d950bb2b88e434f
# https://code.videolan.org/
git config --global credential.https://code.videolan.org.gitLabDevClientId f35c379241cc20bf9dffecb47990491b62757db4fb96080cddf2461eacb40375
git config --global credential.https://code.videolan.org.gitLabDevClientSecret 631558ec973c5ef65b78db9f41103f8247dc68d979c86f051c0fe4389e1995e8
```

See also [issue #677](https://github.com/git-ecosystem/git-credential-manager/issues/677).

## Preferences

```console
Select an authentication method for 'https://gitlab.com/':
  1. Web browser (default)
  2. Personal access token
  3. Username/password
option (enter for default):
```

If you have a preferred authentication mode, you can specify
[credential.gitLabAuthModes][config-gitlab-auth-modes]:

```console
git config --global credential.gitLabAuthModes browser
```

## Caveats

Improved support requires changes in GitLab. Please vote for these issues if
they affect you:

1. No support for OAuth device authorization (necessary for machines without web
browser): [GitLab issue 332682][gitlab-issue-332682]
1. Preconfigure Git Credential Manager as instance-wide OAuth application:
[GitLab issue 374172](gitlab-issue-374172)
1. Username/password authentication is suggested even if disabled on server:
[GitLab issue 349463][gitlab-issue-349463]

[config-gitlab-auth-modes]: configuration.md#credential.gitLabAuthModes
[gitlab]: https://gitlab.com
[gitlab-issue-332682]: https://gitlab.com/gitlab-org/gitlab/-/issues/332682
[gitlab-issue-374172]: https://gitlab.com/gitlab-org/gitlab/-/issues/374172
[gitlab-issue-349463]: https://gitlab.com/gitlab-org/gitlab/-/issues/349463
[gitlab-oauth]: https://docs.gitlab.com/ee/integration/oauth_provider.html
