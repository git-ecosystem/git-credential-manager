// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using Microsoft.Git.CredentialManager.Tests.Objects;
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

            var provider1 = new TestHostProvider {IsSupported = false};
            var provider2 = new TestHostProvider {IsSupported = true};
            var provider3 = new TestHostProvider {IsSupported = false};

            registry.Register(provider1, provider2, provider3);

            IHostProvider result = registry.GetProvider(input);

            Assert.Same(provider2, result);
        }

        [Fact]
        public void HostProviderRegistry_MultipleValidProviders_ReturnsFirstRegistered()
        {
            var registry = new HostProviderRegistry();
            var input = new InputArguments(new Dictionary<string, string>());

            var provider1 = new TestHostProvider {IsSupported = true};
            var provider2 = new TestHostProvider {IsSupported = true};
            var provider3 = new TestHostProvider {IsSupported = true};

            registry.Register(provider1, provider2, provider3);

            IHostProvider result = registry.GetProvider(input);

            Assert.Same(provider1, result);
        }
    }
}
