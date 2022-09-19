# Migration Guide

## Migrating from Git Credential Manager for Windows

### GCM_AUTHORITY

This setting (and the corresponding `credential.authority` configuration) is
deprecated and should be replaced with the `GCM_PROVIDER` (or corresponding
`credential.authority` configuration) setting.

Because both Basic HTTP authentication and Windows Integrated Authentication
(WIA) are now handled by one provider, if you specified `basic` as your
authority you also need to disable WIA using `GCM_ALLOW_WINDOWSAUTH` /
`credential.allowWindowsAuth`.

The following table shows the correct replacement for all legacy authorities
values:

GCM_AUTHORITY (credential.authority)|&rarr;|GCM_PROVIDER (credential.provider)|GCM_ALLOW_WINDOWSAUTH (credential.allowWindowsAuth)
-|-|-|-
`msa`, `microsoft`, `microsoftaccount`, `aad`, `azure`, `azuredirectory`, `live`, `liveconnect`, `liveid`|&rarr;|`azure-repos`|_N/A_
`github`|&rarr;|`github`|_N/A_
`basic`|&rarr;|`generic`|`false`
`integrated`, `windows`, `kerberos`, `ntlm`, `tfs`, `sso`|&rarr;|`generic`|`true` _(default)_

For example if you had previous set the authority for the `example.com` host to
`basic`..

```shell
git config --global credential.example.com.authority basic
```

..then you can replace this with the following..

```shell
git config --global --unset credential.example.com.authority
git config --global credential.example.com.provider generic
git config --global credential.example.com.allowWindowsAuth false
```
