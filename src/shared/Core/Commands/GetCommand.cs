using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitCredentialManager.Commands
{
    /// <summary>
    /// Acquire a new <see cref="GitCredential"/> from a <see cref="IHostProvider"/>.
    /// </summary>
    public class GetCommand : GitCommandBase
    {
        public GetCommand(ICommandContext context, IHostProviderRegistry hostProviderRegistry)
            : base(context, "get", "[Git] Return a stored credential", hostProviderRegistry) { }

        protected override async Task ExecuteInternalAsync(InputArguments input, IHostProvider provider)
        {
            GetCredentialResult result = await provider.GetCredentialAsync(input);
            ICredential credential = result.Credential;

            var output = new Dictionary<string, string>();

            // Echo protocol, host, and path back at Git
            if (input.Protocol != null)
            {
                output["protocol"] = input.Protocol;
            }
            if (input.Host != null)
            {
                output["host"] = input.Host;
            }
            if (input.Path != null)
            {
                output["path"] = input.Path;
            }

            // Return the credential to Git
            output["username"] = credential.Account;
            output["password"] = credential.Password;

            // Write any additional output from the provider
            foreach (var kvp in result.AdditionalProperties)
            {
                output[kvp.Key] = kvp.Value;
            }

            Context.Trace.WriteLine("Writing credentials to output:");
            Context.Trace.WriteDictionarySecrets(output, new []{ "password" }, StringComparer.OrdinalIgnoreCase);

            // Write the values to standard out
            Context.Streams.Out.WriteDictionary(output);
        }
    }
}
