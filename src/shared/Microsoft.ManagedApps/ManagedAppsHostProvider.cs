using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using KnownGitCfg = GitCredentialManager.Constants.GitConfiguration;

namespace Microsoft.ManagedApps
{
    /// <summary>
    /// Host provider for Git repositories hosted by Microsoft Managed Apps' Power Platform
    /// environment Git service.
    /// </summary>
    public class ManagedAppsHostProvider : HostProvider
    {
        private readonly IMicrosoftAuthentication _msAuth;
        private readonly IManagedAppsBindingManager _bindingManager;

        public ManagedAppsHostProvider(ICommandContext context)
            : this(context, new MicrosoftAuthentication(context), new ManagedAppsBindingManager(context))
        {
        }

        public ManagedAppsHostProvider(ICommandContext context, IMicrosoftAuthentication msAuth,
            IManagedAppsBindingManager bindingManager)
            : base(context)
        {
            EnsureArgument.NotNull(msAuth, nameof(msAuth));
            EnsureArgument.NotNull(bindingManager, nameof(bindingManager));

            _msAuth = msAuth;
            _bindingManager = bindingManager;
        }

        #region IHostProvider

        public override string Id => "microsoft-managed-apps";

        public override string Name => "Microsoft Managed Apps";

        public override IEnumerable<string> SupportedAuthorityIds => MicrosoftAuthentication.AuthorityIds;

        public override bool IsSupported(InputArguments input)
        {
            if (input is null || !input.TryGetHostAndPort(out string hostName, out _))
            {
                return false;
            }

            bool isHttp = StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http");
            bool isHttps = StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https");

            return (isHttp || isHttps) && TryMatchCloudEnvironment(hostName, out _);
        }

        public override string GetServiceName(InputArguments input)
        {
            // Authentication is scoped to the environment (host), never the path - one
            // sign-in per host, regardless of which repository under it is being cloned.
            Uri remote = input.GetRemoteUri(includeUser: false);
            return new Uri($"{remote.Scheme}://{remote.Authority}").AbsoluteUri.TrimEnd('/');
        }

        public override async Task<GetCredentialResult> GetCredentialAsync(InputArguments input)
        {
            // Never consult the OS credential store: every authentication mode either
            // re-derives a fresh credential via MSAL (which maintains its own silent/refresh
            // token cache) or via a non-interactive federated/managed-identity/service-principal
            // flow. There is no PAT-equivalent, long-lived credential for us to cache
            // ourselves - this mirrors AzureReposHostProvider's OAuth-token (non-PAT) branch.
            ICredential credential = await GenerateCredentialAsync(input);
            return new GetCredentialResult(credential);
        }

        public override Task StoreCredentialAsync(InputArguments input)
        {
            if (UseManagedIdentity(out _) || UseWorkloadFederation(out _) || UseServicePrincipal(out _))
            {
                Context.Trace.WriteLine("Nothing to store for non-interactive authentication.");
                return Task.CompletedTask;
            }

            string serviceName = GetServiceName(input);
            Context.Trace.WriteLine($"Recording account binding for '{serviceName}'...");
            _bindingManager.SignIn(serviceName, input.UserName);
            return Task.CompletedTask;
        }

        public override Task EraseCredentialAsync(InputArguments input)
        {
            if (UseManagedIdentity(out _) || UseWorkloadFederation(out _) || UseServicePrincipal(out _))
            {
                Context.Trace.WriteLine("Nothing to erase for non-interactive authentication.");
                return Task.CompletedTask;
            }

            string serviceName = GetServiceName(input);
            Context.Trace.WriteLine($"Removing account binding for '{serviceName}'...");
            _bindingManager.SignOut(serviceName);
            return Task.CompletedTask;
        }

        #endregion

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();
            ThrowIfUnsafeRemote(input);

            if (!input.TryGetHostAndPort(out string hostName, out _) || !TryMatchCloudEnvironment(hostName, out ManagedAppsCloudEnvironment cloudEnvironment))
            {
                throw new Trace2Exception(Context.Trace2,
                    $"'{input.Host}' is not a recognized Microsoft Managed Apps environment host.");
            }

            string[] scopes = cloudEnvironment.Scopes.ToArray();
            Context.Trace.WriteLine($"Matched cloud environment '{cloudEnvironment.Name}' (resource='{cloudEnvironment.ResourceAudience}', scopes=[{string.Join(", ", scopes)}]).");

            if (UseManagedIdentity(out string mid))
            {
                Context.Trace.WriteLine($"Getting Azure access token for managed identity '{mid}' (cloud environment '{cloudEnvironment.Name}')...");
                IMicrosoftAuthenticationResult miResult = await _msAuth.GetTokenForManagedIdentityAsync(mid, cloudEnvironment.ResourceAudience);
                return new GitCredential(mid, miResult.AccessToken);
            }

