// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
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

        protected override async Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider)
        {
            ICredential credential = await provider.GetCredentialAsync(input);

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
            output["username"] = credential.UserName;
            output["password"] = credential.Password;

            // Write the values to standard out
            context.Streams.Out.WriteDictionary(output);
        }
    }
}
