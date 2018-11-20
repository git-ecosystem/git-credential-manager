using System;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    public class StoreCommand : HostProviderCommandBase
    {
        public StoreCommand(IHostProviderRegistry hostProviderRegistry)
            : base(hostProviderRegistry) { }

        protected override string Name => "store";

        protected override Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider, string credentialKey)
        {
            throw new NotImplementedException();
        }
    }
}
