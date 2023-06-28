using Atlassian.Bitbucket.Cloud;
using GitCredentialManager;

namespace Atlassian.Bitbucket
{
    public class BitbucketRestApiRegistry : IRegistry<IBitbucketRestApi>
    {
        private readonly ICommandContext context;
        private BitbucketRestApi cloudApi;
        private DataCenter.BitbucketRestApi dataCenterApi;

        public BitbucketRestApiRegistry(ICommandContext context)
        {
            this.context = context;
        }

        public IBitbucketRestApi Get(InputArguments input)
        {
            if(!BitbucketHelper.IsBitbucketOrg(input))
            {
                return DataCenterApi;
            }

            return CloudApi;
        }

        public void Dispose()
        {
            context.Dispose();
            cloudApi?.Dispose();
            dataCenterApi?.Dispose();
        }

        private Cloud.BitbucketRestApi CloudApi => cloudApi ??= new Cloud.BitbucketRestApi(context);
        private DataCenter.BitbucketRestApi DataCenterApi => dataCenterApi ??= new DataCenter.BitbucketRestApi(context);
    }
}