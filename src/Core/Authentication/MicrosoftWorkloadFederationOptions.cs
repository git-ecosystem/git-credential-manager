using System;

namespace GitCredentialManager.Authentication;

public enum MicrosoftWorkloadFederationScenario
{
    /// <summary>
    /// Federate via pre-computed client assertion.
    /// </summary>
    Generic,

    /// <summary>
    /// Federate via an access token for an Entra ID Managed Identity.
    /// </summary>
    ManagedIdentity,

    /// <summary>
    /// Federate via a GitHub Actions OIDC token.
    /// </summary>
    GitHubActions,
}

public class MicrosoftWorkloadFederationOptions
{
    public const string DefaultAudience = Constants.DefaultWorkloadFederationAudience;

    private string _audience = DefaultAudience;

    /// <summary>
    /// The workload federation scenario to use.
    /// </summary>
    public MicrosoftWorkloadFederationScenario Scenario { get; set; }

    /// <summary>
    /// Tenant ID of the identity to request an access token for.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// Client ID of the identity to request an access token for.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// The audience to use when requesting a token.
    /// </summary>
    /// <remarks>If this is null, the default audience <see cref="DefaultAudience"/> will be used.</remarks>
    public string Audience
    {
        get => _audience;
        set => _audience = value ?? DefaultAudience;
    }

    /// <summary>
    /// Generic assertion.
    /// </summary>
    /// <remarks>Used with the <see cref="MicrosoftWorkloadFederationScenario.Generic"/> federation scenario.</remarks>
    public string GenericClientAssertion { get; set; }

    /// <summary>
    /// The managed identity to request a federated token for, to exchange for an access token.
    /// </summary>
    /// <remarks>Used with the <see cref="MicrosoftWorkloadFederationScenario.ManagedIdentity"/> federation scenario.</remarks>
    public string ManagedIdentityId { get; set; }

    /// <summary>
    /// GitHub Actions OIDC token request URI.
    /// </summary>
    /// <remarks>Used with the <see cref="MicrosoftWorkloadFederationScenario.GitHubActions"/> federation scenario.</remarks>
    public Uri GitHubTokenRequestUrl { get; set; }

    /// <summary>
    /// GitHub Actions OIDC token request token.
    /// </summary>
    /// <remarks>Used with the <see cref="MicrosoftWorkloadFederationScenario.GitHubActions"/> federation scenario.</remarks>
    public string GitHubTokenRequestToken { get; set; }
}
