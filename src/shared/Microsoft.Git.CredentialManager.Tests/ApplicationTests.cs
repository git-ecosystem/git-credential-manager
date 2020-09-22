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
    public class ApplicationTests
    {
        #region Common configuration tests

        [Fact]
        public async Task Application_ConfigureAsync_HelperSet_DoesNothing()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            IConfigurableComponent application = new Application(new TestCommandContext(), executablePath);

            var environment = new Mock<IEnvironment>();

            var git = new TestGit();
            git.GlobalConfiguration.Dictionary[key] = new List<string>
            {
                emptyHelper, gcmConfigName
            };

            await application.ConfigureAsync(
                environment.Object, EnvironmentVariableTarget.User,
                git, GitConfigurationLevel.Global);

            Assert.Single(git.GlobalConfiguration.Dictionary);
            Assert.True(git.GlobalConfiguration.Dictionary.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(gcmConfigName, actualValues[1]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_HelperSetWithOthersPreceding_DoesNothing()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            IConfigurableComponent application = new Application(new TestCommandContext(), executablePath);

            var environment = new Mock<IEnvironment>();

            var git = new TestGit();
            git.GlobalConfiguration.Dictionary[key] = new List<string>
            {
                "foo", "bar", emptyHelper, gcmConfigName
            };

            await application.ConfigureAsync(
                environment.Object, EnvironmentVariableTarget.User,
                git, GitConfigurationLevel.Global);

            Assert.Single(git.GlobalConfiguration.Dictionary);
            Assert.True(git.GlobalConfiguration.Dictionary.TryGetValue(key, out var actualValues));
            Assert.Equal(4, actualValues.Count);
            Assert.Equal("foo", actualValues[0]);
            Assert.Equal("bar", actualValues[1]);
            Assert.Equal(emptyHelper, actualValues[2]);
            Assert.Equal(gcmConfigName, actualValues[3]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_HelperSetWithOthersFollowing_ClearsEntriesSetsHelper()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            IConfigurableComponent application = new Application(new TestCommandContext(), executablePath);

            var environment = new Mock<IEnvironment>();

            var git = new TestGit();
            git.GlobalConfiguration.Dictionary[key] = new List<string>
            {
                "bar", emptyHelper, executablePath, "foo"
            };

            await application.ConfigureAsync(
                environment.Object, EnvironmentVariableTarget.User,
                git, GitConfigurationLevel.Global);

            Assert.Single(git.GlobalConfiguration.Dictionary);
            Assert.True(git.GlobalConfiguration.Dictionary.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(gcmConfigName, actualValues[1]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_HelperNotSet_SetsHelper()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            IConfigurableComponent application = new Application(new TestCommandContext(), executablePath);

            var environment = new Mock<IEnvironment>();

            var git = new TestGit();

            await application.ConfigureAsync(
                environment.Object, EnvironmentVariableTarget.User,
                git, GitConfigurationLevel.Global);

            Assert.Single(git.GlobalConfiguration.Dictionary);
            Assert.True(git.GlobalConfiguration.Dictionary.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(gcmConfigName, actualValues[1]);
        }

        [Fact]
        public async Task Application_UnconfigureAsync_HelperSet_RemovesEntries()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            IConfigurableComponent application = new Application(new TestCommandContext(), executablePath);

            var environment = new Mock<IEnvironment>();
            var git = new TestGit();
            git.GlobalConfiguration.Dictionary[key] = new List<string> {emptyHelper, gcmConfigName};

            await application.UnconfigureAsync(
                environment.Object, EnvironmentVariableTarget.User,
                git, GitConfigurationLevel.Global);

            Assert.Empty(git.GlobalConfiguration.Dictionary);
        }

        #endregion

        #region Windows-specific configuration tests

        [PlatformFact(Platforms.Windows)]
        public async Task Application_ConfigureAsync_User_PathSet_DoesNothing()
        {
            const string directoryPath = @"X:\Install Location";
            const string executablePath = @"X:\Install Location\git-credential-manager-core.exe";

            IConfigurableComponent application = new Application(new TestCommandContext(), executablePath);

            var environment = new Mock<IEnvironment>();
            environment.Setup(x => x.IsDirectoryOnPath(directoryPath)).Returns(true);

            var git = new TestGit();

            await application.ConfigureAsync(
                environment.Object, EnvironmentVariableTarget.User,
                git, GitConfigurationLevel.Global);

            environment.Verify(x => x.AddDirectoryToPath(It.IsAny<string>(), It.IsAny<EnvironmentVariableTarget>()), Times.Never);
        }

        [PlatformFact(Platforms.Windows)]
        public async Task Application_ConfigureAsync_User_PathNotSet_SetsUserPath()
        {
            const string directoryPath = @"X:\Install Location";
            const string executablePath = @"X:\Install Location\git-credential-manager-core.exe";

            IConfigurableComponent application = new Application(new TestCommandContext(), executablePath);

            var environment = new Mock<IEnvironment>();
            environment.Setup(x => x.IsDirectoryOnPath(directoryPath)).Returns(false);

            var git = new TestGit();

            await application.ConfigureAsync(
                environment.Object, EnvironmentVariableTarget.User,
                git, GitConfigurationLevel.Global);

            environment.Verify(x => x.AddDirectoryToPath(directoryPath, EnvironmentVariableTarget.User), Times.Once);
        }

        [PlatformFact(Platforms.Windows)]
        public async Task Application_UnconfigureAsync_User_PathSet_RemovesFromUserPath()
        {
            const string directoryPath = @"X:\Install Location";
            const string executablePath = @"X:\Install Location\git-credential-manager-core.exe";

            IConfigurableComponent application = new Application(new TestCommandContext(), executablePath);

            var environment = new Mock<IEnvironment>();
            environment.Setup(x => x.IsDirectoryOnPath(directoryPath)).Returns(true);

            var git = new TestGit();

            await application.UnconfigureAsync(
                environment.Object, EnvironmentVariableTarget.User,
                git, GitConfigurationLevel.Global);

            environment.Verify(x => x.RemoveDirectoryFromPath(directoryPath, EnvironmentVariableTarget.User), Times.Once);
        }

        #endregion
    }
}
