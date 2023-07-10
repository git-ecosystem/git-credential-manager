using System;

namespace Atlassian.Bitbucket.DataCenter
{
    public class UserInfo : IUserInfo
    {
        // Bitbucket DC does not support this property per-user
        public bool IsTwoFactorAuthenticationEnabled { get => false; }

        public string UserName { get; set; }
    }
}