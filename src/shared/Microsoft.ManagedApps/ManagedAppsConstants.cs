using System;

namespace Microsoft.ManagedApps
{
    /// <summary>
    /// Constants for the Microsoft Managed Apps Git host provider.
    /// </summary>
    public static class ManagedAppsConstants
    {
        // Microsoft Entra ID authority base URL.
        public const string AadAuthorityBaseUrl = "https://login.microsoftonline.com/";

        public const string AadAuthoritySegment = "organizations";

        // Well-known public client ID for this integration.
        // This is a public/native client (no client secret) - not a secret value.
        public const string AadClientId = "c4ee713f-aede-4371-91fc-921aa3a5ded9";

        // Default loopback redirect URI.
        public static readonly Uri AadRedirectUri = new Uri("http://localhost");

        // Prefix for the `credential.managedAppsCloudEnvironment.<name>.*` configuration
        // subsection used to add or complete cloud environment definitions without a GCM
        // code change.
        public const string CloudEnvironmentConfigScopePrefix = "managedAppsCloudEnvironment.";

        public static class GitConfigCloudEnvironmentKeys
        {
            public const string HostSuffix = "hostSuffix";
            public const string Resource = "resource";
            public const string Scopes = "scopes";
        }

        public static class EnvironmentVariables
        {
            public const string DevAadClientId = "GCM_DEV_MANAGEDAPPS_CLIENTID";
            public const string DevAadRedirectUri = "GCM_DEV_MANAGEDAPPS_REDIRECTURI";
            public const string DevAadAuthorityBaseUri = "GCM_DEV_MANAGEDAPPS_AUTHORITYBASEURI";
            public const string ServicePrincipalId = "GCM_MANAGEDAPPS_SERVICE_PRINCIPAL";
            public const string ServicePrincipalSecret = "GCM_MANAGEDAPPS_SERVICE_PRINCIPAL_SECRET";
            public const string ServicePrincipalCertificateThumbprint = "GCM_MANAGEDAPPS_SERVICE_PRINCIPAL_CERT_THUMBPRINT";
            public const string ServicePrincipalCertificateSendX5C = "GCM_MANAGEDAPPS_SERVICE_PRINCIPAL_CERT_SEND_X5C";
            public const string ManagedIdentity = "GCM_MANAGEDAPPS_MANAGEDIDENTITY";
            public const string WorkloadFederation = "GCM_MANAGEDAPPS_WIF";
            public const string WorkloadFederationClientId = "GCM_MANAGEDAPPS_WIF_CLIENTID";
            public const string WorkloadFederationTenantId = "GCM_MANAGEDAPPS_WIF_TENANTID";
            public const string WorkloadFederationAudience = "GCM_MANAGEDAPPS_WIF_AUDIENCE";
            public const string WorkloadFederationAssertion = "GCM_MANAGEDAPPS_WIF_ASSERTION";
            public const string WorkloadFederationManagedIdentity = "GCM_MANAGEDAPPS_WIF_MANAGEDIDENTITY";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string DevAadClientId = "managedAppsDevClientId";
                public const string DevAadRedirectUri = "managedAppsDevRedirectUri";
                public const string DevAadAuthorityBaseUri = "managedAppsDevAuthorityBaseUri";
                public const string ServicePrincipal = "managedAppsServicePrincipal";
                public const string ServicePrincipalSecret = "managedAppsServicePrincipalSecret";
                public const string ServicePrincipalCertificateThumbprint = "managedAppsServicePrincipalCertificateThumbprint";
                public const string ServicePrincipalCertificateSendX5C = "managedAppsServicePrincipalCertificateSendX5C";
                public const string ManagedIdentity = "managedAppsManagedIdentity";
                public const string WorkloadFederation = "managedAppsWorkloadFederation";
                public const string WorkloadFederationClientId = "managedAppsWorkloadFederationClientId";
                public const string WorkloadFederationTenantId = "managedAppsWorkloadFederationTenantId";
                public const string WorkloadFederationAudience = "managedAppsWorkloadFederationAudience";
                public const string WorkloadFederationAssertion = "managedAppsWorkloadFederationAssertion";
                public const string WorkloadFederationManagedIdentity = "managedAppsWorkloadFederationManagedIdentity";
            }
        }
    }
}
