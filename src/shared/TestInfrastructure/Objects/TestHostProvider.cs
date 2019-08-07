// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestHostProvider : HostProvider
    {
        public TestHostProvider(ICommandContext context)
            : base(context) { }

        public Func<InputArguments, bool> IsSupportedFunc { get; set; }

        public string CredentialKey { get; set; }

        public string LegacyAuthorityIdValue { get; set; }

        public Func<InputArguments, ICredential> GenerateCredentialFunc { get; set; }

        #region HostProvider

        public override string Id { get; } = "test-provider";

        public override string Name { get; } = "TestHostProvider";

        public string LegacyAuthorityId => LegacyAuthorityIdValue;

        public override bool IsSupported(InputArguments input) => IsSupportedFunc(input);

        public override string GetCredentialKey(InputArguments input)
        {
            return CredentialKey;
        }

        public override Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            return Task.FromResult(GenerateCredentialFunc(input));
        }

        #endregion
    }
}
