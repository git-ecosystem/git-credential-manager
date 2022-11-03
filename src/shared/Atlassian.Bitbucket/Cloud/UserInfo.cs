using System;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket.Cloud
{
    public class UserInfo : IUserInfo
    {
        [JsonProperty("has_2fa_enabled")]
        public bool IsTwoFactorAuthenticationEnabled { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("account_id")]
        public string AccountId { get; set; }

        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }
    }
}
