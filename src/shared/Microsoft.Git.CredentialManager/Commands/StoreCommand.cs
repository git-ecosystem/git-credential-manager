// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    /// <summary>
    /// Store a previously created <see cref="GitCredential"/> in the OS secure credential store.
    /// </summary>
    public class StoreCommand : GitCommandBase
    {
        public StoreCommand(ICommandContext context, IHostProviderRegistry hostProviderRegistry)
            : base(context, "store", "[Git] Store a credential", hostProviderRegistry) { }

        protected override Task ExecuteInternalAsync(InputArguments input, IHostProvider provider)
        {
            return provider.StoreCredentialAsync(input);
        }

        protected override void EnsureMinimumInputArguments(InputArguments input)
        {
            base.EnsureMinimumInputArguments(input);

            // An empty string username/password are valid inputs, so only check for `null` (not provided)
            if (input.UserName is null)
            {
                throw new InvalidOperationException("Missing 'username' input argument");
            }

            if (input.Password is null)
            {
                throw new InvalidOperationException("Missing 'password' input argument");
            }
        }
    }
}
