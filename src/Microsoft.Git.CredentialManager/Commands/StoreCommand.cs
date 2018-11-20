// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    /// <summary>
    /// Store a previously created <see cref="GitCredential"/> in the OS secure credential store.
    /// </summary>
    public class StoreCommand : HostProviderCommandBase
    {
        public StoreCommand(IHostProviderRegistry hostProviderRegistry)
            : base(hostProviderRegistry) { }

        protected override string Name => "store";

        protected override Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider, string credentialKey)
        {
            // Some providers may choose to store the credential when initially asked for it during
            // a previous `get` call (where there might be more information available in the context
            // or input which is no longer present).
            // To prevent 'double stores' we should ask the provider if they store on create.
            if (!provider.IsCredentialStoredOnCreation)
            {
                // Create the credential based on Git's input
                string userName = input.UserName;
                string password = input.Password;
                var credential = new GitCredential(userName, password);

                // Add or update the credential in the store.
                context.Trace.WriteLine("Storing credential...");
                context.CredentialStore.AddOrUpdate(credentialKey, credential);
                context.Trace.WriteLine("Credential was successfully stored.");
            }
            else
            {
                context.Trace.WriteLine("Skipping 'store' because provider stores credentials on 'get'.");
            }

            return Task.CompletedTask;
        }
    }
}
