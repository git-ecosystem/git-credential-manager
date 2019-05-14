// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Commands
{
    public class GetCommandTests
    {
        [Theory]
        [InlineData("get", true)]
        [InlineData("GET", true)]
        [InlineData("gEt", true)]
        [InlineData("erase", false)]
        [InlineData("store", false)]
        [InlineData("foobar", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void GetCommand_CanExecuteAsync(string argString, bool expected)
        {
            var command = new GetCommand(Mock.Of<IHostProviderRegistry>());

            bool result = command.CanExecute(argString?.Split(null));

            if (expected)
            {
                Assert.True(result);
            }
            else
            {
                Assert.False(result);
            }
        }

        [Fact]
        public async Task GetCommand_ExecuteAsync_CallsHostProviderAndWritesCredential()
        {
            const string testUserName = "john.doe";
            const string testPassword = "letmein123";
            ICredential testCredential = new GitCredential(testUserName, testPassword);
            var expectedStdOutDict = new Dictionary<string, string>
            {
                ["username"] = testUserName,
                ["password"] = testPassword
            };

            var providerMock = new Mock<IHostProvider>();
            providerMock.Setup(x => x.GetCredentialAsync(It.IsAny<InputArguments>()))
                        .ReturnsAsync(testCredential);
            var providerRegistry = new TestHostProviderRegistry {Provider = providerMock.Object};
            var context = new TestCommandContext();

            string[] cmdArgs = {"get"};
            var command = new GetCommand(providerRegistry);

            await command.ExecuteAsync(context, cmdArgs);

            IDictionary<string, string> actualStdOutDict = ParseDictionary(context.StdOut);

            providerMock.Verify(x => x.GetCredentialAsync(It.IsAny<InputArguments>()), Times.Once);
            Assert.Equal(expectedStdOutDict, actualStdOutDict);
        }

        #region Helpers

        private static IDictionary<string, string> ParseDictionary(StringBuilder sb) => ParseDictionary(sb.ToString());

        private static IDictionary<string, string> ParseDictionary(string str) => new StringReader(str).ReadDictionary();

        #endregion
    }
}
