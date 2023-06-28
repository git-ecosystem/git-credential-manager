using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Atlassian.Bitbucket.Cloud;
using Xunit;

namespace Atlassian.Bitbucket.Tests.Cloud
{
    public class UserInfoTest
    {
        [Fact]
        public void UserInfo_Set()
        {
            var userInfo = new UserInfo()
            {
                UserName = "123",
            };

            Assert.Equal("123", userInfo.UserName);
        }

        [Fact]
        public void Deserialize_UserInfo()
        {
            var uuid = "{bef4bd75-03fe-4f19-9c6c-ed57b05ab6f6}";
            var userName = "bob";
            var accountId = "123abc";

            var json = $"{{\"uuid\": \"{uuid}\", \"has_2fa_enabled\": null, \"username\": \"{userName}\", \"account_id\": \"{accountId}\"}}";

            var result = JsonSerializer.Deserialize<UserInfo>(json, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            });

            Assert.Equal(userName, result.UserName);
        }
    }
}
