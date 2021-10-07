# Host provider auto-detection

Git Credential Manager (GCM) supports authentication with multiple different Git
host providers including: GitHub, Bitbucket, and Azure Repos. As well as the
hosted/cloud offerings, GCM can also work with the self-hosted or "on-premises"
versions of these services: GitHub Enterprise Server, Bitbucket DC Server, and
Azure DevOps Server (TFS).

By default, GCM will attempt to automatically detect which particular provider
is behind the Git remote URL you're interacting with. For the cloud versions of
the supported providers this is done by matching the hostname of the remote URL
to the well-known hostnames of the services. For example "github.com" or
"dev.azure.com".

## Self-hosted/on-prem detection

In order to detect which host provider to use for a self-hosted instance, each
provider can provide some heuristic matching of the hostname. For example any
hostname that begins "github.*" will be matched to the GitHub host provider.

If a heuristic matches incorrectly, you can always [explicitly configure](#explicit-configuration)
GCM to use a particular provider.

## Remote URL probing

In addition to heuristic matching, GCM will make a network call to the remote
URL and inspect HTTP response headers to try and detect a self-hosted instance.

This network call is only performed if neither an exact nor fuzzy match by
hostname can be made. Only one HTTP `HEAD` call is made per credential request
received by Git. To avoid this network call, please [explicitly configure](#explicit-configuration)
the host provider for your self-hosted instance.

### Timeout

You can control how long GCM will wait for a response to the remote network call
by setting the [`GCM_AUTODETECT_TIMEOUT`](environment.md#GCM_AUTODETECT_TIMEOUT)
environment variable, or the [`credential.autoDetectTimeout`](configuration.md#credentialautodetecttimeout)
Git configuration setting to the maximum number of milliseconds to wait.

The default value is 2000 milliseconds (2 seconds). You can prevent the network
call altogether by setting a zero or negative value, for example -1.

## Explicit configuration

If the auto-detection mechanism fails to select the correct host provider, or
if the remote probing network call is causing performance issues, you can
configure GCM to always use a particular host provider, for a given remote URL.

You can either use the the [`GCM_PROVIDER`](environment.md#GCM_PROVIDER)
environment variable, or the [`credential.provider`](configuration.md#credentialprovider)
Git configuration setting for this purpose.

For example to tell GCM to always use the GitHub host provider for the
"ghe.example.com" hostname, you can run the following command:

```shell
git config --global credential.ghe.example.com.provider github
```
