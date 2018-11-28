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

            var provider1 = new Mock<IHostProvider>();
            var provider2 = new Mock<IHostProvider>();
            var provider3 = new Mock<IHostProvider>();

            provider1.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);
            provider2.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(false);

            registry.Register(provider1.Object, provider2.Object, provider3.Object);

            IHostProvider result = registry.GetProvider(input);

            Assert.Same(provider2.Object, result);
        }

        [Fact]
        public void HostProviderRegistry_MultipleValidProviders_ReturnsFirstRegistered()
        {
            var registry = new HostProviderRegistry();
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1 = new Mock<IHostProvider>();
            var provider2 = new Mock<IHostProvider>();
            var provider3 = new Mock<IHostProvider>();

            provider1.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider2.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);
            provider3.Setup(x => x.IsSupported(It.IsAny<InputArguments>())).Returns(true);

            registry.Register(provider1.Object, provider2.Object, provider3.Object);

            IHostProvider result = registry.GetProvider(input);

            Assert.Same(provider1.Object, result);
        }
    }
}
