using System;
using System.Collections.Generic;
using System.Net.Http;
using Atlassian.Bitbucket.Cloud;
using Atlassian.Bitbucket.DataCenter;
using GitCredentialManager;
using Moq;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class OAuth2ClientRegistryTest
    {
        private Mock<ICommandContext> context = new Mock<ICommandContext>(MockBehavior.Loose);
        private Mock<ISettings> settings = new Mock<ISettings>(MockBehavior.Strict);
        private Mock<IHttpClientFactory> httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        private Mock<ITrace> trace = new Mock<ITrace>(MockBehavior.Strict);

        [Fact]
        public void BitbucketRestApiRegistry_Get_ReturnsCloudOAuth2Client()
        {
            var host = "bitbucket.org";

            // Given
            settings.Setup(s => s.RemoteUri).Returns(new System.Uri("https://" + host));
            context.Setup(c => c.Settings).Returns(settings.Object);
            MockSettingOverride(CloudConstants.EnvironmentVariables.OAuthClientId, CloudConstants.GitConfiguration.Credential.OAuthClientId, "never used", false);
            MockSettingOverride(CloudConstants.EnvironmentVariables.OAuthClientSecret, CloudConstants.GitConfiguration.Credential.OAuthClientSecret, "never used", false);
            MockSettingOverride(CloudConstants.EnvironmentVariables.OAuthRedirectUri, CloudConstants.GitConfiguration.Credential.OAuthRedirectUri,  "never used", false);
            MockHttpClientFactory();
            var input = MockInputArguments(host);

            // When
            var registry = new OAuth2ClientRegistry(context.Object);
            var api = registry.Get(input);

            // Then
            Assert.NotNull(api);
            Assert.IsType<Atlassian.Bitbucket.Cloud.BitbucketOAuth2Client>(api);

        }

        [Fact]
        public void BitbucketRestApiRegistry_Get_ReturnsDataCenterOAuth2Client_ForBitbucketDC()
        {
            var host = "example.com";

            // Given
            settings.Setup(s => s.RemoteUri).Returns(new System.Uri("https://example.com"));
            context.Setup(c => c.Settings).Returns(settings.Object);
            MockSettingOverride(DataCenterConstants.EnvironmentVariables.OAuthClientId, DataCenterConstants.GitConfiguration.Credential.OAuthClientId, "", true);
            MockSettingOverride(DataCenterConstants.EnvironmentVariables.OAuthClientSecret, DataCenterConstants.GitConfiguration.Credential.OAuthClientSecret, "", true); ;
            MockSettingOverride(DataCenterConstants.EnvironmentVariables.OAuthRedirectUri, DataCenterConstants.GitConfiguration.Credential.OAuthRedirectUri,  "never used", false);
            MockHttpClientFactory();
            var input = MockInputArguments(host);

            // When
            var registry = new OAuth2ClientRegistry(context.Object);
            var api = registry.Get(input);

            // Then
            Assert.NotNull(api);
            Assert.IsType<Atlassian.Bitbucket.DataCenter.BitbucketOAuth2Client>(api);

        }

        private static InputArguments MockInputArguments(string host)
        {
            return new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = host,
            });
        }

        private void MockHttpClientFactory()
        {
            context.Setup(c => c.HttpClientFactory).Returns(httpClientFactory.Object);
            httpClientFactory.Setup(f => f.CreateClient()).Returns(new HttpClient());
        }

        private string MockSettingOverride(string envKey, string configKey, string settingValue, bool isOverridden)
        {
            settings.Setup(s => s.TryGetSetting(
                envKey,
                Constants.GitConfiguration.Credential.SectionName, configKey,
                out settingValue)).Returns(isOverridden);
            return settingValue;
        }
    }
}