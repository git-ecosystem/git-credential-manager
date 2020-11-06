// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.AzureRepos
{
    internal static class AzureDevOpsConstants
    {
        // Azure DevOps's resource ID
        public const string AadResourceId = "499b84ac-1321-427f-aa17-267ca6975798";

        // Visual Studio's client ID
        // We share this to be able to consume existing access tokens from the VS caches
        public const string AadClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";

        // Redirect URI specified by the Visual Studio application configuration
        public static readonly Uri AadRedirectUri = new Uri("http://localhost");

        public const string VstsHostSuffix = ".visualstudio.com";
        public const string AzureDevOpsHost = "dev.azure.com";

        public const string VssResourceTenantHeader = "X-VSS-ResourceTenant";

        public static class PersonalAccessTokenScopes
        {
            public const string ReposWrite = "vso.code_write";
            public const string ArtifactsRead = "vso.packaging";
        }
    }
}
