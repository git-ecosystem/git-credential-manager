// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
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

            Assert.Throws<ArgumentException>(() => registry.Register(provider.Object));
        }

        [Fact]
        public void HostProviderRegistry_Register_AutoAuthorityId_ThrowException()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var provider = new Mock<IHostProvider>();
            provider.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"foo", Constants.AuthorityIdAuto, "bar"});

            Assert.Throws<ArgumentException>(() => registry.Register(provider.Object));
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
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, provider2Mock.Object, provider3Mock.Object);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider2Mock.Object, result);
        }

        [Fact]
        public async Task HostProviderRegistry_GetProvider_Auto_MultipleValidProviders_ReturnsFirstRegistered()
        {
            var context = new TestCommandContext();
            var registry = new HostProviderRegistry(context);
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);

            registry.Register(provider1Mock.Object, provider2Mock.Object, provider3Mock.Object);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider1Mock.Object, result);
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

            registry.Register(provider1Mock.Object, provider2Mock.Object, provider3Mock.Object);

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
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.Id).Returns("provider1");
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.Id).Returns("provider2");
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.Id).Returns("provider3");
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, provider2Mock.Object, provider3Mock.Object);

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
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.Id).Returns("provider1");
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.Id).Returns("provider2");
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.Id).Returns("provider3");
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, provider2Mock.Object, provider3Mock.Object);

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

            registry.Register(provider1Mock.Object, provider2Mock.Object, provider3Mock.Object);

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
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"authorityA"});
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"authorityB", "authorityC"});
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.SupportedAuthorityIds).Returns(new[]{"authorityD"});
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, provider2Mock.Object, provider3Mock.Object);

            IHostProvider result = await registry.GetProviderAsync(input);

            Assert.Same(provider2Mock.Object, result);
        }
    }
}
