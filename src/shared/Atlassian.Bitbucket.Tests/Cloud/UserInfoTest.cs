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
            var uuid = System.Guid.NewGuid();
            var userInfo = new UserInfo()
            {
                AccountId = "abc",
                IsTwoFactorAuthenticationEnabled = false,
                UserName = "123",
                Uuid = uuid
            };

            Assert.Equal("abc", userInfo.AccountId);
            Assert.False(userInfo.IsTwoFactorAuthenticationEnabled);
            Assert.Equal("123", userInfo.UserName);
            Assert.Equal(uuid, userInfo.Uuid);
        }
    }
}