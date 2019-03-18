// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
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
            // Try to locate an existing credential with the computed key
            context.Trace.WriteLine("Looking for existing credential in store...");
            ICredential credential = context.CredentialStore.Get(credentialKey);
            if (credential == null)
            {
                context.Trace.WriteLine("No stored credential was found.");
                return Task.CompletedTask;
            }
            else
            {
                context.Trace.WriteLine("Existing credential found.");
            }

            // If we've been given a specific username and/or password we should only proceed
            // to erase the stored credential if they match exactly
            if (!string.IsNullOrWhiteSpace(input.UserName) && !StringComparer.Ordinal.Equals(input.UserName, credential.UserName))
            {
                context.Trace.WriteLine("Stored username does not match specified username - not erasing credential.");
                context.Trace.WriteLine($"\tInput  username={input.UserName}");
                context.Trace.WriteLine($"\tStored username={credential.UserName}");
                return Task.CompletedTask;
            }

            if (!string.IsNullOrWhiteSpace(input.Password) && !StringComparer.Ordinal.Equals(input.Password, credential.Password))
            {
                context.Trace.WriteLine("Stored password does not match specified password - not erasing credential.");
                context.Trace.WriteLineSecrets("\tInput  password={0}", new object[] {input.Password});
                context.Trace.WriteLineSecrets("\tStored password={0}", new object[] {credential.Password});
                return Task.CompletedTask;
            }

            context.Trace.WriteLine("Erasing stored credential...");
            if (context.CredentialStore.Remove(credentialKey))
            {
                context.Trace.WriteLine("Credential was successfully erased.");
            }
            else
            {
                context.Trace.WriteLine("Credential erase failed.");
            }

            return Task.CompletedTask;
        }
    }
}
