using Atlassian.Bitbucket.Cloud;
using GitCredentialManager;

namespace Atlassian.Bitbucket
{
    public class BitbucketRestApiRegistry : IRegistry<IBitbucketRestApi>
    {
        private readonly ICommandContext context;
        private BitbucketRestApi cloudApi;

        public BitbucketRestApiRegistry(ICommandContext context)
        {
            this.context = context;
        }

        public IBitbucketRestApi Get(InputArguments input)
        {
            return CloudApi;
        }

        public void Dispose()
        {
            context.Dispose();
            cloudApi?.Dispose();
        }

        private Cloud.BitbucketRestApi CloudApi => cloudApi ??= new Cloud.BitbucketRestApi(context);
    }
}