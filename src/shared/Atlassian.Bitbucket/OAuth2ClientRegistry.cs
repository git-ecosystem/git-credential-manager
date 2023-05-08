using System.Net.Http;
using GitCredentialManager;

namespace Atlassian.Bitbucket
{
    public class OAuth2ClientRegistry : DisposableObject, IRegistry<BitbucketOAuth2Client>
    {
        private readonly ICommandContext _context;

        private HttpClient _httpClient;
        private Cloud.BitbucketOAuth2Client _cloudClient;
        private DataCenter.BitbucketOAuth2Client _dataCenterClient;

        public OAuth2ClientRegistry(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));
            _context = context;
        }

        public BitbucketOAuth2Client Get(InputArguments input)
        {
            if (!BitbucketHelper.IsBitbucketOrg(input))
            {
                return DataCenterClient;
            }

            return CloudClient;
        }

        protected override void ReleaseManagedResources()
        {
            _httpClient?.Dispose();
            _cloudClient = null;
            _dataCenterClient = null;
            base.ReleaseManagedResources();
        }

        private HttpClient HttpClient => _httpClient ??= _context.HttpClientFactory.CreateClient();

        private Cloud.BitbucketOAuth2Client CloudClient =>
            _cloudClient ??= new Cloud.BitbucketOAuth2Client(HttpClient, _context.Settings, _context.Trace2);

        private DataCenter.BitbucketOAuth2Client DataCenterClient =>
            _dataCenterClient ??= new DataCenter.BitbucketOAuth2Client(HttpClient, _context.Settings, _context.Trace2);
    }
}
