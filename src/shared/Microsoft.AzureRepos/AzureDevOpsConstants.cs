using System;

namespace Microsoft.AzureRepos
{
    internal static class AzureDevOpsConstants
    {
        // AAD environment authority base URL
        public const string AadAuthorityBaseUrl = "https://login.microsoftonline.com";

        // Azure DevOps's app ID + default scopes
        public const string AzureDevOpsResourceId = "499b84ac-1321-427f-aa17-267ca6975798";
        public static readonly string[] AzureDevOpsDefaultScopes = {$"{AzureDevOpsResourceId}/.default"};

        // Visual Studio's client ID
        // We share this to be able to consume existing access tokens from the VS caches
        public const string AadClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";

        // Redirect URI specified by the Visual Studio application configuration
        public static readonly Uri AadRedirectUri = new Uri("http://localhost");

        public const string VstsHostSuffix = ".visualstudio.com";
        public const string AzureDevOpsHost = "dev.azure.com";

        public const string VssResourceTenantHeader = "X-VSS-ResourceTenant";

        public const string PatCredentialType = "pat";
        public const string OAuthCredentialType = "oauth";

        public const string UrnScheme = "azrepos";
        public const string UrnOrgPrefix = "org";

        public static class PersonalAccessTokenScopes
        {
            public const string ReposWrite = "vso.code_write";
            public const string ArtifactsRead = "vso.packaging";
        }

        public static class EnvironmentVariables
        {
            public const string DevAadClientId = "GCM_DEV_AZREPOS_CLIENTID";
            public const string DevAadRedirectUri = "GCM_DEV_AZREPOS_REDIRECTURI";
            public const string DevAadAuthorityBaseUri = "GCM_DEV_AZREPOS_AUTHORITYBASEURI";
            public const string CredentialType = "GCM_AZREPOS_CREDENTIALTYPE";
            public const string ServicePrincipalId = "GCM_AZREPOS_SERVICE_PRINCIPAL";
            public const string ServicePrincipalSecret = "GCM_AZREPOS_SP_SECRET";
            public const string ServicePrincipalCertificateThumbprint = "GCM_AZREPOS_SP_CERT_THUMBPRINT";
            public const string ServicePrincipalCertificateSendX5C = "GCM_AZREPOS_SP_CERT_SEND_X5C";
            public const string ManagedIdentity = "GCM_AZREPOS_MANAGEDIDENTITY";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string DevAadClientId = "azreposDevClientId";
                public const string DevAadRedirectUri = "azreposDevRedirectUri";
                public const string DevAadAuthorityBaseUri = "azreposDevAuthorityBaseUri";
                public const string CredentialType = "azreposCredentialType";
                public const string AzureAuthority = "azureAuthority";
                public const string ServicePrincipal = "azreposServicePrincipal";
                public const string ServicePrincipalSecret = "azreposServicePrincipalSecret";
                public const string ServicePrincipalCertificateThumbprint = "azreposServicePrincipalCertificateThumbprint";
                public const string ServicePrincipalCertificateSendX5C = "azreposServicePrincipalCertificateSendX5C";
                public const string ManagedIdentity = "azreposManagedIdentity";
            }
        }
    }
}
