using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlassian.Bitbucket
{
    public interface IBitbucketRestApi : IDisposable
    {
        Task<RestApiResult<IUserInfo>> GetUserInformationAsync(string userName, string password, bool isBearerToken);
        Task<bool> IsOAuthInstalledAsync();
        Task<List<AuthenticationMethod>> GetAuthenticationMethodsAsync();
    }
}
