// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Commands
{
    public class HostProviderCommandBaseTests
    {
        [Fact]
        public async Task HostProviderCommandBase_ExecuteAsync_CallsExecuteInternalAsyncWithCorrectArgs()
        {
            var mockContext = new Mock<ICommandContext>();
            var mockStreams = new Mock<IStandardStreams>();
            var mockProvider = new Mock<IHostProvider>();
            var mockHostRegistry = new Mock<IHostProviderRegistry>();

            mockHostRegistry.Setup(x => x.GetProviderAsync(It.IsAny<InputArguments>()))
                .ReturnsAsync(mockProvider.Object)
                .Verifiable();

            mockProvider.Setup(x => x.IsSupported(It.IsAny<InputArguments>()))
                .Returns(true);

            string standardIn = "protocol=test\nhost=example.com\npath=a/b/c\n\n";
            TextReader standardInReader = new StringReader(standardIn);

            mockStreams.Setup(x => x.In).Returns(standardInReader);
            mockContext.Setup(x => x.Streams).Returns(mockStreams.Object);
            mockContext.Setup(x => x.Trace).Returns(Mock.Of<ITrace>());
            mockContext.Setup(x => x.Settings).Returns(Mock.Of<ISettings>());

            HostProviderCommandBase testCommand = new TestCommand(mockHostRegistry.Object)
            {
                VerifyExecuteInternalAsync = (context, input, provider) =>
                {
                    Assert.Same(mockContext.Object, context);
                    Assert.Same(mockProvider.Object, provider);
                    Assert.Equal("test", input.Protocol);
                    Assert.Equal("example.com", input.Host);
                    Assert.Equal("a/b/c", input.Path);
                }
            };

            await testCommand.ExecuteAsync(mockContext.Object, new string[0]);
        }

        [Fact]
        public async Task HostProviderCommandBase_ExecuteAsync_ConfiguresSettingsRemoteUri()
        {
            var mockContext = new Mock<ICommandContext>();
            var mockStreams = new Mock<IStandardStreams>();
            var mockProvider = new Mock<IHostProvider>();
            var mockSettings = new Mock<ISettings>();
            var mockHostRegistry = new Mock<IHostProviderRegistry>();

            mockHostRegistry.Setup(x => x.GetProviderAsync(It.IsAny<InputArguments>()))
                .ReturnsAsync(mockProvider.Object);

            string standardIn = "protocol=test\nhost=example.com\npath=a/b/c\n\n";
            TextReader standardInReader = new StringReader(standardIn);

            var remoteUri = new Uri("test://example.com/a/b/c");

            mockSettings.SetupProperty(x => x.RemoteUri);

            mockStreams.Setup(x => x.In).Returns(standardInReader);
            mockContext.Setup(x => x.Streams).Returns(mockStreams.Object);
            mockContext.Setup(x => x.Trace).Returns(Mock.Of<ITrace>());
            mockContext.Setup(x => x.Settings).Returns(mockSettings.Object);

            HostProviderCommandBase testCommand = new TestCommand(mockHostRegistry.Object);

            await testCommand.ExecuteAsync(mockContext.Object, new string[0]);

            Assert.Equal(remoteUri, mockSettings.Object.RemoteUri);
        }

        private class TestCommand : HostProviderCommandBase
        {
            public TestCommand(IHostProviderRegistry hostProviderRegistry)
                : base(hostProviderRegistry)
            {
            }

            protected override string Name { get; }

            protected override Task ExecuteInternalAsync(ICommandContext context, InputArguments input, IHostProvider provider)
            {
                VerifyExecuteInternalAsync?.Invoke(context, input, provider);
                return Task.CompletedTask;
            }

            public Action<ICommandContext, InputArguments, IHostProvider> VerifyExecuteInternalAsync { get; set; }
        }
    }
}
