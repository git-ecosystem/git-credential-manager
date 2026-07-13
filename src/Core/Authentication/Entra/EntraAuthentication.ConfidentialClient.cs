using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace GitCredentialManager.Authentication.Entra
{
    public partial class EntraAuthentication
    {
        public async Task<IEntraAuthenticationResult> GetTokenForServicePrincipalAsync(ServicePrincipalIdentity sp, string[] scopes)
        {
            IConfidentialClientApplication app = await CreateConfidentialClientApplicationAsync(sp);

            try
            {
                Context.Trace.WriteLine($"Sending with X5C: '{sp.SendX5C}'.");
                AuthenticationResult result = await app.AcquireTokenForClient(scopes).WithSendX5C(sp.SendX5C).ExecuteAsync();;

                return AuthResult.FromMsalResult(result);
            }
            catch (Exception ex)
            {
                Context.Trace.WriteLine($"Failed to acquire token for service principal '{sp.TenantId}/{sp.Id}'.");
                Context.Trace.WriteException(ex);
                throw;
            }
        }

        public async Task<IEntraAuthenticationResult> GetTokenForManagedIdentityAsync(
            ManagedIdentity managedIdentity, string resource)
        {
            var httpFactoryAdaptor = new MsalHttpClientFactoryAdaptor(Context.HttpClientFactory);

            IManagedIdentityApplication app = ManagedIdentityApplicationBuilder.Create(managedIdentity)
                .WithHttpClientFactory(httpFactoryAdaptor)
                .Build();

            try
            {
                AuthenticationResult result = await app.AcquireTokenForManagedIdentity(resource).ExecuteAsync();
                return AuthResult.FromMsalResult(result);
            }
            catch (Exception ex)
            {
                Context.Trace.WriteLine(managedIdentity == ManagedIdentity.System
                    ? "Failed to acquire token for system managed identity."
                    : $"Failed to acquire token for user managed identity '{managedIdentity.Id}'.");
                Context.Trace.WriteException(ex);
                throw;
            }
        }

        public async Task<IEntraAuthenticationResult> GetTokenUsingWorkloadFederationAsync(WorkloadFederationOptions fedOpts, string[] scopes)
        {
            IConfidentialClientApplication app = await CreateConfidentialClientApplicationAsync(fedOpts);

            AuthenticationResult result = await app.AcquireTokenForClient(scopes)
              .ExecuteAsync()
              .ConfigureAwait(false);

            return AuthResult.FromMsalResult(result);
        }

        private async Task<string> GetClientAssertion(WorkloadFederationOptions fedOpts, AssertionRequestOptions _)
        {
            switch (fedOpts.Scenario)
            {
                case MicrosoftWorkloadFederationScenario.Generic:
                    Context.Trace.WriteLine("Getting client assertion for generic workload federation scenario...");
                    if (string.IsNullOrWhiteSpace(fedOpts.GenericClientAssertion))
                        throw new InvalidOperationException(
                            "Client assertion must be provided for generic workload federation scenario.");
                    return fedOpts.GenericClientAssertion;

                case MicrosoftWorkloadFederationScenario.ManagedIdentity:
                    Context.Trace.WriteLine("Getting client assertion for managed identity workload federation scenario...");
                    var mi = ManagedIdentity.Create(fedOpts.ManagedIdentityId);
                    var miResult = await GetTokenForManagedIdentityAsync(mi, fedOpts.Audience);
                    return miResult.AccessToken;

                case MicrosoftWorkloadFederationScenario.GitHubActions:
                    Context.Trace.WriteLine("Getting client assertion for GitHub Actions workload federation scenario...");
                    return await GetGitHubOidcToken(fedOpts.GitHubTokenRequestUrl, fedOpts.Audience, fedOpts.GitHubTokenRequestToken);

                default:
                    throw new ArgumentOutOfRangeException(nameof(fedOpts.Scenario), fedOpts.Scenario, "Unsupported workload federation scenario.");
            }
        }

        private async Task<string> GetGitHubOidcToken(Uri requestUri, string audience, string requestToken)
        {
            using HttpClient http = Context.HttpClientFactory.CreateClient();

            UriBuilder ub = new UriBuilder(requestUri);
            if (ub.Query.Length > 0) ub.Query += "&";
            ub.Query += $"audience={Uri.EscapeDataString(audience)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, ub.Uri);
            request.AddBearerAuthenticationHeader(requestToken);

            Context.Trace.WriteLine($"Requesting GitHub OIDC token from '{request.RequestUri}'...");
            Context.Trace.WriteLineSecrets("OIDC request token: {0}", new[] { requestToken });
            using HttpResponseMessage response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                Context.Trace.WriteLine($"Failed to acquire GitHub OIDC token [{response.StatusCode:D} {response.StatusCode}]: {error}");
                response.EnsureSuccessStatusCode();
            }

            string json = await response.Content.ReadAsStringAsync();

            try
            {
                using JsonDocument jsonDoc = JsonDocument.Parse(json);
                if (!jsonDoc.RootElement.TryGetProperty("value", out JsonElement tokenElement))
                {
                    throw new InvalidOperationException(
                        "Invalid response from GitHub OIDC token endpoint: 'value' property not found.");
                }

                return tokenElement.GetString() ??
                       throw new InvalidOperationException(
                           "Invalid response from GitHub OIDC token endpoint: 'value' property is null.");
            }
            catch (Exception ex)
            {
                Context.Trace.WriteException(ex);
                Context.Trace.WriteLine($"OIDC token response: {json}");
                throw;
            }
        }

        private async Task<IConfidentialClientApplication> CreateConfidentialClientApplicationAsync(ServicePrincipalIdentity sp)
        {
            var httpFactoryAdaptor = new MsalHttpClientFactoryAdaptor(Context.HttpClientFactory);

            Context.Trace.WriteLine($"Creating confidential client application for {sp.TenantId}/{sp.Id}...");
            var appBuilder = ConfidentialClientApplicationBuilder.Create(sp.Id)
                .WithTenantId(sp.TenantId)
                .WithHttpClientFactory(httpFactoryAdaptor);

            if (sp.Certificate is not null)
            {
                Context.Trace.WriteLineSecrets("Using certificate with thumbprint: '{0}'", new object[] { sp.Certificate.Thumbprint });
                appBuilder = appBuilder.WithCertificate(sp.Certificate);
            }
            else if (!string.IsNullOrWhiteSpace(sp.ClientSecret))
            {
                Context.Trace.WriteLineSecrets("Using client secret: '{0}'", new object[] { sp.ClientSecret });
                appBuilder = appBuilder.WithClientSecret(sp.ClientSecret);
            }
            else
            {
                throw new InvalidOperationException("Service principal identity does not contain a certificate or client secret.");
            }

            IConfidentialClientApplication app = appBuilder.Build();

            await RegisterTokenCacheAsync(app.AppTokenCache, CreateAppTokenCacheProps, Context.Trace2);

            return app;
        }

        private async Task<IConfidentialClientApplication> CreateConfidentialClientApplicationAsync(
            WorkloadFederationOptions fedOpts)
        {
            var httpFactoryAdaptor = new MsalHttpClientFactoryAdaptor(Context.HttpClientFactory);

            Context.Trace.WriteLine($"Creating federated confidential client application for {fedOpts.TenantId}/{fedOpts.ClientId}...");
            var appBuilder = ConfidentialClientApplicationBuilder.Create(fedOpts.ClientId)
                .WithTenantId(fedOpts.TenantId)
                .WithHttpClientFactory(httpFactoryAdaptor)
                .WithClientAssertion(reqOpts => GetClientAssertion(fedOpts, reqOpts));

            IConfidentialClientApplication app = appBuilder.Build();

            await RegisterTokenCacheAsync(app.AppTokenCache, CreateAppTokenCacheProps, Context.Trace2);

            return app;
        }
    }
}
