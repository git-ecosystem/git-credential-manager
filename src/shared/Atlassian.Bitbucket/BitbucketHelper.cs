using System;
using Atlassian.Bitbucket.Cloud;
using Atlassian.Bitbucket.DataCenter;
using GitCredentialManager;

namespace Atlassian.Bitbucket
{
    public static class BitbucketHelper
    {

        private static bool TryGetHttpPath(ISettings settings, out string httpPath)
        {
            return settings.TryGetSetting(
                DataCenterConstants.EnvironmentVariables.HttpPath,
                Constants.GitConfiguration.Credential.SectionName, DataCenterConstants.GitConfiguration.Credential.HttpPath,
                out httpPath);
        }

        public static string GetBaseUri(ISettings settings)
        {
            var remoteUri = settings?.RemoteUri;
            if (remoteUri == null)
            {
                throw new ArgumentException("RemoteUri must be defined to generate Bitbucket DC Rest/OAuth endpoints");
            }

            var pathParts = remoteUri.PathAndQuery.Split('/');
            var pathPart = remoteUri.PathAndQuery.StartsWith("/") ? pathParts[1] : pathParts[0];
            var path = !string.IsNullOrWhiteSpace(pathPart) ? "/" + pathPart : null;
            if(path == null && TryGetHttpPath(settings, out string httpPath) && !string.IsNullOrEmpty(httpPath))
            {
                path = httpPath;
            }
            return $"{remoteUri.Scheme}://{remoteUri.Host}:{remoteUri.Port}{path}";

        }

        public static bool IsBitbucketOrg(InputArguments input)     
        {
            return IsBitbucketOrg(input.GetRemoteUri());
        }

        public static bool IsBitbucketOrg(Uri targetUri)
        {
            return IsBitbucketOrg(targetUri.Host);
        }

        public static bool IsBitbucketOrg(string targetHost)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(targetHost, CloudConstants.BitbucketBaseUrlHost);
        }
    }
}
