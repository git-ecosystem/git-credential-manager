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

        public AzureReposHostProvider(ICommandContext context)
            : this(context, new AzureDevOpsRestApi(context), new MicrosoftAuthentication(context))
        {
        }

        public AzureReposHostProvider(ICommandContext context, IAzureDevOpsRestApi azDevOps,
            IMicrosoftAuthentication msAuth)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(azDevOps, nameof(azDevOps));
            EnsureArgument.NotNull(msAuth, nameof(msAuth));

            _context = context;
            _azDevOps = azDevOps;
            _msAuth = msAuth;
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
            Uri remoteUri = input.GetRemoteUri();

            if (UsePersonalAccessTokens())
            {
                string service = GetServiceName(remoteUri);
                string account = GetAccountNameForCredentialQuery(input);

                _context.Trace.WriteLine($"Looking for existing credential in store with service={service} account={account}...");

                ICredential credential = _context.CredentialStore.Get(service, account);
                if (credential == null)
                {
                    _context.Trace.WriteLine("No existing credentials found.");

                    // No existing credential was found, create a new one
                    _context.Trace.WriteLine("Creating new credential...");
                    credential = await GeneratePersonalAccessTokenAsync(input);
                    _context.Trace.WriteLine("Credential created.");
                }
                else
                {
                    _context.Trace.WriteLine("Existing credential found.");
                }

                return credential;
            }
            else
            {
                // Include the username request here so that we may use it as an override
                // for user account lookups when getting Azure Access Tokens.
                var azureResult = await GetAzureAccessTokenAsync(remoteUri, input.UserName);
                return new GitCredential(azureResult.AccountUpn, azureResult.AccessToken);
            }
        }

        public Task StoreCredentialAsync(InputArguments input)
        {
            Uri remoteUri = input.GetRemoteUri();

            if (UsePersonalAccessTokens())
            {
                string service = GetServiceName(remoteUri);

                // We always store credentials against the given username argument for
                // both vs.com and dev.azure.com-style URLs.
                string account = input.UserName;

                // Add or update the credential in the store.
                _context.Trace.WriteLine($"Storing credential with service={service} account={account}...");
                _context.CredentialStore.AddOrUpdate(service, account, input.Password);
                _context.Trace.WriteLine("Credential was successfully stored.");
            }

            return Task.CompletedTask;
        }

        public Task EraseCredentialAsync(InputArguments input)
        {
            Uri remoteUri = input.GetRemoteUri();

            if (UsePersonalAccessTokens())
            {
                string service = GetServiceName(remoteUri);
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

            return Task.CompletedTask;
        }

        protected override void ReleaseManagedResources()
        {
            _azDevOps.Dispose();
            base.ReleaseManagedResources();
        }

        private async Task<ICredential> GeneratePersonalAccessTokenAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for Azure Repos. Ensure the repository remote URL is using HTTPS.");
            }

            Uri remoteUri = input.GetRemoteUri();
            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri, out _);

            // Determine the MS authentication authority for this organization
            _context.Trace.WriteLine("Determining Microsoft Authentication Authority...");
            string authAuthority = await _azDevOps.GetAuthorityAsync(orgUri);
            _context.Trace.WriteLine($"Authority is '{authAuthority}'.");

            // Get an AAD access token for the Azure DevOps SPS
            _context.Trace.WriteLine("Getting Azure AD access token...");
            IMicrosoftAuthenticationResult result = await _msAuth.GetTokenAsync(
                authAuthority,
                GetClientId(),
                GetRedirectUri(),
                AzureDevOpsConstants.AzureDevOpsDefaultScopes,
                null);
            _context.Trace.WriteLineSecrets(
                $"Acquired Azure access token. Account='{result.AccountUpn}' Token='{{0}}'", new object[] {result.AccessToken});

            // Ask the Azure DevOps instance to create a new PAT
            var patScopes = new[]
            {
                AzureDevOpsConstants.PersonalAccessTokenScopes.ReposWrite,
                AzureDevOpsConstants.PersonalAccessTokenScopes.ArtifactsRead
            };
            _context.Trace.WriteLine($"Creating Azure DevOps PAT with scopes '{string.Join(", ", patScopes)}'...");
            string pat = await _azDevOps.CreatePersonalAccessTokenAsync(
                orgUri,
                result.AccessToken,
                patScopes);
            _context.Trace.WriteLineSecrets("PAT created. PAT='{0}'", new object[] {pat});

            return new GitCredential(result.AccountUpn, pat);
        }

        private async Task<IMicrosoftAuthenticationResult> GetAzureAccessTokenAsync(Uri remoteUri, string userName)
        {
            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(remoteUri.Scheme, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for Azure Repos. Ensure the repository remote URL is using HTTPS.");
            }

            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string orgName);

            _context.Trace.WriteLine($"Determining Microsoft Authentication authority for Azure DevOps organization '{orgName}'...");
            string authAuthority = await _azDevOps.GetAuthorityAsync(orgUri);
            _context.Trace.WriteLine($"Authority is '{authAuthority}'.");

            //
            // If the remote URI is a classic "*.visualstudio.com" host name and we have a user specified from the
            // remote then take that as the current AAD/MSA user in the first instance.
            //
            // For "dev.azure.com" host names we only use the user info part of the remote when this doesn't
            // match the Azure DevOps organization name. Our friends in Azure DevOps decided "borrow" the username
            // part of the remote URL to include the organization name (not an actual username).
            //
            if (UriHelpers.IsAzureDevOpsHost(remoteUri.Host) && StringComparer.OrdinalIgnoreCase.Equals(orgName, userName))
            {
                _context.Trace.WriteLine("Cannot use username from dev.azure.com remote URL because this is the Azure DevOps organization name.");
                userName = null;
            }
            else if (!string.IsNullOrWhiteSpace(userName))
            {
                _context.Trace.WriteLine("Using username as specified in remote.");
            }

            _context.Trace.WriteLine(string.IsNullOrWhiteSpace(userName) ? "No user found." : $"User is '{userName}'.");

            // Get an AAD access token for the Azure DevOps SPS
            _context.Trace.WriteLine("Getting Azure AD access token...");
            IMicrosoftAuthenticationResult result = await _msAuth.GetTokenAsync(
                authAuthority,
                GetClientId(),
                GetRedirectUri(),
                AzureDevOpsConstants.AzureDevOpsDefaultScopes,
                userName);
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
        private static string GetServiceName(Uri remoteUri)
        {
            // dev.azure.com
            if (UriHelpers.IsDevAzureComHost(remoteUri.Host))
            {
                // We can never store the new dev.azure.com-style URLs against the full path because
                // we have forced the useHttpPath option to true to in order to retrieve the AzDevOps
                // organization name from Git.
                return UriHelpers.CreateOrganizationUri(remoteUri, out _).AbsoluteUri.TrimEnd('/');
            }

            // *.visualstudio.com
            if (UriHelpers.IsVisualStudioComHost(remoteUri.Host))
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
        private bool UsePersonalAccessTokens()
        {
            // Default to using PATs whilst the Azure AT functionality is being tested
            const bool defaultValue = true;

            if (_context.Settings.TryGetSetting(
                AzureDevOpsConstants.EnvironmentVariables.CredentialType,
                KnownGitCfg.Credential.SectionName,
                AzureDevOpsConstants.GitConfiguration.Credential.CredentialType,
                out string valueStr))
            {
                _context.Trace.WriteLine($"Azure Repos credential type override set to '{valueStr}'");

                switch (valueStr.ToLowerInvariant())
                {
                    case AzureDevOpsConstants.PatCredentialType:
                        return true;

                    case AzureDevOpsConstants.OAuthCredentialType:
                        return false;

                    default:
                        _context.Streams.Error.WriteLine(
                            $"warning: unknown Azure Repos credential type '{valueStr}' - using default option");
                        return defaultValue;
                }
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

            IGitConfiguration targetConfig = _context.Git.GetConfiguration();

            if (targetConfig.TryGet(useHttpPathKey, out string currentValue) && currentValue.IsTruthy())
            {
                _context.Trace.WriteLine("Git configuration 'credential.useHttpPath' is already set to 'true' for https://dev.azure.com.");
            }
            else
            {
                _context.Trace.WriteLine("Setting Git configuration 'credential.useHttpPath' to 'true' for https://dev.azure.com...");
                targetConfig.Set(configurationLevel, useHttpPathKey, "true");
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

            IGitConfiguration targetConfig = _context.Git.GetConfiguration();

            // On Windows, if there is a "manager-core" entry remaining in the system config then we must not clear
            // the useHttpPath option otherwise this would break the bundled version of GCM Core in Git for Windows.
            if (!PlatformUtils.IsWindows() || target != ConfigurationTarget.System ||
                targetConfig.GetAll(helperKey).All(x => !string.Equals(x, "manager-core")))
            {
                targetConfig.Unset(configurationLevel, useHttpPathKey);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
