using System.Net.Http;
using GitCredentialManager;

namespace Atlassian.Bitbucket
{
    public class OAuth2ClientRegistry : IRegistry<BitbucketOAuth2Client>
    {
        private readonly HttpClient http;
        private ISettings settings;
        private readonly ITrace trace;
        private Cloud.BitbucketOAuth2Client cloudClient;
        private DataCenter.BitbucketOAuth2Client dataCenterClient;

        public OAuth2ClientRegistry(ICommandContext context)
        {
            this.http = context.HttpClientFactory.CreateClient();
            this.settings = context.Settings;
            this.trace = context.Trace;
        }

        public BitbucketOAuth2Client Get(InputArguments input)
        {
            if (!BitbucketHelper.IsBitbucketOrg(input))
            {
                return DataCenterClient;
            }

            return CloudClient;
        }

        public void Dispose()
        {
            http.Dispose();
            settings.Dispose();
            cloudClient = null;
            dataCenterClient = null;
        }

        private Cloud.BitbucketOAuth2Client CloudClient => cloudClient ??= new Cloud.BitbucketOAuth2Client(http, settings, trace);
        private DataCenter.BitbucketOAuth2Client DataCenterClient => dataCenterClient ??= new DataCenter.BitbucketOAuth2Client(http, settings, trace);
    }
}