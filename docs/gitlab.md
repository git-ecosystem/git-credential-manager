# GitLab support

Git Credential Manager supports [gitlab.com][gitlab] out the box.

## Using on a another instance

To use on another instance, eg. `https://gitlab.example.com` requires setup and configuration:

1. [Create an OAuth application][gitlab-oauth]. This can be at the user, group or instance level. Specify a name and use a redirect URI of `http://127.0.0.1/`. _Unselect_ the 'Confidential' option. Set the 'write_repository' and 'read_repository' scopes.
1. Copy the application ID and configure `git config --global credential.https://gitlab.example.com.GitLabDevClientId <APPLICATION_ID>`
1. Copy the application secret and configure `git config --global credential.https://gitlab.example.com.GitLabDevClientSecret <APPLICATION_SECRET>`
1. Configure authentication modes to include 'browser' `git config --global credential.https://gitlab.example.com.gitLabAuthModes browser`
1. For good measure, configure `git config --global credential.https://gitlab.example.com.provider gitlab`. This may be necessary to recognise the domain as a GitLab instance.
1. Verify the config is as expected `git config --global --get-urlmatch credential https://gitlab.example.com`

### Clearing config

```console
    git config --global --unset-all credential.https://gitlab.example.com.GitLabDevClientId
    git config --global --unset-all credential.https://gitlab.example.com.GitLabDevClientSecret
    git config --global --unset-all credential.https://gitlab.example.com.provider
```

## Preferences

```console
Select an authentication method for 'https://gitlab.com/':
  1. Web browser (default)
  2. Personal access token
  3. Username/password
option (enter for default):
```

If you have a preferred authentication mode, you can specify [credential.gitLabAuthModes][config-gitlab-auth-modes]:

```console
git config --global credential.gitlabauthmodes browser
```

## Caveats

Improved support requires changes in GitLab. Please vote for these issues if they affect you:

1. No support for OAuth device authorization (necessary for machines without web browser): [GitLab issue 332682][gitlab-issue-332682]
1. Only domains with prefix `gitlab.` are recognised as GitLab remotes: [GitLab issue 349464][gitlab-issue-349464]
1. Username/password authentication is suggested even if disabled on server: [GitLab issue 349463][gitlab-issue-349463]

[config-gitlab-auth-modes]: configuration.md#credential.gitLabAuthModes
[gitlab]: https://gitlab.com
[gitlab-issue-332682]: https://gitlab.com/gitlab-org/gitlab/-/issues/332682
[gitlab-issue-349464]: https://gitlab.com/gitlab-org/gitlab/-/issues/349464
[gitlab-issue-349463]: https://gitlab.com/gitlab-org/gitlab/-/issues/349463
[gitlab-oauth]: https://docs.gitlab.com/ee/integration/oauth_provider.html
