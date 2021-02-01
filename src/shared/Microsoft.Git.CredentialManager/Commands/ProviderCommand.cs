using System.CommandLine;

namespace Microsoft.Git.CredentialManager.Commands
{
    public interface ICommandProvider
    {
        /// <summary>
        /// Create a custom provider command.
        /// </summary>
        ProviderCommand CreateCommand();
    }

    public class ProviderCommand : Command
    {
        public ProviderCommand(IHostProvider provider)
            : base(provider.Id, $"Commands for interacting with the {provider.Name} host provider")
        {
        }
    }
}
