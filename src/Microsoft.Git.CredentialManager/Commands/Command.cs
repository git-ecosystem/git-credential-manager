using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    public abstract class CommandBase
    {
        public abstract bool CanExecute(string[] args);

        public abstract Task ExecuteAsync(ICommandContext context, string[] args);
    }

    public abstract class VerbCommandBase : CommandBase
    {
        protected abstract string Name { get; }

        public override bool CanExecute(string[] args)
        {
            return args.Length != 0 && StringComparer.OrdinalIgnoreCase.Equals(args[0], Name);
        }
    }

    public abstract class HostProviderCommandBase : VerbCommandBase
    {
        private readonly IHostProviderRegistry _hostProviderRegistry;

        protected HostProviderCommandBase(IHostProviderRegistry hostProviderRegistry)
        {
            _hostProviderRegistry = hostProviderRegistry;
        }

        public override async Task ExecuteAsync(ICommandContext context, string[] args)
        {
            // Parse standard input arguments
            IDictionary<string, string> inputDict = await context.StdIn.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);
            var input = new InputArguments(inputDict);

            // Determine the host provider
            context.Trace.WriteLine("Detecting host provider for input:");
            context.Trace.WriteDictionary(inputDict);
            IHostProvider provider = _hostProviderRegistry.GetProvider(input);
            context.Trace.WriteLine($"Host provider '{provider.Name}' was selected.");

            // Build the credential identifier
            string hostProviderKey = provider.GetCredentialKey(input);
            string credentialKey = $"git:{hostProviderKey}";
            context.Trace.WriteLine($"Credential key is '{credentialKey}'.");

            await ExecuteInternalAsync(context, input, provider, credentialKey);
        }

        protected abstract Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider, string credentialKey);
    }
}