            if (UseWorkloadFederation(out MicrosoftWorkloadFederationOptions fedOpts))
            {
                Context.Trace.WriteLine($"Getting Azure access token using workload identity federation (scenario: {fedOpts.Scenario}, cloud environment '{cloudEnvironment.Name}')...");
                IMicrosoftAuthenticationResult fedResult = await _msAuth.GetTokenUsingWorkloadFederationAsync(fedOpts, scopes);
                return new GitCredential(fedOpts.ClientId, fedResult.AccessToken);
            }

            if (UseServicePrincipal(out ServicePrincipalIdentity sp))
            {
                Context.Trace.WriteLine($"Getting Azure access token for service principal '{sp.TenantId}/{sp.Id}' (cloud environment '{cloudEnvironment.Name}')...");
                IMicrosoftAuthenticationResult spResult = await _msAuth.GetTokenForServicePrincipalAsync(sp, scopes);
                return new GitCredential(sp.Id, spResult.AccessToken);
            }

            // Interactive/silent user authentication (default path).
            string serviceName = GetServiceName(input);
            string accountHint = input.UserName ?? _bindingManager.GetAccount(serviceName);

            Context.Trace.WriteLine(accountHint is null
                ? $"No existing account binding found for '{serviceName}' - will prompt for account selection."
                : $"Using account hint '{accountHint}' for '{serviceName}' (cloud environment '{cloudEnvironment.Name}').");

            IMicrosoftAuthenticationResult result = await _msAuth.GetTokenForUserAsync(
                GetAuthority(), GetClientId(), GetRedirectUri(), scopes, accountHint, msaPt: false);

            Context.Trace.WriteLineSecrets(
                $"Acquired Azure access token. Account='{result.AccountUpn}' Token='{{0}}'",
                new object[] { result.AccessToken });

            return new GitCredential(result.AccountUpn, result.AccessToken);
        }

        private bool TryMatchCloudEnvironment(string host, out ManagedAppsCloudEnvironment cloudEnvironment)
        {
            return ManagedAppsCloudEnvironment.TryMatch(Context.Trace, Context.Git.GetConfiguration(), host, out cloudEnvironment);
        }

        private void ThrowIfUnsafeRemote(InputArguments input)
        {
            if (!Context.Settings.AllowUnsafeRemotes &&
                StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Trace2Exception(Context.Trace2,
                    "Unencrypted HTTP is not recommended for Microsoft Managed Apps. " +
                    "Ensure the repository remote URL is using HTTPS " +
                    $"or see {Constants.HelpUrls.GcmUnsafeRemotes} about how to allow unsafe remotes.");
            }
        }

        private string GetAuthority()
        {
            string baseUri = ManagedAppsConstants.AadAuthorityBaseUrl;

            if (Context.Settings.TryGetSetting(
                    ManagedAppsConstants.EnvironmentVariables.DevAadAuthorityBaseUri,
                    KnownGitCfg.Credential.SectionName,
                    ManagedAppsConstants.GitConfiguration.Credential.DevAadAuthorityBaseUri,
                    out string devBaseUri) && !string.IsNullOrWhiteSpace(devBaseUri))
            {
                baseUri = devBaseUri.TrimEnd('/') + "/";
            }

            return baseUri + ManagedAppsConstants.AadAuthoritySegment;
        }

        private string GetClientId()
        {
            if (Context.Settings.TryGetSetting(
                    ManagedAppsConstants.EnvironmentVariables.DevAadClientId,
                    KnownGitCfg.Credential.SectionName,
                    ManagedAppsConstants.GitConfiguration.Credential.DevAadClientId,
                    out string clientId) && !string.IsNullOrWhiteSpace(clientId))
            {
                return clientId;
            }

            return ManagedAppsConstants.AadClientId;
        }

