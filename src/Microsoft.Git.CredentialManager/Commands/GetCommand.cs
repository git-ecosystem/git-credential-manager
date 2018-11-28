using System;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    /// <summary>
    /// Acquire a new <see cref="GitCredential"/> from a <see cref="IHostProvider"/>.
    /// </summary>
    public class GetCommand : HostProviderCommandBase
    {
        public GetCommand(IHostProviderRegistry hostProviderRegistry)
            : base(hostProviderRegistry) { }

        protected override string Name => "get";

        protected override Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider, string credentialKey)
        {
            throw new NotImplementedException();
        }
    }
}
