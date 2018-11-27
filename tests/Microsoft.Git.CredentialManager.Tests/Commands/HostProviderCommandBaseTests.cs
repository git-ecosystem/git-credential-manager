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
        public async Task HostProviderCommandBase_ExecuteAsync_CallsExecuteInternalAyncWithCorrectArgs()
        {
            const string testProviderCredKey = "test-cred-key";
            const string testFinalCredKey = "git:test-cred-key";

            var mockContext = new Mock<ICommandContext>();
            var mockProvider = new Mock<IHostProvider>();
            var mockHostRegistry = new Mock<IHostProviderRegistry>();

            mockHostRegistry.Setup(x => x.GetProvider(It.IsAny<InputArguments>()))
                .Returns(mockProvider.Object)
                .Verifiable();

            mockProvider.Setup(x => x.IsSupported(It.IsAny<InputArguments>()))
                .Returns(true);
            mockProvider.Setup(x => x.GetCredentialKey(It.IsAny<InputArguments>()))
                .Returns(testProviderCredKey);

            string standardIn = "protocol=test\nhost=example.com\npath=a/b/c\n\n";
            TextReader standardInReader = new StringReader(standardIn);

            mockContext.Setup(x => x.StdIn).Returns(standardInReader);
            mockContext.Setup(x => x.Trace).Returns(new Mock<ITrace>().Object);

            HostProviderCommandBase testCommand = new TestCommand(mockHostRegistry.Object)
            {
                VerifyExecuteInternalAsync = (context, input, provider, credentialKey) =>
                {
                    Assert.Same(mockContext.Object, context);
                    Assert.Same(mockProvider.Object, provider);
                    Assert.Equal(testFinalCredKey, credentialKey);
                    Assert.Equal("test", input.Protocol);
                    Assert.Equal("example.com", input.Host);
                    Assert.Equal("a/b/c", input.Path);
                }
            };

            await testCommand.ExecuteAsync(mockContext.Object, new string[0]);
        }

        private class TestCommand : HostProviderCommandBase
        {
            public TestCommand(IHostProviderRegistry hostProviderRegistry)
                : base(hostProviderRegistry)
            {
            }

            protected override string Name { get; }

            protected override Task ExecuteInternalAsync(ICommandContext context, InputArguments input,
                IHostProvider provider, string credentialKey)
            {
                VerifyExecuteInternalAsync(context, input, provider, credentialKey);
                return Task.CompletedTask;
            }

            public Action<ICommandContext, InputArguments, IHostProvider, string> VerifyExecuteInternalAsync { get; set; }
        }
    }
}
