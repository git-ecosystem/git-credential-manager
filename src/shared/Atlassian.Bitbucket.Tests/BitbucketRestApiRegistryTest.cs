using System.Collections.Generic;
using GitCredentialManager;
using Moq;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketRestApiRegistryTest
    {
        private Mock<ICommandContext> context = new Mock<ICommandContext>(MockBehavior.Strict);
        private Mock<ISettings> settings = new Mock<ISettings>(MockBehavior.Strict);

        [Fact]
        public void BitbucketRestApiRegistry_Get_ReturnsCloudApi_ForBitbucketOrg()
        {
            // Given
            settings.Setup(s => s.RemoteUri).Returns(new System.Uri("https://bitbucket.org"));
            context.Setup(c => c.Settings).Returns(settings.Object);

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "bitbucket.org",
            });

            // When
            var registry = new BitbucketRestApiRegistry(context.Object);
            var api = registry.Get(input);
        
            // Then
            Assert.NotNull(api);
            Assert.IsType<Atlassian.Bitbucket.Cloud.BitbucketRestApi>(api);

        }

        [Fact]
        public void BitbucketRestApiRegistry_Get_ReturnsDataCenterApi_ForBitbucketDC()
        {
            // Given
            settings.Setup(s => s.RemoteUri).Returns(new System.Uri("https://example.com"));
            context.Setup(c => c.Settings).Returns(settings.Object);

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "example.com",
            });

            // When
            var registry = new BitbucketRestApiRegistry(context.Object);
            var api = registry.Get(input);

            // Then
            Assert.NotNull(api);
            Assert.IsType<Atlassian.Bitbucket.DataCenter.BitbucketRestApi>(api);
        }
    }
}