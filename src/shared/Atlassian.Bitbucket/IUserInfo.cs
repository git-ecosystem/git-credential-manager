using System;
namespace Atlassian.Bitbucket
{
    public interface IUserInfo
    {
        string UserName{ get; }
        bool IsTwoFactorAuthenticationEnabled { get; }
    }
}
