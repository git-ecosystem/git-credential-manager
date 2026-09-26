using System;
using System.Threading.Tasks;

namespace GitCredentialManager.Commands
{
    /// <summary>
    /// Store a previously created <see cref="GitCredential"/> in the OS secure credential store.
    /// </summary>
    public class StoreCommand : GitCommandBase
    {
        public StoreCommand(ICommandContext context, IHostProviderRegistry hostProviderRegistry)
            : base(context, "store", "[Git] Store a credential", hostProviderRegistry)
        {
            IsHidden = true;
        }

        protected override async Task ExecuteInternalAsync(GitRequest request, IHostProvider provider)
        {
            using var _ = Trace2.StartRegion("git_cmd_store", "provider_store");
            await provider.StoreCredentialAsync(request);
        }

        protected override void EnsureMinimumRequest(GitRequest request)
        {
            base.EnsureMinimumRequest(request);

            // When "authtype" is set, Git uses "credential" field for the secret
            if (request.AuthType is not null)
            {
                if (request.Credential is null)
                {
                    throw new InvalidOperationException("Missing 'credential' request argument");
                }
                return;
            }

            // An empty string username/password are valid inputs, so only check for `null` (not provided)
            if (request.UserName is null)
            {
                throw new InvalidOperationException("Missing 'username' request argument");
            }

            if (request.Password is null)
            {
                throw new InvalidOperationException("Missing 'password' request argument");
            }
        }
    }
}
