// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.SecureStorage;

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

        protected override async Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider, string credentialKey)
        {
            // Try and locate an existing PAT in the OS credential store
            context.Trace.WriteLine("Looking for existing credential in store...");
            ICredential credential = context.CredentialStore.Get(credentialKey);

            if (credential == null)
            {
                context.Trace.WriteLine("No existing credential found.");

                // No existing PAT was found.
                // Create a new one and add this to the store.
                context.Trace.WriteLine("Creating new credential...");
                credential = await provider.CreateCredentialAsync(input);
                context.Trace.WriteLine("Credential created.");

                if (provider.IsCredentialStoredOnCreation)
                {
                    context.Trace.WriteLine("Storing credential...");
                    context.CredentialStore.AddOrUpdate(credentialKey, credential);
                    context.Trace.WriteLine("Credential stored.");
                }
            }
            else
            {
                context.Trace.WriteLine("Existing credential found.");
            }

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
            context.StdOut.WriteDictionary(output);
        }
    }
}
