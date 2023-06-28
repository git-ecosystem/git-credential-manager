using System.Threading.Tasks;
using Atlassian.Bitbucket.DataCenter;
using Xunit;

namespace Atlassian.Bitbucket.Tests.DataCenter
{
    public class UserInfoTest
    {
        [Fact]
        public void UserInfo_Set()
        {
            var userInfo = new UserInfo()
            {
                UserName = "123"
            };

            Assert.Equal("123", userInfo.UserName);
        }
    }
}
