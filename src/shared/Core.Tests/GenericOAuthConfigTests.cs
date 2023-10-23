using System;
using System.Collections.Generic;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class GenericOAuthConfigTests
    {
        [Fact]
        public void GenericOAuthConfig_TryGet_Valid_ReturnsTrue()
        {
            var protocol = "https";
            var host = "example.com";
            var remoteUri = new Uri($"{protocol}://{host}");
            const string expectedClientId = "115845b0-77f8-4c06-a3dc-7d277381fad1";
            const string expectedClientSecret = "4D35385D9F24";
            const string expectedUserName = "TEST_USER";
            const string authzEndpoint = "/oauth/authorize";
            const string tokenEndpoint = "/oauth/token";
            const string deviceEndpoint = "/oauth/device";
            string[] expectedScopes = { "scope1", "scope2" };
            var expectedRedirectUri = new Uri("http://localhost:12345");
            var expectedAuthzEndpoint = new Uri(remoteUri, authzEndpoint);
            var expectedTokenEndpoint = new Uri(remoteUri, tokenEndpoint);
            var expectedDeviceEndpoint = new Uri(remoteUri, deviceEndpoint);

            string GetKey(string name) => $"{Constants.GitConfiguration.Credential.SectionName}.https://example.com.{name}";

            var trace = new NullTrace();
            var settings = new TestSettings
            {
                GitConfiguration = new TestGitConfiguration
                {
                    Global =
                    {
                        [GetKey(Constants.GitConfiguration.Credential.OAuthClientId)] = new[] { expectedClientId },
                        [GetKey(Constants.GitConfiguration.Credential.OAuthClientSecret)] = new[] { expectedClientSecret },
                        [GetKey(Constants.GitConfiguration.Credential.OAuthRedirectUri)] = new[] { expectedRedirectUri.ToString() },
                        [GetKey(Constants.GitConfiguration.Credential.OAuthScopes)] = new[] { string.Join(' ', expectedScopes) },
                        [GetKey(Constants.GitConfiguration.Credential.OAuthAuthzEndpoint)] = new[] { authzEndpoint },
                        [GetKey(Constants.GitConfiguration.Credential.OAuthTokenEndpoint)] = new[] { tokenEndpoint },
                        [GetKey(Constants.GitConfiguration.Credential.OAuthDeviceEndpoint)] = new[] { deviceEndpoint },
                        [GetKey(Constants.GitConfiguration.Credential.OAuthDefaultUserName)] = new[] { expectedUserName },
                    }
                },
                RemoteUri = remoteUri
            };

            var input = new InputArguments(new Dictionary<string, string> {
                {"protocol", protocol},
                {"host", host}, 
            });

            bool result = GenericOAuthConfig.TryGet(trace, settings, input, out GenericOAuthConfig config);

            Assert.True(result);
            Assert.Equal(expectedClientId, config.ClientId);
            Assert.Equal(expectedClientSecret, config.ClientSecret);
            Assert.Equal(expectedRedirectUri, config.RedirectUri);
            Assert.Equal(expectedScopes, config.Scopes);
            Assert.Equal(expectedAuthzEndpoint, config.Endpoints.AuthorizationEndpoint);
            Assert.Equal(expectedTokenEndpoint, config.Endpoints.TokenEndpoint);
            Assert.Equal(expectedDeviceEndpoint, config.Endpoints.DeviceAuthorizationEndpoint);
            Assert.Equal(expectedUserName, config.DefaultUserName);
            Assert.True(config.UseAuthHeader);
        }

        [Fact]
        public void GenericOAuthConfig_TryGet_Gitea()
        {
            var protocol = "https";
            var host = "example.com";
            var remoteUri = new Uri($"{protocol}://{host}");
            const string expectedClientId = GenericOAuthConfig.WellKnown.GiteaClientId;
            string[] expectedScopes = Array.Empty<string>();
            var expectedRedirectUri = GenericOAuthConfig.WellKnown.LocalIPv4RedirectUri;
            var expectedAuthzEndpoint = new Uri(remoteUri, GenericOAuthConfig.WellKnown.GiteaAuthzEndpoint);
            var expectedTokenEndpoint = new Uri(remoteUri, GenericOAuthConfig.WellKnown.GiteaTokenEndpoint);

            var trace = new NullTrace();
            var settings = new TestSettings
            {
                RemoteUri = remoteUri
            };

            var input = new InputArguments(new Dictionary<string, string> {
                {"protocol", protocol},
                {"host", host},
                {"wwwauth", "Basic realm=\"Gitea\""}
            });

            bool result = GenericOAuthConfig.TryGet(trace, settings, input, out GenericOAuthConfig config);

            Assert.True(result);
            Assert.Equal(expectedClientId, config.ClientId);
            Assert.Equal(expectedRedirectUri, config.RedirectUri);
            Assert.Equal(expectedScopes, config.Scopes);
            Assert.Equal(expectedAuthzEndpoint, config.Endpoints.AuthorizationEndpoint);
            Assert.Equal(expectedTokenEndpoint, config.Endpoints.TokenEndpoint);
        }
    }
}
