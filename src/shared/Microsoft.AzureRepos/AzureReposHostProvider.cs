// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using KnownGitCfg = Microsoft.Git.CredentialManager.Constants.GitConfiguration;

namespace Microsoft.AzureRepos
{
    public class AzureReposHostProvider : HostProvider, IConfigurableComponent
    {
        private readonly IAzureDevOpsRestApi _azDevOps;
        private readonly IMicrosoftAuthentication _msAuth;

        public AzureReposHostProvider(ICommandContext context)
            : this(context, new AzureDevOpsRestApi(context), new MicrosoftAuthentication(context))
        {
        }

        public AzureReposHostProvider(ICommandContext context, IAzureDevOpsRestApi azDevOps,
            IMicrosoftAuthentication msAuth)
            : base(context)
        {
            EnsureArgument.NotNull(azDevOps, nameof(azDevOps));
            EnsureArgument.NotNull(msAuth, nameof(msAuth));

            _azDevOps = azDevOps;
            _msAuth = msAuth;
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
            return $"git:{UriHelpers.CreateOrganizationUri(input).AbsoluteUri}";
        }

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for Azure Repos. Ensure the repository remote URL is using HTTPS.");
            }

            Uri orgUri = UriHelpers.CreateOrganizationUri(input);
            Uri remoteUri = input.GetRemoteUri();

            // Determine the MS authentication authority for this organization
            Context.Trace.WriteLine("Determining Microsoft Authentication Authority...");
            string authAuthority = await _azDevOps.GetAuthorityAsync(orgUri);
            Context.Trace.WriteLine($"Authority is '{authAuthority}'.");

            // Get an AAD access token for the Azure DevOps SPS
            Context.Trace.WriteLine("Getting Azure AD access token...");
            JsonWebToken accessToken = await _msAuth.GetAccessTokenAsync(
                authAuthority,
                AzureDevOpsConstants.AadClientId,
                AzureDevOpsConstants.AadRedirectUri,
                AzureDevOpsConstants.AadResourceId,
                remoteUri,
                null);
            string atUser = accessToken.GetAzureUserName();
            Context.Trace.WriteLineSecrets($"Acquired Azure access token. User='{atUser}' Token='{{0}}'", new object[] {accessToken.EncodedToken});

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

        protected override void ReleaseManagedResources()
        {
            _azDevOps.Dispose();
            base.ReleaseManagedResources();
        }

        #endregion

        #region IConfigurationComponent

        string IConfigurableComponent.Name => "Azure Repos provider";

        public Task ConfigureAsync(
            IEnvironment environment, EnvironmentVariableTarget environmentTarget,
            IGit git, GitConfigurationLevel configurationLevel)
        {
            string useHttpPathKey = $"{KnownGitCfg.Credential.SectionName}.https://dev.azure.com.{KnownGitCfg.Credential.UseHttpPath}";

            IGitConfiguration targetConfig = git.GetConfiguration(configurationLevel);

            if (targetConfig.TryGetValue(useHttpPathKey, out string currentValue) && currentValue.IsTruthy())
            {
                Context.Trace.WriteLine("Git configuration 'credential.useHttpPath' is already set to 'true' for https://dev.azure.com.");
            }
            else
            {
                Context.Trace.WriteLine("Setting Git configuration 'credential.useHttpPath' to 'true' for https://dev.azure.com...");
                targetConfig.SetValue(useHttpPathKey, "true");
            }

            return Task.CompletedTask;
        }

        public Task UnconfigureAsync(
            IEnvironment environment, EnvironmentVariableTarget environmentTarget,
            IGit git, GitConfigurationLevel configurationLevel)
        {
            string useHttpPathKey = $"{KnownGitCfg.Credential.SectionName}.https://dev.azure.com.{KnownGitCfg.Credential.UseHttpPath}";

            Context.Trace.WriteLine("Clearing Git configuration 'credential.useHttpPath' for https://dev.azure.com...");

            IGitConfiguration targetConfig = git.GetConfiguration(configurationLevel);
            targetConfig.Unset(useHttpPathKey);

            return Task.CompletedTask;
        }

        #endregion
    }
}
