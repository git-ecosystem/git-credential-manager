using System;
using System.Threading.Tasks;

namespace GitCredentialManager.Tests.Objects
{
    public class TestHostProvider : HostProvider
    {
        public TestHostProvider(ICommandContext context)
            : base(context) { }

        public Func<GitRequest, bool> IsSupportedFunc { get; set; }

        public string LegacyAuthorityIdValue { get; set; }

        public Func<GitRequest, ICredential> GenerateCredentialFunc { get; set; }

        #region HostProvider

        public override string Id { get; } = "test-provider";

        public override string Name { get; } = "TestHostProvider";

        public string LegacyAuthorityId => LegacyAuthorityIdValue;

        public override bool IsSupported(GitRequest request) => IsSupportedFunc(request);

        public override Task<ICredential> GenerateCredentialAsync(GitRequest request)
        {
            return Task.FromResult(GenerateCredentialFunc(request));
        }

        #endregion
    }
}
