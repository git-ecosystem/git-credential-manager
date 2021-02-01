// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Commands;
using KnownGitCfg = Microsoft.Git.CredentialManager.Constants.GitConfiguration;

namespace Microsoft.AzureRepos
{
    public class AzureReposHostProvider : DisposableObject, IHostProvider, IConfigurableComponent
    {
        private readonly ICommandContext _context;
        private readonly IAzureDevOpsRestApi _azDevOps;
        private readonly IMicrosoftAuthentication _msAuth;
        private readonly IAzureDevOpsAuthorityCache _authorityCache;

        public AzureReposHostProvider(ICommandContext context)
            : this(context, new AzureDevOpsRestApi(context), new MicrosoftAuthentication(context),
                new AzureDevOpsAuthorityCache(context))
        {
        }

        public AzureReposHostProvider(ICommandContext context, IAzureDevOpsRestApi azDevOps,
            IMicrosoftAuthentication msAuth, IAzureDevOpsAuthorityCache authorityCache)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(azDevOps, nameof(azDevOps));
            EnsureArgument.NotNull(msAuth, nameof(msAuth));
            EnsureArgument.NotNull(authorityCache, nameof(authorityCache));

            _context = context;
            _azDevOps = azDevOps;
            _msAuth = msAuth;
            _authorityCache = authorityCache;
        }

        #region IHostProvider

        public string Id => "azure-repos";

        public string Name => "Azure Repos";

        public IEnumerable<string> SupportedAuthorityIds => MicrosoftAuthentication.AuthorityIds;

        public bool IsSupported(InputArguments input)
        {
            if (input is null)
            {
                return false;
            }

            // We do not support unencrypted HTTP communications to Azure Repos,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            return input.TryGetHostAndPort(out string hostName, out _)
                   && (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                       StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https")) &&
                   UriHelpers.IsAzureDevOpsHost(hostName);
        }

        public bool IsSupported(HttpResponseMessage response)
        {
            // Azure DevOps Server (TFS) is handled by the generic provider, which supports basic auth, and WIA detection.
            return false;
        }

        public async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for Azure Repos. Ensure the repository remote URL is using HTTPS.");
            }

            Uri remoteUri = input.GetRemoteUri();

            if (IsPersonalAccessTokenMode())
            {
                string service = GetServiceName(input);
                string account = GetAccountNameForCredentialQuery(input);

                _context.Trace.WriteLine($"Looking for existing PAT in store with service={service} account={account}...");

                ICredential credential = _context.CredentialStore.Get(service, account);
                if (credential == null)
                {
                    _context.Trace.WriteLine("No existing PAT found.");

                    // No existing credential was found, create a new one
                    _context.Trace.WriteLine("Creating new PAT...");
                    credential = await GeneratePersonalAccessTokenAsync(remoteUri);
                    _context.Trace.WriteLine("PAT created.");
                }
                else
                {
                    _context.Trace.WriteLine("Existing PAT found.");
                }

                return credential;
            }

