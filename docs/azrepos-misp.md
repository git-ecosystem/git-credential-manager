# Azure Managed Identities and Service Principals

Git Credential Manager supports Managed Identities and Service Principals for
authentication with Azure Repos. This document provides an overview of Managed
Identities and Service Principals and how to use them with GCM.

## Managed Identities

Azure Managed Identities can be used to authenticate and authorize applications
and services to access Azure resources. Managed Identities are a secure way to
access Azure resources without needing to store credentials in code or
configuration files.

There are two types of Managed Identities:

**System-assigned**

System-assigned Managed Identities are tied to a specific Azure resource, such
as a Virtual Machine or App Service. When a system-assigned Managed Identity is
enabled, Azure creates an identity for the resource in the Azure AD tenant
that's trusted by the subscription. The lifecycle of the identity is tied to the
resource to which it's assigned.

**User-assigned**

User-assigned Managed Identities are created as standalone Azure resources and
can be assigned to one or more Azure resources. This allows you to use the same
Managed Identity across multiple resources.

You can read more about Managed Identities in the
[Azure documentation][az-mi].

### How to configure Managed Identities

In order to use a Managed Identity with GCM, you need to ensure that the Managed
Identity has the necessary permissions to access the Azure Repos repository.

You can read more about how to configure Managed Identities in the
[Azure Repos documentation][azdo-misp].

Once you have configured the Managed Identity, you can use it with GCM by simply
setting one of the following environment variables or Git configuration options:

**Git configuration:** [`credential.azreposManagedIdentity`][gcm-mi-config]

**Environment variable:** [`GCM_AZREPOS_MANAGEDIDENTITY`][gcm-mi-env]

Value|Description
-|-
`system`|System-Assigned Managed Identity
`[guid]`|User-Assigned Managed Identity with the specified client ID
`id://[guid]` **|User-Assigned Managed Identity with the specified client ID
`resource://[guid]` **|User-Assigned Managed Identity for the associated resource

You can obtain the `[guid]` from the Azure Portal or by using the Azure CLI
to inspect the Managed Identity or resource.

** Note there is an open issue that prevents successfull authentication when
using these formats: https://github.com/git-ecosystem/git-credential-manager/issues/1570

## Service Principals

Azure Service Principals are used to authenticate and authorize applications and
services to access Azure resources. Service Principals are similar in many ways
to Managed Identities (in fact Service Principals are used under the hood to
implement Managed Identities), but they have expliclty defined credentials that
are not managed by Azure.

There are a number of different ways to create and configure Service Principals,
including using the Azure Portal or Azure CLI. You can read more about Service
Principals in the [Azure documentation][az-sp].

### How to configure Service Principals

Much like with Managed Identities, to use a Service Principal with GCM you first
need to ensure that the principal has the necessary permissions to access the
Azure Repos repository.

You can read more about how to configure Service Principals in the
[Azure Repos documentation][azdo-misp].

Once you have configured the Service Principal, you can use it with GCM by
setting one of the following environment variables or Git configuration options:

**Git configuration:** [`credential.azreposServicePrincipal`][gcm-sp-config]

**Environment variable:** [`GCM_AZREPOS_SERVICE_PRINCIPAL`][gcm-sp-env]

The format of the value for these options must be in the format:

```text
{tenantId}/{clientId}
```

Where `{tenantId}` is the Azure tenant ID and `{clientId}` is the client ID of
the Service Principal. These values can be found in the Azure Portal or by using
the Azure CLI to inspect the Service Principal.

#### Authentication with Service Principals

When using a Service Principal with GCM, you will also need to provide the
client secret or certificate that is associated with the Service Principal.

You can provide the client secret or certificate to GCM by setting one of the
following environment variables or Git configuration options.

Type|Git Configuration|Environment Variable
-|-|-
Client Secret|[`credential.azreposServicePrincipalSecret`][gcm-sp-secret-config]|[`GCM_AZREPOS_SP_SECRET`][gcm-sp-secret-env]
Certificate|[`credential.azreposServicePrincipalCertificateThumbprint`][gcm-sp-cert-config]|[`GCM_AZREPOS_SP_CERT_THUMBPRINT`][gcm-sp-cert-env]

The value for these options should be the client secret or the thumbrint of the
certificate that is associated with the Service Principal.

The certificate itself should be installed on the machine where GCM is running
and should be installed in personal store the certificate store for either the
current user or the local machine.

[az-mi]: https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/overview
[az-sp]: https://learn.microsoft.com/en-us/entra/identity-platform/app-objects-and-service-principals?tabs=browser
[azdo-misp]: https://learn.microsoft.com/en-us/azure/devops/integrate/get-started/authentication/service-principal-managed-identity?view=azure-devops
[gcm-mi-config]: https://gh.io/gcm/config#credentialazreposmanagedidentity
[gcm-mi-env]: https://gh.io/gcm/env#GCM_AZREPOS_MANAGEDIDENTITY
[gcm-sp-config]: https://gh.io/gcm/config#credentialazreposserviceprincipal
[gcm-sp-env]: https://gh.io/gcm/env#GCM_AZREPOS_SERVICE_PRINCIPAL
[gcm-sp-secret-config]: https://gh.io/gcm/config#credentialazreposserviceprincipalsecret
[gcm-sp-secret-env]: https://gh.io/gcm/env#GCM_AZREPOS_SP_SECRET
[gcm-sp-cert-config]: https://gh.io/gcm/config#credentialazreposserviceprincipalcertificatethumbprint
[gcm-sp-cert-env]: https://gh.io/gcm/env#GCM_AZREPOS_SP_CERT_THUMBPRINT
