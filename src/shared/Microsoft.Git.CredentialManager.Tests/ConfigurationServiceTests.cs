// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class ConfigurationServiceTests
    {
        [Fact]
        public async Task ConfigurationService_ConfigureAsync_System_ComponentsAreConfiguredWithSystem()
        {
            var context = new TestCommandContext();
            var service = new ConfigurationService(context);

            var component1 = new Mock<IConfigurableComponent>();
            var component2 = new Mock<IConfigurableComponent>();
            var component3 = new Mock<IConfigurableComponent>();

            service.AddComponent(component1.Object);
            service.AddComponent(component2.Object);
            service.AddComponent(component3.Object);

            await service.ConfigureAsync(ConfigurationTarget.System);

            component1.Verify(x => x.ConfigureAsync(ConfigurationTarget.System),
                Times.Once);
            component2.Verify(x => x.ConfigureAsync(ConfigurationTarget.System),
                Times.Once);
            component3.Verify(x => x.ConfigureAsync(ConfigurationTarget.System),
                Times.Once);
        }

        [Fact]
        public async Task ConfigurationService_ConfigureAsync_User_ComponentsAreConfiguredWithUser()
        {
            var context = new TestCommandContext();
            var service = new ConfigurationService(context);

            var component1 = new Mock<IConfigurableComponent>();
            var component2 = new Mock<IConfigurableComponent>();
            var component3 = new Mock<IConfigurableComponent>();

            service.AddComponent(component1.Object);
            service.AddComponent(component2.Object);
            service.AddComponent(component3.Object);

            await service.ConfigureAsync(ConfigurationTarget.User);

            component1.Verify(x => x.ConfigureAsync(ConfigurationTarget.User),
                Times.Once);
            component2.Verify(x => x.ConfigureAsync(ConfigurationTarget.User),
                Times.Once);
            component3.Verify(x => x.ConfigureAsync(ConfigurationTarget.User),
                Times.Once);
        }

        [Fact]
        public async Task ConfigurationService_UnconfigureAsync_System_ComponentsAreUnconfiguredWithSystem()
        {
            var context = new TestCommandContext();
            var service = new ConfigurationService(context);

            var component1 = new Mock<IConfigurableComponent>();
            var component2 = new Mock<IConfigurableComponent>();
            var component3 = new Mock<IConfigurableComponent>();

            service.AddComponent(component1.Object);
            service.AddComponent(component2.Object);
            service.AddComponent(component3.Object);

            await service.UnconfigureAsync(ConfigurationTarget.System);

            component1.Verify(x => x.UnconfigureAsync(ConfigurationTarget.System),
                Times.Once);
            component2.Verify(x => x.UnconfigureAsync(ConfigurationTarget.System),
                Times.Once);
            component3.Verify(x => x.UnconfigureAsync(ConfigurationTarget.System),
                Times.Once);
        }

        [Fact]
        public async Task ConfigurationService_UnconfigureAsync_User_ComponentsAreUnconfiguredWithUser()
        {
            var context = new TestCommandContext();
            var service = new ConfigurationService(context);

            var component1 = new Mock<IConfigurableComponent>();
            var component2 = new Mock<IConfigurableComponent>();
            var component3 = new Mock<IConfigurableComponent>();

            service.AddComponent(component1.Object);
            service.AddComponent(component2.Object);
            service.AddComponent(component3.Object);

            await service.UnconfigureAsync(ConfigurationTarget.User);

            component1.Verify(x => x.UnconfigureAsync(ConfigurationTarget.User),
                Times.Once);
            component2.Verify(x => x.UnconfigureAsync(ConfigurationTarget.User),
                Times.Once);
            component3.Verify(x => x.UnconfigureAsync(ConfigurationTarget.User),
                Times.Once);
        }
    }
}
