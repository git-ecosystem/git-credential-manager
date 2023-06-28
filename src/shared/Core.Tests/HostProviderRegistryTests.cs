using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class HostProviderRegistryTests
    {
        [Fact]
        public void HostProviderRegistry_Register_AutoProviderId_ThrowException()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var provider = new Mock<IHostProvider>();
            provider.Setup(x => x.Id).Returns(Constants.ProviderIdAuto);

            Assert.Throws<ArgumentException>(() => registry.Register(provider.Object, HostProviderPriority.Normal));
        }

        [Fact]
        public void HostProviderRegistry_Register_AutoAuthorityId_ThrowException()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var provider = new Mock<IHostProvider>();
            provider.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"foo", Constants.AuthorityIdAuto, "bar"});

            Assert.Throws<ArgumentException>(() => registry.Register(provider.Object, HostProviderPriority.Normal));
        }

        [Fact]
        public void HostProviderRegistry_GetProvider_NoProviders_ThrowException()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var input = new InputArguments(new Dictionary<string, string>());

            Assert.ThrowsAsync<Exception>(() => registry.GetProviderAsync(input));
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_HasProviders_ReturnsSupportedProvider()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remote = new Uri("https://example.com");
            InputArguments input = CreateInputArguments(remote);

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider2Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider3Mock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider2Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_HasProviders_StaticMatch_DoesNotSetProviderGlobalConfig()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remote = new Uri("https://example.com");
            InputArguments input = CreateInputArguments(remote);

            string providerId = "myProvider";
            string configKey = string.Format(CultureInfo.InvariantCulture,
                "{0}.https://example.com.{1}",
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.Provider);

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.Id).Returns(providerId);
            providerMock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);

            registry.Register(providerMock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(providerMock.Object, result);
            Assert.False(context.Git.Configuration.Global.TryGetValue(configKey, out _));
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_HasProviders_DynamicMatch_SetsProviderGlobalConfig()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remote = new Uri("https://example.com");
            InputArguments input = CreateInputArguments(remote);

            string providerId = "myProvider";
            string configKey = string.Format(CultureInfo.InvariantCulture,
                "{0}.https://example.com.{1}",
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.Provider);

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.Id).Returns(providerId);
            providerMock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            providerMock.Setup(x => x.IsSupported(It.IsAny<HttpResponseMessage>())).Returns(true);

            registry.Register(providerMock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(providerMock.Object, result);
            Assert.True(context.Git.Configuration.Global.TryGetValue(configKey, out IList<string> config));
            Assert.Equal(1, config.Count);
            Assert.Equal(providerId, config[0]);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_HasProviders_DynamicMatch_SetsProviderGlobalConfig_HostWithPath()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remote = new Uri("https://example.com/alice/repo.git/");
            InputArguments input = CreateInputArguments(remote);

            string providerId = "myProvider";
            string configKey = string.Format(CultureInfo.InvariantCulture,
                "{0}.https://example.com/alice/repo.git.{1}", // expect any trailing slash to be removed
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.Provider);

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.Id).Returns(providerId);
            providerMock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            providerMock.Setup(x => x.IsSupported(It.IsAny<HttpResponseMessage>())).Returns(true);

            registry.Register(providerMock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(providerMock.Object, result);
            Assert.True(context.Git.Configuration.Global.TryGetValue(configKey, out IList<string> config));
            Assert.Equal(1, config.Count);
            Assert.Equal(providerId, config[0]);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_MultipleValidProviders_ReturnsFirstRegistered()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remote = new Uri("https://example.com");
            InputArguments input = CreateInputArguments(remote);

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);

            registry.Register(provider1Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider2Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider3Mock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider1Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_MultipleValidProvidersMultipleLevels_ReturnsFirstHighestRegistered()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remote = new Uri("https://example.com");
            InputArguments input = CreateInputArguments(remote);

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            var provider4Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider4Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);

            registry.Register(provider1Mock.Object, HostProviderPriority.Low);
            registry.Register(provider2Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider3Mock.Object, HostProviderPriority.High);
            registry.Register(provider4Mock.Object, HostProviderPriority.Low);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider3Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_ProviderSpecified_ReturnsProvider()
        {
            var context = new TestCommandContext
            {
                Settings = {ProviderOverride = "provider3"}
            };
            var registry = new HostProviderRegistry(context);
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.Id).Returns("provider1");
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider2Mock.Setup(x => x.Id).Returns("provider2");
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.Id).Returns("provider3");
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider2Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider3Mock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider3Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_AutoProviderSpecified_ReturnsFirstSupportedProvider()
        {
            var context = new TestCommandContext
            {
                Settings = {ProviderOverride = Constants.ProviderIdAuto}
            };
            var registry = new HostProviderRegistry(context);
            var remote = new Uri("https://example.com");
            InputArguments input = CreateInputArguments(remote);

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.Id).Returns("provider1");
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.Id).Returns("provider2");
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.Id).Returns("provider3");
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider2Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider3Mock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider2Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_UnknownProviderSpecified_ReturnsFirstSupportedProvider()
        {
            var context = new TestCommandContext
            {
                Settings = {ProviderOverride = "provider42"}
            };
            var registry = new HostProviderRegistry(context);
            var remote = new Uri("https://example.com");
            InputArguments input = CreateInputArguments(remote);

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.Id).Returns("provider1");
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.Id).Returns("provider2");
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.Id).Returns("provider3");
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider2Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider3Mock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider2Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_LegacyAuthoritySpecified_ReturnsProvider()
        {
            var context = new TestCommandContext
            {
                Settings = {LegacyAuthorityOverride = "authorityB"}
            };
            var registry = new HostProviderRegistry(context);
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"authorityA"});
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"authorityB", "authorityC"});
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider3Mock.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"authorityD"});
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider2Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider3Mock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider2Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_AutoLegacyAuthoritySpecified_ReturnsFirstSupportedProvider()
        {
            var context = new TestCommandContext
            {
                Settings = {LegacyAuthorityOverride = Constants.AuthorityIdAuto}
            };
            var registry = new HostProviderRegistry(context);
            var remote = new Uri("https://example.com");
            InputArguments input = CreateInputArguments(remote);

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"authorityA"});
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"authorityB", "authorityC"});
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"authorityD"});
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider2Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider3Mock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider2Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_NetworkProbe_ReturnsSupportedProvider()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remoteUri = new Uri("https://provider2.onprem.example.com");
            InputArguments input = CreateInputArguments(remoteUri);

            var provider1Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<HttpResponseMessage>())).Returns(false);

            var provider2Mock = new Mock<IHostProvider>();
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<HttpResponseMessage>())).Returns(true);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers = { { "X-Provider2", "true" } }
            };

            var httpHandler = new TestHttpMessageHandler();

            httpHandler.Setup(HttpMethod.Head, remoteUri, responseMessage);
            context.HttpClientFactory.MessageHandler = httpHandler;

            registry.Register(provider1Mock.Object, HostProviderPriority.Normal);
            registry.Register(provider2Mock.Object, HostProviderPriority.Normal);

            IHostProvider result = await registry.GetProviderAsync(input);

            httpHandler.AssertRequest(HttpMethod.Head, remoteUri, 1);
            Assert.Same(provider2Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_NetworkProbe_TimeoutZero_NoNetworkCall()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remoteUri = new Uri("https://onprem.example.com");
            var input = new InputArguments(
                new Dictionary<string, string>
                {
                    ["protocol"] = remoteUri.Scheme,
                    ["host"] = remoteUri.Host
                }
            );

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            providerMock.Setup(x => x.IsSupported(It.IsAny<HttpResponseMessage>())).Returns(true);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var httpHandler = new TestHttpMessageHandler();

            httpHandler.Setup(HttpMethod.Head, remoteUri, responseMessage);
            context.HttpClientFactory.MessageHandler = httpHandler;

            registry.Register(providerMock.Object, HostProviderPriority.Normal);

            context.Settings.AutoDetectProviderTimeout = 0;

            await Assert.ThrowsAnyAsync<Exception>(() => registry.GetProviderAsync(input));

            httpHandler.AssertRequest(HttpMethod.Head, remoteUri, 0);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_NetworkProbe_TimeoutNegative_NoNetworkCall()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remoteUri = new Uri("https://onprem.example.com");
            var input = new InputArguments(
                new Dictionary<string, string>
                {
                    ["protocol"] = remoteUri.Scheme,
                    ["host"] = remoteUri.Host
                }
            );

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            providerMock.Setup(x => x.IsSupported(It.IsAny<HttpResponseMessage>())).Returns(true);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var httpHandler = new TestHttpMessageHandler();

            httpHandler.Setup(HttpMethod.Head, remoteUri, responseMessage);
            context.HttpClientFactory.MessageHandler = httpHandler;

            registry.Register(providerMock.Object, HostProviderPriority.Normal);

            context.Settings.AutoDetectProviderTimeout = -1;

            await Assert.ThrowsAnyAsync<Exception>(() => registry.GetProviderAsync(input));

            httpHandler.AssertRequest(HttpMethod.Head, remoteUri, 0);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_NetworkProbe_NoNetwork_ReturnsLastProvider()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var remoteUri = new Uri("https://provider2.onprem.example.com");
            var input = new InputArguments(
                new Dictionary<string, string>
                {
                    ["protocol"] = remoteUri.Scheme,
                    ["host"] = remoteUri.Host
                }
            );

            var highProviderMock = new Mock<IHostProvider>();
            highProviderMock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            highProviderMock.Setup(x => x.IsSupported(It.IsAny<HttpResponseMessage>())).Returns(false);
            registry.Register(highProviderMock.Object, HostProviderPriority.Normal);

            var lowProviderMock = new Mock<IHostProvider>();
            lowProviderMock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            registry.Register(lowProviderMock.Object, HostProviderPriority.Low);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers = { { "X-Provider2", "true" } }
            };

            var httpHandler = new TestHttpMessageHandler
            {
                SimulateNoNetwork = true,
            };

            httpHandler.Setup(HttpMethod.Head, remoteUri, responseMessage);
            context.HttpClientFactory.MessageHandler = httpHandler;

            IHostProvider result = await registry.GetProviderAsync(input);

            httpHandler.AssertRequest(HttpMethod.Head, remoteUri, 1);
            Assert.Same(lowProviderMock.Object, result);
        }

        public static InputArguments CreateInputArguments(Uri uri)
        {
            var dict = new Dictionary<string, string>
            {
                ["protocol"] = uri.Scheme,
                ["host"] = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}"
            };

            if (!string.IsNullOrWhiteSpace(uri.AbsolutePath) && uri.AbsolutePath != "/")
            {
                dict["path"] = uri.AbsolutePath.TrimEnd('/');
            }

            return new InputArguments(dict);
        }
    }
}