        private Uri GetRedirectUri()
        {
            if (Context.Settings.TryGetSetting(
                    ManagedAppsConstants.EnvironmentVariables.DevAadRedirectUri,
                    KnownGitCfg.Credential.SectionName,
                    ManagedAppsConstants.GitConfiguration.Credential.DevAadRedirectUri,
                    out string redirectUriStr) && Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return ManagedAppsConstants.AadRedirectUri;
        }

        private bool UseManagedIdentity(out string mid)
        {
            return Context.Settings.TryGetSetting(
                       ManagedAppsConstants.EnvironmentVariables.ManagedIdentity,
                       KnownGitCfg.Credential.SectionName,
                       ManagedAppsConstants.GitConfiguration.Credential.ManagedIdentity,
                       out mid) &&
                   !string.IsNullOrWhiteSpace(mid);
        }

        private bool UseServicePrincipal(out ServicePrincipalIdentity sp)
        {
            if (!Context.Settings.TryGetSetting(
                    ManagedAppsConstants.EnvironmentVariables.ServicePrincipalId,
                    KnownGitCfg.Credential.SectionName,
                    ManagedAppsConstants.GitConfiguration.Credential.ServicePrincipal,
                    out string spStr) || string.IsNullOrWhiteSpace(spStr))
            {
                sp = null;
                return false;
            }

            string[] split = spStr.Split(new[] { '/' }, count: 2);

            if (split.Length < 1 || string.IsNullOrWhiteSpace(split[0]))
            {
                Context.Streams.Error.WriteLine("error: unable to use configured service principal - missing tenant ID in configuration");
                sp = null;
                return false;
            }

            if (split.Length < 2 || string.IsNullOrWhiteSpace(split[1]))
            {
                Context.Streams.Error.WriteLine("error: unable to use configured service principal - missing client ID in configuration");
                sp = null;
                return false;
            }

            string tenantId = split[0];
            string clientId = split[1];

            sp = new ServicePrincipalIdentity
            {
                Id = clientId,
                TenantId = tenantId,
            };

            bool hasClientSecret = Context.Settings.TryGetSetting(
                ManagedAppsConstants.EnvironmentVariables.ServicePrincipalSecret,
                KnownGitCfg.Credential.SectionName,
                ManagedAppsConstants.GitConfiguration.Credential.ServicePrincipalSecret,
                out string clientSecret);

            bool hasCertThumbprint = Context.Settings.TryGetSetting(
                ManagedAppsConstants.EnvironmentVariables.ServicePrincipalCertificateThumbprint,
                KnownGitCfg.Credential.SectionName,
                ManagedAppsConstants.GitConfiguration.Credential.ServicePrincipalCertificateThumbprint,
                out string certThumbprint);

            if (hasCertThumbprint && hasClientSecret)
            {
                Context.Streams.Error.WriteLine("warning: both service principal client secret and certificate thumbprint are configured - using certificate");
            }

            if (hasCertThumbprint)
            {
                sp.SendX5C = Context.Settings.TryGetSetting(
                    ManagedAppsConstants.EnvironmentVariables.ServicePrincipalCertificateSendX5C,
                    KnownGitCfg.Credential.SectionName,
                    ManagedAppsConstants.GitConfiguration.Credential.ServicePrincipalCertificateSendX5C,
                    out string certHasX5CStr) && certHasX5CStr.ToBooleanyOrDefault(false);

                X509Certificate2 cert = X509Utils.GetCertificateByThumbprint(certThumbprint);
                if (cert is null)
                {
                    Context.Streams.Error.WriteLine($"error: unable to find certificate with thumbprint '{certThumbprint}' for service principal");
                    sp = null;
                    return false;
                }

                sp.Certificate = cert;
            }
            else if (hasClientSecret)
            {
                sp.ClientSecret = clientSecret;
            }

            return true;
        }

        private bool UseWorkloadFederation(out MicrosoftWorkloadFederationOptions fedOpts)
        {
            if (!Context.Settings.TryGetSetting(
                    ManagedAppsConstants.EnvironmentVariables.WorkloadFederation,
                    KnownGitCfg.Credential.SectionName,
                    ManagedAppsConstants.GitConfiguration.Credential.WorkloadFederation,
                    out string wifStr))
            {
                fedOpts = null;
                return false;
            }

            MicrosoftWorkloadFederationScenario scenario;
            switch (wifStr.ToLowerInvariant())
            {
                case "generic":
                    scenario = MicrosoftWorkloadFederationScenario.Generic;
                    break;

                case "mi":
                case "managedidentity":
                    scenario = MicrosoftWorkloadFederationScenario.ManagedIdentity;
                    break;

                case "github":
                case "githubactions":
                    scenario = MicrosoftWorkloadFederationScenario.GitHubActions;
                    break;

                default: // Unknown scenario value
                    fedOpts = null;
                    return false;
            }

            bool hasClientId = Context.Settings.TryGetSetting(
                ManagedAppsConstants.EnvironmentVariables.WorkloadFederationClientId,
                KnownGitCfg.Credential.SectionName,
                ManagedAppsConstants.GitConfiguration.Credential.WorkloadFederationClientId,
                out string clientId);

            bool hasTenantId = Context.Settings.TryGetSetting(
                ManagedAppsConstants.EnvironmentVariables.WorkloadFederationTenantId,
                KnownGitCfg.Credential.SectionName,
                ManagedAppsConstants.GitConfiguration.Credential.WorkloadFederationTenantId,
                out string tenantId);

            if (!hasClientId || !hasTenantId)
            {
                Context.Streams.Error.WriteLine("error: both client ID and tenant ID are required for workload federation");
                fedOpts = null;
                return false;
            }

            // Audience is optional - the default is "api://AzureADTokenExchange"
            if (!Context.Settings.TryGetSetting(
                    ManagedAppsConstants.EnvironmentVariables.WorkloadFederationAudience,
                    KnownGitCfg.Credential.SectionName,
                    ManagedAppsConstants.GitConfiguration.Credential.WorkloadFederationAudience,
                    out string audience) || string.IsNullOrWhiteSpace(audience))
            {
                audience = MicrosoftWorkloadFederationOptions.DefaultAudience;
            }

            fedOpts = new MicrosoftWorkloadFederationOptions
            {
                Scenario = scenario,
                ClientId = clientId,
                TenantId = tenantId,
                Audience = audience
            };

            switch (scenario)
            {
                case MicrosoftWorkloadFederationScenario.Generic:
                    if (!Context.Settings.TryGetSetting(
                            ManagedAppsConstants.EnvironmentVariables.WorkloadFederationAssertion,
                            KnownGitCfg.Credential.SectionName,
                            ManagedAppsConstants.GitConfiguration.Credential.WorkloadFederationAssertion,
                            out string assertion) || string.IsNullOrWhiteSpace(assertion))
                    {
                        Context.Streams.Error.WriteLine("error: assertion is required for the generic workload federation scenario");
                        fedOpts = null;
                        return false;
                    }

                    // Check if this value points to a file containing the actual assertion (file://<path>)
                    if (Uri.TryCreate(assertion, UriKind.Absolute, out Uri assertionUri)
                        && StringComparer.OrdinalIgnoreCase.Equals(assertionUri.Scheme, "file"))
                    {
                        string filePath = assertionUri.LocalPath;
                        if (!Context.FileSystem.FileExists(filePath))
                        {
                            Context.Streams.Error.WriteLine($"error: assertion file not found: {filePath}");
                            fedOpts = null;
                            return false;
                        }

                        Context.Trace.WriteLine($"Reading workload federation assertion from file '{filePath}'...");
                        assertion = Context.FileSystem.ReadAllText(filePath).Trim();
                        if (string.IsNullOrWhiteSpace(assertion))
                        {
                            Context.Streams.Error.WriteLine($"error: assertion file is empty: {filePath}");
                            fedOpts = null;
                            return false;
                        }
                    }

                    fedOpts.GenericClientAssertion = assertion;
                    break;

                case MicrosoftWorkloadFederationScenario.ManagedIdentity:
                    if (!Context.Settings.TryGetSetting(
                            ManagedAppsConstants.EnvironmentVariables.WorkloadFederationManagedIdentity,
                            KnownGitCfg.Credential.SectionName,
                            ManagedAppsConstants.GitConfiguration.Credential.WorkloadFederationManagedIdentity,
                            out string managedIdentity) || string.IsNullOrWhiteSpace(managedIdentity))
                    {
                        Context.Streams.Error.WriteLine("error: managed identity is required for the managed identity workload federation scenario");
                        fedOpts = null;
                        return false;
                    }

                    fedOpts.ManagedIdentityId = managedIdentity;
                    break;

                case MicrosoftWorkloadFederationScenario.GitHubActions:
                    if (!Context.Environment.Variables.TryGetValue(
                            Constants.EnvironmentVariables.GitHubActionsTokenRequestUrl, out string tokenRequestUrl)
                        || !Uri.TryCreate(tokenRequestUrl, UriKind.Absolute, out Uri tokenRequestUri))
                    {
                        Context.Streams.Error.WriteLine(
                            "error: unable to get valid token request URL from environment variable for the GitHub Actions workload federation scenario");
                        fedOpts = null;
                        return false;
                    }

                    if (!Context.Environment.Variables.TryGetValue(
                            Constants.EnvironmentVariables.GitHubActionsTokenRequestToken, out string tokenRequestToken)
                        || string.IsNullOrWhiteSpace(tokenRequestToken))
                    {
                        Context.Streams.Error.WriteLine(
                            "error: unable to get valid token request token from environment variable for the GitHub Actions workload federation scenario");
                        fedOpts = null;
                        return false;
                    }

                    fedOpts.GitHubTokenRequestUrl = tokenRequestUri;
                    fedOpts.GitHubTokenRequestToken = tokenRequestToken;
                    break;
            }

            return true;
        }
    }
}
