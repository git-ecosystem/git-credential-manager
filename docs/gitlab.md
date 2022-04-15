# GitLab support

Git Credential Manager supports [gitlab.com](https://gitlab.com) out the box.

## Using on a another instance

To use on another instance, eg. `https://gitlab.example.com` requires setup and configuration:

1. [Create an OAuth application](https://docs.gitlab.com/ee/integration/oauth_provider.html). This can be at the user, group or instance level. Specify a name and use a redirect URI of `http://127.0.0.1/`. _Unselect_ the 'Confidential' option, and ensure the 'Expire access tokens' option is selected. Set the scope to 'write_repository'.
2. Copy the application ID and configure `git config --global credential.https://gitlab.example.com.GitLabDevClientId <APPLICATION_ID>`
3. Copy the application secret and configure `git config --global credential.https://gitlab.example.com.GitLabDevClientSecret <APPLICATION_SECRET>`
4. Configure authentication modes to include 'browser' `git config --global credential.https://gitlab.example.com.gitLabAuthModes browser`
5. For good measure, configure `git config --global credential.https://gitlab.example.com.provider gitlab`. This may be necessary to recognise the domain as a GitLab instance.
6. Verify the config is as expected `git config --global --get-urlmatch credential https://gitlab.example.com`

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

If you have a preferred authentication mode, you can specify [credential.gitLabAuthModes](configuration.md#credential.gitLabAuthModes):

```console
git config --global credential.gitlabauthmodes browser
```

## Caveats

Improved support requires changes in GitLab. Please vote for these issues if they affect you:

1. No support for OAuth device authorization (necessary for machines without web browser) https://gitlab.com/gitlab-org/gitlab/-/issues/332682
2. Only domains with prefix `gitlab.` are recognised as GitLab remotes https://gitlab.com/gitlab-org/gitlab/-/issues/349464
3. Username/password authentication is suggested even if disabled on server https://gitlab.com/gitlab-org/gitlab/-/issues/349463
