using Microsoft.Identity.Client;

namespace GitCredentialManager.Authentication.Entra;

public partial class EntraAuthentication : AuthenticationBase, IEntraAuthentication
{
    private readonly IMsalHttpClientFactory _httpFactory;

    public static readonly string[] AuthorityIds =
    {
        "msa",  "microsoft",   "microsoftaccount",
        "aad",  "azure",       "azuredirectory",
        "live", "liveconnect", "liveid",
    };

    /// <summary>
    /// Create a new Entra authentication component.
    /// </summary>
    /// <param name="context">Command context.</param>
    /// <param name="publicClientConfig">Public application configuration. Required when calling public client APIs.</param>
    public EntraAuthentication(ICommandContext context, PublicClientConfig publicClientConfig = null)
        : base(context)
    {
        _publicClientConfig = publicClientConfig;
        _httpFactory = new MsalHttpClientFactoryAdaptor(context.HttpClientFactory);
    }

    private class AuthResult : IEntraAuthenticationResult
    {
        private AuthResult() { }

        public static IEntraAuthenticationResult FromMsalResult(AuthenticationResult result) =>
            new AuthResult
            {
                AccessToken = result.AccessToken,
                Account = result.Account is null ? null : EntraAccount.FromMsalAccount(result.Account)
            };

        public string AccessToken { get; private init; }
        public IEntraAccount Account { get; private init; }
    }
}
