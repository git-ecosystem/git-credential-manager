using System.Collections.Generic;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class ApplicationTests
    {
        [Fact]
        public async Task Application_ConfigureAsync_NoHelpers_AddsEmptyAndGcm()
        {
            const string emptyHelper = "";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);
            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(executablePath, actualValues[1]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_Gcm_AddsEmptyBeforeGcm()
        {
            const string emptyHelper = "";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string> {executablePath};

            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(executablePath, actualValues[1]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_EmptyAndGcm_DoesNothing()
        {
            const string emptyHelper = "";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string>
            {
                emptyHelper, executablePath
            };

            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(executablePath, actualValues[1]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_EmptyAndGcmWithOthersBefore_DoesNothing()
        {
            const string emptyHelper = "";
            const string beforeHelper = "foo";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string>
            {
                beforeHelper, emptyHelper, executablePath
            };

            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(3, actualValues.Count);
            Assert.Equal(beforeHelper, actualValues[0]);
            Assert.Equal(emptyHelper, actualValues[1]);
            Assert.Equal(executablePath, actualValues[2]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_EmptyAndGcmWithOthersAfter_DoesNothing()
        {
            const string emptyHelper = "";
            const string afterHelper = "foo";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string>
            {
                emptyHelper, executablePath, afterHelper
            };

            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(3, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(executablePath, actualValues[1]);
            Assert.Equal(afterHelper, actualValues[2]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_EmptyAndGcmWithOthersBeforeAndAfter_DoesNothing()
        {
            const string emptyHelper = "";
            const string beforeHelper = "foo";
            const string afterHelper = "bar";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string>
            {
                beforeHelper, emptyHelper, executablePath, afterHelper
            };

            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(4, actualValues.Count);
            Assert.Equal(beforeHelper, actualValues[0]);
            Assert.Equal(emptyHelper, actualValues[1]);
            Assert.Equal(executablePath, actualValues[2]);
            Assert.Equal(afterHelper, actualValues[3]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_EmptyAndGcmWithEmptyAfter_RemovesExistingGcmAndAddsEmptyAndGcm()
        {
            const string emptyHelper = "";
            const string afterHelper = "foo";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string>
            {
                emptyHelper, executablePath, emptyHelper, afterHelper
            };

            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(5, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(emptyHelper, actualValues[1]);
            Assert.Equal(afterHelper, actualValues[2]);
            Assert.Equal(emptyHelper, actualValues[3]);
            Assert.Equal(executablePath, actualValues[4]);
        }

        [Fact]
        public async Task Application_UnconfigureAsync_NoHelpers_DoesNothing()
        {
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);
            await application.UnconfigureAsync(ConfigurationTarget.User);

            Assert.Empty(context.Git.Configuration.Global);
        }

        [Fact]
        public async Task Application_UnconfigureAsync_Gcm_RemovesGcm()
        {
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string> {executablePath};

            await application.UnconfigureAsync(ConfigurationTarget.User);

            Assert.Empty(context.Git.Configuration.Global);
        }

        [Fact]
        public async Task Application_UnconfigureAsync_EmptyAndGcm_RemovesEmptyAndGcm()
        {
            const string emptyHelper = "";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string> {emptyHelper, executablePath};

            await application.UnconfigureAsync(ConfigurationTarget.User);

            Assert.Empty(context.Git.Configuration.Global);
        }

        [Fact]
        public async Task Application_UnconfigureAsync_EmptyAndGcmWithOthersBefore_RemovesEmptyAndGcm()
        {
            const string emptyHelper = "";
            const string beforeHelper = "foo";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string>
            {
                beforeHelper, emptyHelper, executablePath
            };

            await application.UnconfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(1, actualValues.Count);
            Assert.Equal(beforeHelper, actualValues[0]);
        }

        [Fact]
        public async Task Application_UnconfigureAsync_EmptyAndGcmWithOthersAfterBefore_RemovesGcmOnly()
        {
            const string emptyHelper = "";
            const string afterHelper = "bar";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string>
            {
                emptyHelper, executablePath, afterHelper
            };

            await application.UnconfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(afterHelper, actualValues[1]);
        }

        [Fact]
        public async Task Application_UnconfigureAsync_EmptyAndGcmWithOthersBeforeAndAfter_RemovesGcmOnly()
        {
            const string emptyHelper = "";
            const string beforeHelper = "foo";
            const string afterHelper = "bar";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext {AppPath = executablePath};
            IConfigurableComponent application = new Application(context);

            context.Git.Configuration.Global[key] = new List<string>
            {
                beforeHelper, emptyHelper, executablePath, afterHelper
            };

            await application.UnconfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.Configuration.Global);
            Assert.True(context.Git.Configuration.Global.TryGetValue(key, out var actualValues));
            Assert.Equal(3, actualValues.Count);
            Assert.Equal(beforeHelper, actualValues[0]);
            Assert.Equal(emptyHelper, actualValues[1]);
            Assert.Equal(afterHelper, actualValues[2]);
        }
    }
}