            var azureResult = await GetAzureAccessTokenAsync(remoteUri);
            return new GitCredential(azureResult.AccountUpn, azureResult.AccessToken);
        }

        public Task StoreCredentialAsync(InputArguments input)
        {
            if (IsPersonalAccessTokenMode())
            {
                string service = GetServiceName(input);

                // We always store credentials against the given username argument for
                // both vs.com and dev.azure.com-style URLs.
                string account = input.UserName;

                // Add or update the credential in the store.
                _context.Trace.WriteLine($"Storing credential with service={service} account={account}...");
                _context.CredentialStore.AddOrUpdate(service, account, input.Password);
                _context.Trace.WriteLine("Credential was successfully stored.");
            }
            else
            {
                _context.Trace.WriteLine("Not using PATs - nothing to store");
            }

            return Task.CompletedTask;
        }

        public Task EraseCredentialAsync(InputArguments input)
        {
            if (IsPersonalAccessTokenMode())
            {
                string service = GetServiceName(input);
                string account = GetAccountNameForCredentialQuery(input);

                // Try to locate an existing credential
                _context.Trace.WriteLine($"Erasing stored credential in store with service={service} account={account}...");
                if (_context.CredentialStore.Remove(service, account))
                {
                    _context.Trace.WriteLine("Credential was successfully erased.");
                }
                else
                {
                    _context.Trace.WriteLine("No credential was erased.");
                }
            }
            else
            {
                _context.Trace.WriteLine("Not using PATs - nothing to erase");
            }

            // Clear the authority cache in case this was the reason for failure
            Uri remoteUri = input.GetRemoteUri();
            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            _authorityCache.EraseAuthority(orgName);

            return Task.CompletedTask;
        }

        protected override void ReleaseManagedResources()
        {
            _azDevOps.Dispose();
            base.ReleaseManagedResources();
        }

        private async Task<ICredential> GeneratePersonalAccessTokenAsync(Uri remoteUri)
        {
            ThrowIfDisposed();

            IMicrosoftAuthenticationResult azureResult = await GetAzureAccessTokenAsync(remoteUri);

            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri, out _);

            // Ask the Azure DevOps instance to create a new PAT
            var patScopes = new[]
            {
                AzureDevOpsConstants.PersonalAccessTokenScopes.ReposWrite,
                AzureDevOpsConstants.PersonalAccessTokenScopes.ArtifactsRead
            };
            _context.Trace.WriteLine($"Creating Azure DevOps PAT with scopes '{string.Join(", ", patScopes)}'...");
            string pat = await _azDevOps.CreatePersonalAccessTokenAsync(
                orgUri,
                azureResult.AccessToken,
                patScopes);
            _context.Trace.WriteLineSecrets("PAT created. PAT='{0}'", new object[] {pat});

            return new GitCredential(azureResult.AccountUpn, pat);
        }

        private async Task<IMicrosoftAuthenticationResult> GetAzureAccessTokenAsync(Uri remoteUri)
        {
            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string orgName);

            _context.Trace.WriteLine($"Determining Microsoft Authentication authority for Azure DevOps organization '{orgName}'...");
            string authAuthority = _authorityCache.GetAuthority(orgName);
            if (authAuthority is null)
            {
                // If there is no cached value we must query for it and cache it for future use
                _context.Trace.WriteLine($"No cached authority value - querying {orgUri} for authority...");
                authAuthority = await _azDevOps.GetAuthorityAsync(orgUri);
                _authorityCache.UpdateAuthority(orgName, authAuthority);
            }
            _context.Trace.WriteLine($"Authority is '{authAuthority}'.");

            _context.Trace.WriteLine("Getting Azure AD access token...");
            IMicrosoftAuthenticationResult result = await _msAuth.GetTokenAsync(
                authAuthority,
                GetClientId(),
                GetRedirectUri(),
                AzureDevOpsConstants.AzureDevOpsDefaultScopes,
                null);
            _context.Trace.WriteLineSecrets(
                $"Acquired Azure access token. Account='{result.AccountUpn}' Token='{{0}}'", new object[] {result.AccessToken});

            return result;
        }

        private string GetClientId()
        {
            // Check for developer override value
            if (_context.Settings.TryGetSetting(
                    AzureDevOpsConstants.EnvironmentVariables.DevAadClientId,
                    Constants.GitConfiguration.Credential.SectionName,
                    AzureDevOpsConstants.GitConfiguration.Credential.DevAadClientId,
                    out string clientId))
            {
                return clientId;
            }

            return AzureDevOpsConstants.AadClientId;
        }

        private Uri GetRedirectUri()
        {
            // Check for developer override value
            if (_context.Settings.TryGetSetting(
                    AzureDevOpsConstants.EnvironmentVariables.DevAadRedirectUri,
                    Constants.GitConfiguration.Credential.SectionName, AzureDevOpsConstants.GitConfiguration.Credential.DevAadRedirectUri,
                    out string redirectUriStr) &&
                Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return AzureDevOpsConstants.AadRedirectUri;
        }

        /// <remarks>
        /// For dev.azure.com-style URLs we use the path arg to get the Azure DevOps organization name.
        /// We ensure the presence of the path arg by setting credential.useHttpPath = true at install time.
        ///
        /// The result of this workaround is that we are now unable to determine if the user wanted to store
        /// credentials with the full path or not for dev.azure.com-style URLs.
        ///
        /// Rather than always assume we're storing credentials against the full path, and therefore resulting
        /// in an personal access token being created per remote URL/repository, we never store against
        /// the full path and always store with the organization URL "dev.azure.com/org".
        ///
        /// For visualstudio.com-style URLs we know the AzDevOps organization name from the host arg, and
        /// don't set the useHttpPath option. This means if we get the full path for a vs.com-style URL
        /// we can store against the full remote path (the intended design).
        ///
        /// Users that need to clone a repository from Azure Repos against the full path therefore must
        /// use the vs.com-style remote URL and not the dev.azure.com one.
        /// </remarks>
        private static string GetServiceName(InputArguments input)
        {
            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                throw new InvalidOperationException("Failed to parse host name and/or port");
            }

            Uri remoteUri = input.GetRemoteUri();

            // dev.azure.com
            if (UriHelpers.IsDevAzureComHost(hostName))
            {
                // We can never store the new dev.azure.com-style URLs against the full path because
                // we have forced the useHttpPath option to true to in order to retrieve the AzDevOps
                // organization name from Git.
                return UriHelpers.CreateOrganizationUri(remoteUri, out _).AbsoluteUri.TrimEnd('/');
            }

            // *.visualstudio.com
            if (UriHelpers.IsVisualStudioComHost(hostName))
            {
                // If we're given the full path for an older *.visualstudio.com-style URL then we should
                // respect that in the service name.
                return remoteUri.AbsoluteUri.TrimEnd('/');
            }

            throw new InvalidOperationException("Host is not Azure DevOps.");
        }

        private static string GetAccountNameForCredentialQuery(InputArguments input)
        {
            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                throw new InvalidOperationException("Failed to parse host name and/or port");
            }

            // dev.azure.com
            if (UriHelpers.IsDevAzureComHost(hostName))
            {
                // We ignore the given username for dev.azure.com-style URLs because AzDevOps recommends
                // adding the organization name as the user in the remote URL (resulting in URLs like
                // https://org@dev.azure.com/org/foo/_git/bar) and we don't know if the given username
                // is an actual username, or the org name.
                // Use `null` as the account name so we match all possible credentials (regardless of
                // the account).
                return null;
            }

            // *.visualstudio.com
            if (UriHelpers.IsVisualStudioComHost(hostName))
            {
                // If we're given a username for the vs.com-style URLs we can and should respect any
                // specified username in the remote URL/input arguments.
                return input.UserName;
            }

            throw new InvalidOperationException("Host is not Azure DevOps.");
        }

        /// <summary>
        /// Check if Azure DevOps Personal Access Tokens should be used or not.
        /// </summary>
        /// <returns>True if Personal Access Tokens should be used, false otherwise.</returns>
        private bool IsPersonalAccessTokenMode()
        {
            // Keep PAT mode on by default whilst AT mode is being tested
            const bool defaultValue = true;

            if (_context.Settings.TryGetSetting(
                AzureDevOpsConstants.EnvironmentVariables.PatMode,
                KnownGitCfg.Credential.SectionName,
                AzureDevOpsConstants.GitConfiguration.Credential.PatMode,
                out string valueStr))
            {
                return valueStr.ToBooleanyOrDefault(defaultValue);
            }

            return defaultValue;
        }

        #endregion

        #region IConfigurationComponent

        string IConfigurableComponent.Name => "Azure Repos provider";

        public Task ConfigureAsync(ConfigurationTarget target)
        {
            string useHttpPathKey = $"{KnownGitCfg.Credential.SectionName}.https://dev.azure.com.{KnownGitCfg.Credential.UseHttpPath}";

            GitConfigurationLevel configurationLevel = target == ConfigurationTarget.System
                ? GitConfigurationLevel.System
                : GitConfigurationLevel.Global;

            IGitConfiguration targetConfig = _context.Git.GetConfiguration(configurationLevel);

            if (targetConfig.TryGet(useHttpPathKey, out string currentValue) && currentValue.IsTruthy())
            {
                _context.Trace.WriteLine("Git configuration 'credential.useHttpPath' is already set to 'true' for https://dev.azure.com.");
            }
            else
            {
                _context.Trace.WriteLine("Setting Git configuration 'credential.useHttpPath' to 'true' for https://dev.azure.com...");
                targetConfig.Set(useHttpPathKey, "true");
            }

            return Task.CompletedTask;
        }

        public Task UnconfigureAsync(ConfigurationTarget target)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
            string useHttpPathKey = $"{KnownGitCfg.Credential.SectionName}.https://dev.azure.com.{KnownGitCfg.Credential.UseHttpPath}";

            _context.Trace.WriteLine("Clearing Git configuration 'credential.useHttpPath' for https://dev.azure.com...");

            GitConfigurationLevel configurationLevel = target == ConfigurationTarget.System
                ? GitConfigurationLevel.System
                : GitConfigurationLevel.Global;

            IGitConfiguration targetConfig = _context.Git.GetConfiguration(configurationLevel);

            // On Windows, if there is a "manager-core" entry remaining in the system config then we must not clear
            // the useHttpPath option otherwise this would break the bundled version of GCM Core in Git for Windows.
            if (!PlatformUtils.IsWindows() || target != ConfigurationTarget.System ||
                targetConfig.GetAll(helperKey).All(x => !string.Equals(x, "manager-core")))
            {
                targetConfig.Unset(useHttpPathKey);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
