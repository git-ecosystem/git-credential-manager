using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager.Authentication.Entra;

public interface IEntraAuthentication
{
    /// <summary>
    /// Get the list of user accounts that have previously signed in to the application.
    /// </summary>
    Task<IReadOnlyList<IEntraAccount>> GetUserAccountsAsync(CancellationToken ct = default);

    /// <summary>
    /// Remove the given user account from the configured user token cache.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the account existed and was successfully removed, <see langword="false"/> otherwise.
    /// </returns>
    Task<bool> RemoveUserAccountAsync(IEntraAccount account);

    /// <summary>
    /// Acquire an access token for a user principal.
    /// </summary>
    /// <param name="authority">Azure authority.</param>
    /// <param name="clientId">Client ID.</param>
    /// <param name="redirectUri">Redirect URI for the client.</param>
    /// <param name="scopes">Set of scopes to request.</param>
    /// <param name="userName">Optional user name for an existing account.</param>
    /// <param name="msaPt">Use MSA-Passthrough behavior when authenticating.</param>
    /// <returns>Authentication result.</returns>
    Task<IEntraAuthenticationResult> GetTokenForUserAsync(string authority, string clientId, Uri redirectUri,
        string[] scopes, string userName, bool msaPt = false);

    /// <summary>
    /// Acquire an access token for the given service principal with the specified scopes.
    /// </summary>
    /// <param name="sp">Service principal identity.</param>
    /// <param name="scopes">Scopes to request.</param>
    /// <returns>Authentication result.</returns>
    Task<IEntraAuthenticationResult> GetTokenForServicePrincipalAsync(ServicePrincipalIdentity sp, string[] scopes);

    /// <summary>
    /// Acquire a token using the managed identity in the current environment.
    /// </summary>
    /// <param name="managedIdentity">Managed identity to use.</param>
    /// <param name="resource">Resource to obtain an access token for.</param>
    /// <returns>Authentication result including access token.</returns>
    /// <remarks>
    /// There are several formats for the <paramref name="managedIdentity"/> parameter:
    /// <para/>
    ///  - <c>"system"</c> - Use the system-assigned managed identity.
    /// <para/>
    ///  - <c>"{guid}"</c> - Use the user-assigned managed identity with client ID <c>{guid}</c>.
    /// <para/>
    ///  - <c>"id://{guid}"</c> - Use the user-assigned managed identity with client ID <c>{guid}</c>.
    /// <para/>
    ///  - <c>"resource://{guid}"</c> - Use the user-assigned managed identity with resource ID <c>{guid}</c>.
    /// </remarks>
    Task<IEntraAuthenticationResult> GetTokenForManagedIdentityAsync(string managedIdentity, string resource);

    /// <summary>
    /// Acquire a token using workload federation.
    /// </summary>
    /// <param name="fedOpts">An object containing configuration workload federation.</param>
    /// <param name="scopes">Scopes to request.</param>
    /// <returns>Authentication result including access token.</returns>
    Task<IEntraAuthenticationResult> GetTokenUsingWorkloadFederationAsync(WorkloadFederationOptions fedOpts, string[] scopes);
}

public interface IEntraAuthenticationResult
{
    string AccessToken { get; }
    IEntraAccount Account { get; }
}
