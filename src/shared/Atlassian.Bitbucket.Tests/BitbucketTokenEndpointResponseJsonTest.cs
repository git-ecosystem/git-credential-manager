using System;
using System.Text.Json;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketTokenEndpointResponseJsonTest
    {
        [Fact]
        public void BitbucketTokenEndpointResponseJson_Deserialize_Uses_Scopes()
        {
            var accessToken = "123";
            var tokenType = "Bearer";
            var expiresIn = 1000;
            var scopesString = "x,y,z";
            var scopeString = "a,b,c";

            var json = $"{{\"access_token\": \"{accessToken}\", \"token_type\": \"{tokenType}\", \"expires_in\": {expiresIn}, \"scopes\": \"{scopesString}\", \"scope\": \"{scopeString}\"}}";

            var result = JsonSerializer.Deserialize<BitbucketTokenEndpointResponseJson>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            Assert.Equal(accessToken, result.AccessToken);
            Assert.Equal(tokenType, result.TokenType);
            Assert.Equal(expiresIn, result.ExpiresIn);
            Assert.Equal(scopesString, result.Scope);
        }
    }
}
