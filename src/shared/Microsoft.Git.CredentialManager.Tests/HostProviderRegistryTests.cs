// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class HostProviderRegistryTests
    {
        [Fact]
        public void HostProviderRegistry_NoProviders_ThrowException()
        {
            var registry = new HostProviderRegistry();
            var input = new InputArguments(new Dictionary<string, string>());

            Assert.Throws<Exception>(() => registry.GetProvider(input));
        }

        [Fact]
        public void HostProviderRegistry_HasProviders_ReturnsSupportedProvider()
        {
            var registry = new HostProviderRegistry();
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1Mock.Object, provider2Mock.Object, provider3Mock.Object);

            IHostProvider result = registry.GetProvider(input);

            Assert.Same(provider2Mock.Object, result);
        }

        [Fact]
        public void HostProviderRegistry_MultipleValidProviders_ReturnsFirstRegistered()
        {
            var registry = new HostProviderRegistry();
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1Mock = new Mock<IHostProvider>();
            var provider2Mock = new Mock<IHostProvider>();
            var provider3Mock = new Mock<IHostProvider>();
            provider1Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider2Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3Mock.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);

            registry.Register(provider1Mock.Object, provider2Mock.Object, provider3Mock.Object);

            IHostProvider result = registry.GetProvider(input);

            Assert.Same(provider1Mock.Object, result);
        }
    }
}
