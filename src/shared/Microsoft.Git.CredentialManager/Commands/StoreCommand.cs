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

        protected override Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider)
        {
            return provider.StoreCredentialAsync(input);
        }
    }
}
