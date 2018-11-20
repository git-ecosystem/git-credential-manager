using System;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    public class EraseCommand : HostProviderCommandBase
    {
        public EraseCommand(IHostProviderRegistry hostProviderRegistry)
            : base(hostProviderRegistry) { }

        protected override string Name => "erase";

        protected override Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider, string credentialKey)
        {
            throw new NotImplementedException();
        }
    }
}
