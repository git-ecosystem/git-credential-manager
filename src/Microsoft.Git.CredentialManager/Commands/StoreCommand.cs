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
            // Create the credential based on Git's input
            string userName = input.UserName;
            string password = input.Password;
            var credential = new GitCredential(userName, password);

            // Add or update the credential in the store.
            context.Trace.WriteLine("Storing credential...");
            context.CredentialStore.AddOrUpdate(credentialKey, credential);
            context.Trace.WriteLine("Credential was successfully stored.");

            return Task.CompletedTask;
        }
    }
}
