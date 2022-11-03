using Newtonsoft.Json;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketTokenEndpointResponseJsonTest
    {
        [Fact]
        public void BitbucketTokenEndpointResponseJson_Deserialize_Scopes_Not_Scope()
        {
            var scopesString = "a,b,c";
            var json = "{access_token: '', token_type: '', scopes:'" + scopesString + "', scope: 'x,y,z'}";

            var result = JsonConvert.DeserializeObject<BitbucketTokenEndpointResponseJson>(json);

            Assert.Equal(scopesString, result.Scope);
        }
    }
}