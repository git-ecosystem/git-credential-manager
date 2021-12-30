# GitLab support

git-credential-manager supports the following public GitLab instances out the box:

* https://gitlab.com
* https://gitlab.freedesktop.org

If you'd like support for another puublic instance, please [open an issue](https://github.com/GitCredentialManager/git-credential-manager/issues).

## Using on a custom instance

To use on another instance, eg. `https://gitlab.example.com` requires setup and configuration:

1. [Create an OAuth application](https://docs.gitlab.com/ee/integration/oauth_provider.html). This can be at the user, group or instance level. Specify name `git-credential-manager` and redirect URI `http://127.0.0.1/`. Select options 'Confidential', 'Expire access tokens' and scope 'write_repository'.
2. Copy the application ID and configure `git config --global credential.https://gitlab.example.com.GitLabDevClientId <APPLICATION_ID>`
3. Copy the application secret and configure `git config --global credential.https://gitlab.example.com.GitLabDevClientSecret <APPLICATION_SECRET>`
4. For good measure, configure `git config --global credential.https://gitlab.example.com.provider gitlab`
5. Verify the config is as expected `git config --global --get-urlmatch credential https://gitlab.example.com`

### Clearing config

```
    git config --global --unset-all credential.https://gitlab.example.com.GitLabDevClientId
    git config --global --unset-all credential.https://gitlab.example.com.GitLabDevClientSecret
    git config --global --unset-all credential.https://gitlab.example.com.provider
```

## Preferences

```
Select an authentication method for 'https://gitlab.com/':
  1. Web browser (default)
  2. Personal access token
  3. Username/password
option (enter for default): 
```


If you have a preferred authentication mode, you can specify [credential.gitLabAuthModes](configuration.md#credential.gitLabAuthModes):

    `git config --global credential.gitlabauthmodes browser`

## Caveats

Improved support requires changes in GitLab. Please vote for these issues if they affect you:

1. No support for OAuth device authorization (necessary for machines without web browser) https://gitlab.com/gitlab-org/gitlab/-/issues/332682
2. Only domains with prefix `gitlab.` are recognised as GitLab remotes https://gitlab.com/gitlab-org/gitlab/-/issues/349464
3. Username/password authentication is suggested even if disabled on server https://gitlab.com/gitlab-org/gitlab/-/issues/349463
