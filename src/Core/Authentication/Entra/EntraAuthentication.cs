using Microsoft.Identity.Client;

namespace GitCredentialManager.Authentication.Entra
{
    public partial class EntraAuthentication : AuthenticationBase, IEntraAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "msa",  "microsoft",   "microsoftaccount",
            "aad",  "azure",       "azuredirectory",
            "live", "liveconnect", "liveid",
        };

        public EntraAuthentication(ICommandContext context)
            : base(context) { }

        private class MsalResult : IEntraAuthenticationResult
        {
            private readonly AuthenticationResult _msalResult;

            public MsalResult(AuthenticationResult msalResult)
            {
                _msalResult = msalResult;
            }

            public string AccessToken => _msalResult.AccessToken;
            public string AccountUpn => _msalResult.Account?.Username;
        }
    }
}
