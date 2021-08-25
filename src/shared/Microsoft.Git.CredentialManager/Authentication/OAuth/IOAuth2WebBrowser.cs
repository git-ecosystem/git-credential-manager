using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Authentication.OAuth
{
    public interface IOAuth2WebBrowser
    {
        Uri UpdateRedirectUri(Uri uri);

        Task<Uri> GetAuthenticationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken ct);
    }
}
