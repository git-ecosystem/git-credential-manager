using System;
using System.IO;
using System.Threading.Tasks;
using GitCredentialManager.Commands;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests.Commands
{
    public class GitCommandBaseTests
    {
        [Fact]
        public async Task GitCommandBase_ExecuteAsync_CallsExecuteInternalAsyncWithCorrectArgs()
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

            GitCommandBase testCommand = new TestCommand(mockContext.Object, mockHostRegistry.Object)
            {
                VerifyExecuteInternalAsync = (input, provider) =>
                {
                    Assert.Same(mockProvider.Object, provider);
                    Assert.Equal("test", input.Protocol);
                    Assert.Equal("example.com", input.Host);
                    Assert.Equal("a/b/c", input.Path);
                }
            };

            await testCommand.ExecuteAsync();
        }

        [Fact]
        public async Task GitCommandBase_ExecuteAsync_ConfiguresSettingsRemoteUri()
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

            GitCommandBase testCommand = new TestCommand(mockContext.Object, mockHostRegistry.Object);

            await testCommand.ExecuteAsync();

            Assert.Equal(remoteUri, mockSettings.Object.RemoteUri);
        }

        private class TestCommand : GitCommandBase
        {
            public TestCommand(ICommandContext context, IHostProviderRegistry hostProviderRegistry)
                : base(context, "test", null, hostProviderRegistry)
            {
            }

            protected override Task ExecuteInternalAsync(InputArguments input, IHostProvider provider)
            {
                VerifyExecuteInternalAsync?.Invoke(input, provider);
                return Task.CompletedTask;
            }

            public Action<InputArguments, IHostProvider> VerifyExecuteInternalAsync { get; set; }
        }
    }
}
