using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager.Authentication.Entra;

public interface IEntraAuthentication
{
    /// <summary>
    /// Ask the user which interaction mode they would like to use for authentication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the user has a stored preference, this value will be returned. Otherwise,
    /// the user will be prompted to choose an interaction mode.
    /// </para>
    /// <para>
    /// If the <see cref="InteractionMode.Auto"/> value is returned this means that the user
    /// has not expressed a preference and must use the most appropriate interaction mode for
    /// the current environment.
    /// </para>
    /// </remarks>
    Task<InteractionMode> GetInteractionModeAsync(CancellationToken ct = default);

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
    Task<IEntraAuthenticationResult> GetTokenForUserAsync(
        string[] scopes,
        string authority = null,
        IEntraAccount account = null,
        InteractionMode interactionMode = InteractionMode.Auto,
        CancellationToken ct = default
    );

    /// <summary>
    /// Acquire an access token for a service principal.
    /// </summary>
    Task<IEntraAuthenticationResult> GetTokenForServicePrincipalAsync(
        string[] scopes,
        ServicePrincipalIdentity sp,
        CancellationToken ct = default
    );

    /// <summary>
    /// Acquire an access token for a managed identity.
    /// </summary>
    Task<IEntraAuthenticationResult> GetTokenForManagedIdentityAsync(
        string resource,
        ManagedIdentity mi,
        CancellationToken ct = default
    );

    /// <summary>
    /// Acquire an access token using workload federation.
    /// </summary>
    Task<IEntraAuthenticationResult> GetTokenUsingWorkloadFederationAsync(
        string[] scopes,
        WorkloadFederationOptions fedOpts,
        CancellationToken ct = default
    );
}

public interface IEntraAuthenticationResult
{
    string AccessToken { get; }
    IEntraAccount Account { get; }
}
