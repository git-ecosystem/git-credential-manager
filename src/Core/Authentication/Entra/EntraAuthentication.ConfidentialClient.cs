using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace GitCredentialManager.Authentication.Entra;

public partial class EntraAuthentication
{
    public async Task<IEntraAuthenticationResult> GetTokenForServicePrincipalAsync(
        string[] scopes, ServicePrincipalIdentity sp, CancellationToken ct = default)
    {
        Context.Trace.WriteLine($"Creating confidential client for service principal '{sp.Id}' in tenant '{sp.TenantId}'...");
        var builder = ConfidentialClientApplicationBuilder.Create(sp.Id)
            .WithTenantId(sp.TenantId)
            .WithHttpClientFactory(_httpFactory)
            .WithTraceLogging(Context);

        if (sp.Certificate is not null)
        {
            Context.Trace.WriteLine($"Using service principal certificate: {sp.Certificate.Thumbprint}");
            builder.WithCertificate(sp.Certificate);
        }
        else if (!string.IsNullOrWhiteSpace(sp.ClientSecret))
        {
            Context.Trace.WriteLineSecrets("Using service principal secret: {0}", [sp.ClientSecret]);
            builder.WithClientSecret(sp.ClientSecret);
        }
        else
        {
            throw new ArgumentException($"Service principal '{sp.Id}' must have either a certificate or client secret.", nameof(sp));
        }

        Context.Trace.WriteLine($"SendX5C is '{sp.SendX5C}'");

        IConfidentialClientApplication app = builder.Build();
        await RegisterCacheAsync(app);

        Context.Trace.WriteLine($"Acquiring token for service principal with scopes '{string.Join(", ", scopes)}'...");
        AuthenticationResult result = await app.AcquireTokenForClient(scopes)
            .WithSendX5C(sp.SendX5C)
            .ExecuteAsync(ct);

        return AuthResult.FromMsalResult(result);
    }

    public async Task<IEntraAuthenticationResult> GetTokenForManagedIdentityAsync(
        string resource, ManagedIdentity mi, CancellationToken ct = default)
    {
        Context.Trace.WriteLine($"Creating confidential client for managed identity '{mi.Id}'...");
        var builder = ManagedIdentityApplicationBuilder.Create(mi)
            .WithHttpClientFactory(_httpFactory)
            .WithTraceLogging(Context);

        IManagedIdentityApplication app = builder.Build();

        Context.Trace.WriteLine($"Acquiring token for managed identity with resource '{resource}'...");
        AuthenticationResult result = await app.AcquireTokenForManagedIdentity(resource)
            .ExecuteAsync(ct);

        return AuthResult.FromMsalResult(result);
    }

    public async Task<IEntraAuthenticationResult> GetTokenUsingWorkloadFederationAsync(
        string[] scopes, WorkloadFederationOptions fedOpts, CancellationToken ct = default)
    {
        Context.Trace.WriteLine(
            $"Creating confidential client for federation with client ID '{fedOpts.ClientId}' and tenant ID '{fedOpts.TenantId}'...");
        Context.Trace.WriteLine($"Federation scenario: {fedOpts.Scenario}");
        var builder = ConfidentialClientApplicationBuilder.Create(fedOpts.ClientId)
            .WithTenantId(fedOpts.TenantId)
            .WithHttpClientFactory(_httpFactory)
            .WithTraceLogging(Context)
            .WithClientAssertion(reqOpts => GetClientAssertion(fedOpts, reqOpts));

        IConfidentialClientApplication app = builder.Build();
        await RegisterCacheAsync(app);

        AuthenticationResult result = await app.AcquireTokenForClient(scopes)
            .ExecuteAsync(ct);

        return AuthResult.FromMsalResult(result);
    }

    private async Task<string> GetClientAssertion(WorkloadFederationOptions fedOpts, AssertionRequestOptions _)
    {
        switch (fedOpts.Scenario)
        {
            case WorkloadFederationScenario.Generic:
                Context.Trace.WriteLine("Getting client assertion for generic workload federation scenario...");
                if (string.IsNullOrWhiteSpace(fedOpts.GenericClientAssertion))
                    throw new InvalidOperationException(
                        "Client assertion must be provided for generic workload federation scenario.");
                return fedOpts.GenericClientAssertion;

            case WorkloadFederationScenario.ManagedIdentity:
                Context.Trace.WriteLine(
                    "Getting client assertion for managed identity workload federation scenario...");
                var mi = ManagedIdentity.Create(fedOpts.ManagedIdentityId);
                var miResult = await GetTokenForManagedIdentityAsync(fedOpts.Audience, mi);
                return miResult.AccessToken;

            case WorkloadFederationScenario.GitHubActions:
                Context.Trace.WriteLine("Getting client assertion for GitHub Actions workload federation scenario...");
                return await GetGitHubOidcToken(fedOpts.GitHubTokenRequestUrl, fedOpts.Audience,
                    fedOpts.GitHubTokenRequestToken);

            default:
                throw new ArgumentOutOfRangeException(nameof(fedOpts.Scenario), fedOpts.Scenario,
                    "Unsupported workload federation scenario.");
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
            Context.Trace.WriteLine(
                $"Failed to acquire GitHub OIDC token [{response.StatusCode:D} {response.StatusCode}]: {error}");
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
}
