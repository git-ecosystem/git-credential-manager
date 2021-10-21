using System.Threading.Tasks;

namespace GitCredentialManager.Commands
{
    /// <summary>
    /// Erase a previously stored <see cref="GitCredential"/> from the OS secure credential store.
    /// </summary>
    public class EraseCommand : GitCommandBase
    {
        public EraseCommand(ICommandContext context, IHostProviderRegistry hostProviderRegistry)
            : base(context, "erase", "[Git] Erase a stored credential", hostProviderRegistry) { }

        protected override Task ExecuteInternalAsync(InputArguments input, IHostProvider provider)
        {
            return provider.EraseCredentialAsync(input);
        }
    }
}
