// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;

namespace Microsoft.AzureRepos
{
    public class AzureReposHostProvider : HostProvider
    {
        private readonly IAzureDevOpsRestApi _azDevOps;
        private readonly IMicrosoftAuthentication _msAuth;
        private readonly IAzureReposAuthorityCache _authorityCache;

        public AzureReposHostProvider(ICommandContext context)
            : this(context,
                new AzureDevOpsRestApi(context),
                new MicrosoftAuthentication(context),
                AzureDevOpsConstants.CreateIniDataStore(context?.FileSystem))
        { }

        public AzureReposHostProvider(
            ICommandContext context,
            IAzureDevOpsRestApi azDevOps,
            IMicrosoftAuthentication msAuth,
            ITransactionalValueStore<string, string> dataStore)
            : this(context, azDevOps, msAuth,
                new AzureReposAuthorityCache(context?.Trace, dataStore))
        { }

        public AzureReposHostProvider(
            ICommandContext context,
            IAzureDevOpsRestApi azDevOps,
            IMicrosoftAuthentication msAuth,
            IAzureReposAuthorityCache authorityCache)
            : base(context)
        {
            EnsureArgument.NotNull(azDevOps, nameof(azDevOps));
            EnsureArgument.NotNull(msAuth, nameof(msAuth));
            EnsureArgument.NotNull(authorityCache, nameof(authorityCache));

            _azDevOps = azDevOps;
            _msAuth = msAuth;
            _authorityCache = authorityCache;
        }

        #region HostProvider

        public override string Id => "azure-repos";

        public override string Name => "Azure Repos";

        public override IEnumerable<string> SupportedAuthorityIds => MicrosoftAuthentication.AuthorityIds;

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
            Uri remoteUri = input.GetRemoteUri();
            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri);
            return $"git:{orgUri.AbsoluteUri}";
        }

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for Azure Repos. Ensure the repository remote URL is using HTTPS.");
            }

            Uri remoteUri = input.GetRemoteUri();
            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string orgName);

            // Determine the MS authentication authority for this organization
            string authority = _authorityCache.GetAuthority(orgName);
            if (authority is null)
            {
                Context.Trace.WriteLine("No authority found in cache; querying server...");
                authority = await _azDevOps.GetAuthorityAsync(orgUri);

                // Update our cache
                _authorityCache.UpdateAuthority(orgName, authority);
            }
            Context.Trace.WriteLine($"Authority for '{orgName}' is '{authority}'.");

            // Get an AAD access token for the Azure DevOps SPS
            Context.Trace.WriteLine("Getting Azure AD access token...");
            string accessToken = await _msAuth.GetAccessTokenAsync(
                authority,
                AzureDevOpsConstants.AadClientId,
                AzureDevOpsConstants.AadRedirectUri,
                AzureDevOpsConstants.AadResourceId,
                remoteUri);
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

        public override Task EraseCredentialAsync(InputArguments input)
        {
            // We should clear out the cached authority for this organization in case the reason for
            // the authentication failure was using old or incorrect data to generate the credentials.
            Uri remoteUri = input.GetRemoteUri();
            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            _authorityCache.EraseAuthority(orgName);

            return base.EraseCredentialAsync(input);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _azDevOps.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
