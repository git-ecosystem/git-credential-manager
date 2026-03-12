# Azure Workload Identity Federation

Git Credential Manager supports [Workload Identity Federation][wif] for
authentication with Azure Repos. This document provides an overview of Workload
Identity Federation and how to use it with GCM.

## Overview

Workload Identity Federation allows a workload (such as a CI/CD pipeline, VM, or
container) to exchange a token from an external identity provider for a Microsoft
Entra ID access token — without needing to manage secrets like client secrets or
certificates.

This is especially useful in scenarios where:

- You want to avoid storing long-lived secrets.
- Your workload already has an identity token from another provider (e.g., GitHub
  Actions OIDC, a Managed Identity, or a custom identity provider).
- You want to follow the principle of least privilege with short-lived,
  automatically rotated credentials.

You can read more about Workload Identity Federation in the
[Microsoft Entra documentation][wif].

## How it works

When configured, GCM obtains a client assertion (a token from the external
identity provider) and exchanges it with Microsoft Entra ID for an access token
scoped to Azure DevOps. The exact mechanism for obtaining the client assertion
depends on the federation scenario you choose.

## Scenarios

GCM supports three federation scenarios:

### Generic

Use this scenario when you have a pre-obtained client assertion token from any
external identity provider. You provide the assertion directly and GCM exchanges
it for an access token.

**Required settings:**

Setting|Git Configuration|Environment Variable
-|-|-
Scenario|[`credential.azreposWorkloadFederation`][gcm-wif-config]|[`GCM_AZREPOS_WIF`][gcm-wif-env]
Client ID|[`credential.azreposWorkloadFederationClientId`][gcm-wif-clientid-config]|[`GCM_AZREPOS_WIF_CLIENTID`][gcm-wif-clientid-env]
Tenant ID|[`credential.azreposWorkloadFederationTenantId`][gcm-wif-tenantid-config]|[`GCM_AZREPOS_WIF_TENANTID`][gcm-wif-tenantid-env]
Assertion|[`credential.azreposWorkloadFederationAssertion`][gcm-wif-assertion-config]|[`GCM_AZREPOS_WIF_ASSERTION`][gcm-wif-assertion-env]

**Optional settings:**

Setting|Git Configuration|Environment Variable
-|-|-
Audience|[`credential.azreposWorkloadFederationAudience`][gcm-wif-audience-config]|[`GCM_AZREPOS_WIF_AUDIENCE`][gcm-wif-audience-env]

#### Example

```shell
git config --global credential.azreposWorkloadFederation generic
git config --global credential.azreposWorkloadFederationClientId "11111111-1111-1111-1111-111111111111"
git config --global credential.azreposWorkloadFederationTenantId "22222222-2222-2222-2222-222222222222"
git config --global credential.azreposWorkloadFederationAssertion "eyJhbGci..."
```

### Managed Identity

Use this scenario when your workload runs on an Azure resource that has a
[Managed Identity][az-mi] assigned. GCM will first request a token from the
Managed Identity for the configured audience, then exchange that token for an
Azure DevOps access token.

This is useful for Azure VMs, App Services, or other Azure resources that have a
Managed Identity but need to authenticate as a specific app registration with
a federated credential trust.

**Required settings:**

Setting|Git Configuration|Environment Variable
-|-|-
Scenario|[`credential.azreposWorkloadFederation`][gcm-wif-config]|[`GCM_AZREPOS_WIF`][gcm-wif-env]
Client ID|[`credential.azreposWorkloadFederationClientId`][gcm-wif-clientid-config]|[`GCM_AZREPOS_WIF_CLIENTID`][gcm-wif-clientid-env]
Tenant ID|[`credential.azreposWorkloadFederationTenantId`][gcm-wif-tenantid-config]|[`GCM_AZREPOS_WIF_TENANTID`][gcm-wif-tenantid-env]
Managed Identity|[`credential.azreposWorkloadFederationManagedIdentity`][gcm-wif-mi-config]|[`GCM_AZREPOS_WIF_MANAGEDIDENTITY`][gcm-wif-mi-env]

**Optional settings:**

Setting|Git Configuration|Environment Variable
-|-|-
Audience|[`credential.azreposWorkloadFederationAudience`][gcm-wif-audience-config]|[`GCM_AZREPOS_WIF_AUDIENCE`][gcm-wif-audience-env]

The Managed Identity value accepts the same formats as
[`credential.azreposManagedIdentity`][gcm-mi-config]:

