using System;
using System.Threading.Tasks;

namespace GitCredentialManager.Tests.Objects
{
    public class TestHostProvider : HostProvider
    {
        public TestHostProvider(ICommandContext context)
            : base(context) { }

        public Func<InputArguments, bool> IsSupportedFunc { get; set; }

        public string LegacyAuthorityIdValue { get; set; }

        public Func<InputArguments, ICredential> GenerateCredentialFunc { get; set; }

        public Func<Uri, ICredential, bool> ValidateCredentialFunc { get; set; }

        #region HostProvider

        public override string Id { get; } = "test-provider";

        public override string Name { get; } = "TestHostProvider";

        public string LegacyAuthorityId => LegacyAuthorityIdValue;

        public override bool IsSupported(InputArguments input) => IsSupportedFunc(input);

        public override Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            return Task.FromResult(GenerateCredentialFunc(input));
        }

        public override Task<bool> ValidateCredentialAsync(Uri remoteUri, ICredential credential)
            => ValidateCredentialFunc != null ? Task.FromResult(ValidateCredentialFunc(remoteUri, credential)) : base.ValidateCredentialAsync(remoteUri, credential);

        #endregion
    }
}
