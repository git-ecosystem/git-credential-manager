// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    /// <summary>
    /// Erase a previously stored <see cref="GitCredential"/> from the OS secure credential store.
    /// </summary>
    public class EraseCommand : HostProviderCommandBase
    {
        public EraseCommand(IHostProviderRegistry hostProviderRegistry)
            : base(hostProviderRegistry) { }

        protected override string Name => "erase";

        protected override Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider, string credentialKey)
        {
            context.Trace.WriteLine("Erasing credential...");

            // Delete the credential in the store.
            if (context.CredentialStore.Remove(credentialKey))
            {
                context.Trace.WriteLine("Credential was successfully erased.");
            }

            return Task.CompletedTask;
        }
    }
}