Value|Description
-|-
`system`|System-Assigned Managed Identity
`[guid]`|User-Assigned Managed Identity with the specified client ID
`id://[guid]`|User-Assigned Managed Identity with the specified client ID
`resource://[guid]`|User-Assigned Managed Identity for the associated resource

#### Example

```shell
git config --global credential.azreposWorkloadFederation managedidentity
git config --global credential.azreposWorkloadFederationClientId "11111111-1111-1111-1111-111111111111"
git config --global credential.azreposWorkloadFederationTenantId "22222222-2222-2222-2222-222222222222"
git config --global credential.azreposWorkloadFederationManagedIdentity system
```

### GitHub Actions

Use this scenario when your workload runs in a GitHub Actions workflow. GCM will
automatically obtain an OIDC token from the GitHub Actions runtime and exchange
it for an Azure DevOps access token.

This scenario uses the `ACTIONS_ID_TOKEN_REQUEST_URL` and
`ACTIONS_ID_TOKEN_REQUEST_TOKEN` environment variables that GitHub Actions
automatically provides when a workflow has the `id-token: write` permission.

**Required settings:**

Setting|Git Configuration|Environment Variable
-|-|-
Scenario|[`credential.azreposWorkloadFederation`][gcm-wif-config]|[`GCM_AZREPOS_WIF`][gcm-wif-env]
Client ID|[`credential.azreposWorkloadFederationClientId`][gcm-wif-clientid-config]|[`GCM_AZREPOS_WIF_CLIENTID`][gcm-wif-clientid-env]
Tenant ID|[`credential.azreposWorkloadFederationTenantId`][gcm-wif-tenantid-config]|[`GCM_AZREPOS_WIF_TENANTID`][gcm-wif-tenantid-env]

**Optional settings:**

Setting|Git Configuration|Environment Variable
-|-|-
Audience|[`credential.azreposWorkloadFederationAudience`][gcm-wif-audience-config]|[`GCM_AZREPOS_WIF_AUDIENCE`][gcm-wif-audience-env]

No additional GCM settings are required — the GitHub Actions OIDC environment
variables are read automatically.

#### Prerequisites

1. An app registration in Microsoft Entra ID with a federated credential
   configured to trust your GitHub repository.
2. The app registration must have the necessary permissions to access Azure
   DevOps.
3. Your GitHub Actions workflow must have the `id-token: write` permission.

#### Example workflow

```yaml
permissions:
  id-token: write
  contents: read

steps:
  - uses: actions/checkout@v4
    env:
      GCM_AZREPOS_WIF: githubactions
      GCM_AZREPOS_WIF_CLIENTID: "11111111-1111-1111-1111-111111111111"
      GCM_AZREPOS_WIF_TENANTID: "22222222-2222-2222-2222-222222222222"
```

## Audience

All scenarios accept an optional audience setting that controls the audience
claim in the federated token request. The default value is
`api://AzureADTokenExchange`, which is the standard audience for Microsoft Entra
ID workload identity federation.

You only need to change this if your federated credential trust is configured
with a custom audience.

[az-mi]: https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/overview
[wif]: https://learn.microsoft.com/en-us/entra/workload-id/workload-identity-federation
[gcm-mi-config]: https://gh.io/gcm/config#credentialazreposmanagedidentity
[gcm-wif-config]: https://gh.io/gcm/config#credentialazreposworkloadfederation
[gcm-wif-clientid-config]: https://gh.io/gcm/config#credentialazreposworkloadfederationclientid
[gcm-wif-tenantid-config]: https://gh.io/gcm/config#credentialazreposworkloadfederationtenantid
[gcm-wif-audience-config]: https://gh.io/gcm/config#credentialazreposworkloadfederationaudience
[gcm-wif-assertion-config]: https://gh.io/gcm/config#credentialazreposworkloadfederationassertion
[gcm-wif-mi-config]: https://gh.io/gcm/config#credentialazreposworkloadfederationmanagedidentity
[gcm-wif-env]: https://gh.io/gcm/env#GCM_AZREPOS_WIF
[gcm-wif-clientid-env]: https://gh.io/gcm/env#GCM_AZREPOS_WIF_CLIENTID
[gcm-wif-tenantid-env]: https://gh.io/gcm/env#GCM_AZREPOS_WIF_TENANTID
[gcm-wif-audience-env]: https://gh.io/gcm/env#GCM_AZREPOS_WIF_AUDIENCE
[gcm-wif-assertion-env]: https://gh.io/gcm/env#GCM_AZREPOS_WIF_ASSERTION
[gcm-wif-mi-env]: https://gh.io/gcm/env#GCM_AZREPOS_WIF_MANAGEDIDENTITY
