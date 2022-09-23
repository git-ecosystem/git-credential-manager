# Gitea support

GCM requires Gitea v1.17.3 or later.

## Config for a custom instance

1. In Settings / Applications, create an OAuth application, unchecking the
*Confidential* option (if this option is missing, the version of Gitea is too
old)
1. Copy the application ID and configure
`git config --global credential.https://gitea.example.com.GiteaDevClientId <APPLICATION_ID>`
1. Copy the application secret and configure
`git config --global credential.https://gitea.example.com.GiteaDevClientSecret
<APPLICATION_SECRET>`
1. Configure authentication modes to include 'browser'
`git config --global credential.https://gitea.example.com.GiteaAuthModes browser`
1. For good measure, configure
`git config --global credential.https://gitea.example.com.provider gitea`.
This may be necessary to recognise the domain as a Gitea instance.
1. Verify the config is as expected
`git config --global --get-urlmatch credential https://gitea.example.com`

## Clearing config

### Clearing config

```console
    git config --global --unset-all credential.https://gitea.example.com.GiteaDevClientId
    git config --global --unset-all credential.https://gitea.example.com.GiteaDevClientSecret
    git config --global --unset-all credential.https://gitea.example.com.provider
```

## Preferences

```console
Select an authentication method for 'https://gitea.com/':
  1. Web browser (default)
  2. Personal access token
  3. Username/password
option (enter for default):
```

If you have a preferred authentication mode, you can specify
[credential.giteaAuthModes][config-gitea-auth-modes]:

```console
git config --global credential.giteaauthmodes browser
```
