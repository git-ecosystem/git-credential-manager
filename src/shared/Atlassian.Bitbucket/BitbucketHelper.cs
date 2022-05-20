using System;
using Atlassian.Bitbucket.Cloud;
using GitCredentialManager;

namespace Atlassian.Bitbucket
{
    public static class BitbucketHelper
    {
        public static string GetBaseUri(Uri remoteUri)
        {
            var pathParts = remoteUri.PathAndQuery.Split('/');
            var pathPart = remoteUri.PathAndQuery.StartsWith("/") ? pathParts[1] : pathParts[0];
            var path = !string.IsNullOrWhiteSpace(pathPart) ? "/" + pathPart : null;
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
