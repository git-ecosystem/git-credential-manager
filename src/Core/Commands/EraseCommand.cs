using System.Threading.Tasks;

namespace GitCredentialManager.Commands
{
    /// <summary>
    /// Erase a previously stored <see cref="GitCredential"/> from the OS secure credential store.
    /// </summary>
    public class EraseCommand : GitCommandBase
    {
        public EraseCommand(ICommandContext context, IHostProviderRegistry hostProviderRegistry)
            : base(context, "erase", "[Git] Erase a stored credential", hostProviderRegistry)
        {
            IsHidden = true;
        }

        protected override async Task ExecuteInternalAsync(GitRequest request, IHostProvider provider)
        {
            using var _ = Trace2.StartRegion("git_cmd_erase", "provider_erase");
            await provider.EraseCredentialAsync(request);
        }
    }
}
