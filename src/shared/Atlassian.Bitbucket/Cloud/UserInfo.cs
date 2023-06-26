using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atlassian.Bitbucket.Cloud
{
    public class UserInfo : IUserInfo
    {
        [JsonPropertyName("username")]
        public string UserName { get; set; }
    }
}
