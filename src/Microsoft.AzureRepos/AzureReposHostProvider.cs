// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;

namespace Microsoft.AzureRepos
{
    public class AzureReposHostProvider : HostProvider
    {
        private readonly IAzureDevOpsRestApi _azDevOps;
        private readonly IMicrosoftAuthentication _msAuth;

        public AzureReposHostProvider(ICommandContext context)
            : this(context, new AzureDevOpsRestApi(context), new OutOfProcHelperMicrosoftAuthentication(context)) { }

        public AzureReposHostProvider(ICommandContext context, IAzureDevOpsRestApi azDevOps, IMicrosoftAuthentication msAuth)
            : base (context)
        {
            EnsureArgument.NotNull(azDevOps, nameof(azDevOps));
            EnsureArgument.NotNull(msAuth, nameof(msAuth));

            _azDevOps = azDevOps;
            _msAuth = msAuth;
        }

        #region HostProvider

        public override string Name => "Azure Repos";

        public override bool IsSupported(InputArguments input)
        {
            // We do not support unencrypted HTTP communications to Azure Repos,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            return (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                   StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https")) &&
                   UriHelpers.IsAzureDevOpsHost(input.Host);
        }

        public override string GetCredentialKey(InputArguments input)
        {
            return UriHelpers.CreateOrganizationUri(input).AbsoluteUri;
        }

        public override async Task<GitCredential> CreateCredentialAsync(InputArguments input)
        {
            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for Azure Repos - ensure you are using HTTPS.");
            }

            Uri orgUri = UriHelpers.CreateOrganizationUri(input);

            // Determine the MS authentication authority for this organization
            Context.Trace.WriteLine("Determining Microsoft Authentication Authority...");
            string authAuthority = await _azDevOps.GetAuthorityAsync(orgUri);
            Context.Trace.WriteLine($"Authority is '{authAuthority}'.");

            // Get an AAD access token for the Azure DevOps SPS
            Context.Trace.WriteLine("Getting Azure AD access token...");
            string accessToken = await _msAuth.GetAccessTokenAsync(
                authAuthority,
                AzureDevOpsConstants.AadClientId,
                AzureDevOpsConstants.AadRedirectUri,
                AzureDevOpsConstants.AadResourceId);
            Context.Trace.WriteLineSecrets("Acquired access token. Token='{0}'", new object[] {accessToken});

            // Ask the Azure DevOps instance to create a new PAT
            var patScopes = new[]
            {
                AzureDevOpsConstants.PersonalAccessTokenScopes.ReposWrite,
                AzureDevOpsConstants.PersonalAccessTokenScopes.ArtifactsRead
            };
            Context.Trace.WriteLine($"Creating Azure DevOps PAT with scopes '{string.Join(", ", patScopes)}'...");
            string pat = await _azDevOps.CreatePersonalAccessTokenAsync(
                orgUri,
                accessToken,
                patScopes);
            Context.Trace.WriteLineSecrets("PAT created. PAT='{0}'", new object[] {pat});

            return new GitCredential(Constants.PersonalAccessTokenUserName, pat);
        }

        #endregion
    }
}
