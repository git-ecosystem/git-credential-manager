using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlassian.Bitbucket.DataCenter;
using Xunit;

namespace Atlassian.Bitbucket.Tests.DataCenter
{
    public class LoginOptionsTest
    {

        [Fact]
        public void LoginOptions_Set()
        {
            var loginOption = new LoginOption() 
            { 
                Type = "abc", 
                Id = 1
            };

            var results = new List<LoginOption>() 
            { 
                loginOption
            };

            var loginOptions = new LoginOptions()
            {
                Results = results
            };

            Assert.NotNull(loginOptions.Results);
            Assert.Contains(loginOption, loginOptions.Results);

            Assert.Equal("abc", loginOptions.Results.First().Type);
            Assert.Equal(1, loginOptions.Results.First().Id);
        }
    }
}