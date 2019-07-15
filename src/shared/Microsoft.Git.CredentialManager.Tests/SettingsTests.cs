// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void Settings_IsDebuggingEnabled_EnvarUnset_ReturnsFalse()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>());
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsDebuggingEnabled);
        }

        [Fact]
        public void Settings_IsDebuggingEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [Constants.EnvironmentVariables.GcmDebug] = "1"
            });
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsDebuggingEnabled);
        }

        [Fact]
        public void Settings_IsDebuggingEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [Constants.EnvironmentVariables.GcmDebug] = "0"
            });
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsDebuggingEnabled);
        }

        [Fact]
        public void Settings_IsTerminalPromptsEnabled_EnvarUnset_ReturnsTrue()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>());
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsTerminalPromptsEnabled);
        }

        [Fact]
        public void Settings_IsTerminalPromptsEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [Constants.EnvironmentVariables.GitTerminalPrompts] = "1"
            });
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsTerminalPromptsEnabled);
        }

        [Fact]
        public void Settings_IsTerminalPromptsEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [Constants.EnvironmentVariables.GitTerminalPrompts] = "0"
            });
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsTerminalPromptsEnabled);
        }

        [Fact]
        public void Settings_IsTracingEnabled_EnvarUnset_ReturnsFalse()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>());
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.False(result);
        }

        [Fact]
        public void Settings_IsTracingEnabled_EnvarTruthy_ReturnsTrueOutValue()
        {
            const string expectedValue = "1";
            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [Constants.EnvironmentVariables.GcmTrace] = expectedValue
            });
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_IsTracingEnabled_EnvarFalsey_ReturnsFalseOutValue()
        {
            const string expectedValue = "0";
            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [Constants.EnvironmentVariables.GcmTrace] = expectedValue
            });
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.False(result);
            Assert.Equal(expectedValue, actualValue);
        }


        [Fact]
        public void Settings_IsTracingEnabled_EnvarPathy_ReturnsTrueOutValue()
        {
            const string expectedValue = "/tmp/gcm.log";
            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [Constants.EnvironmentVariables.GcmTrace] = expectedValue
            });
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_IsSecretTracingEnabled_EnvarUnset_ReturnsFalse()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>());
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsSecretTracingEnabled);
        }

        [Fact]
        public void Settings_IsSecretTracingEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [Constants.EnvironmentVariables.GcmTraceSecrets] = "1"
            });
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsSecretTracingEnabled);
        }

        [Fact]
        public void Settings_IsSecretTracingEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [Constants.EnvironmentVariables.GcmTraceSecrets] = "0"
            });
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsSecretTracingEnabled);
        }

        [Fact]
        public void Settings_TryGetSetting_EnvarSet_ReturnsTrueOutValue()
        {
            const string repositoryPath = "/tmp/repos/foo/.git";
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";

            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [envarName] = expectedValue,
            });
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RepositoryPath = repositoryPath,
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_EnvarUnset_ReturnsFalse()
        {
            const string repositoryPath = "/tmp/repos/foo/.git";
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            var envars = new EnvironmentVariables(new Dictionary<string, string>());
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RepositoryPath = repositoryPath,
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.False(result);
            Assert.Null(actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_GlobalConfig_ReturnsTrueAndValue()
        {
            const string repositoryPath = "/tmp/repos/foo/.git";
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";

            var envars = new EnvironmentVariables(new Dictionary<string, string>());
            var git = new TestGit(new Dictionary<string, string>
            {
                [$"{section}.{property}"] = expectedValue
            });

            var settings = new Settings(envars, git)
            {
                RepositoryPath = repositoryPath,
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting( envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_RepoConfig_ReturnsTrueAndValue()
        {
            const string repositoryPath = "/tmp/repos/foo/.git";
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";

            var envars = new EnvironmentVariables(new Dictionary<string, string>());
            var git = new TestGit();
            git.AddRepository(repositoryPath, new Dictionary<string, string>
            {
                [$"{section}.{property}"] = expectedValue
            });

            var settings = new Settings(envars, git)
            {
                RepositoryPath = repositoryPath,
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_ScopedConfig()
        {
            const string repositoryPath = "/tmp/repos/foo/.git";
            const string remoteUrl = "http://example.com/foo/bar/bazz.git";
            const string scope1 = "example.com";
            const string scope2 = "example.com/foo/bar";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";
            const string otherValue = "Goodbye, World!";

            var envars = new EnvironmentVariables(new Dictionary<string, string>());
            var git = new TestGit();
            git.AddRepository(repositoryPath, new Dictionary<string, string>
            {
                [$"{section}.{scope1}.{property}"] = otherValue,
                [$"{section}.{scope2}.{property}"] = expectedValue,
            });

            var settings = new Settings(envars, git)
            {
                RepositoryPath = repositoryPath,
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_EnvarAndConfig_EnvarTakesPrecedence()
        {
            const string repositoryPath = "/tmp/repos/foo/.git";
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";
            const string otherValue = "Goodbye, World!";

            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [envarName] = expectedValue,
            });
            var git = new TestGit();
            git.AddRepository(repositoryPath, new Dictionary<string, string>
            {
                [$"{section}.{property}"] = otherValue
            });

            var settings = new Settings(envars, git)
            {
                RepositoryPath = repositoryPath,
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_GetSettingValues_EnvarAndMultipleConfig_ReturnsAllWithCorrectPrecedence()
        {
            const string repositoryPath = "/tmp/repos/foo/.git";
            const string remoteUrl = "http://example.com/foo.git";
            const string scope1 = "http://example.com";
            const string scope2 = "example.com";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string value1 = "First value";
            const string value2 = "Second value";
            const string value3 = "Third value";
            const string value4 = "Last value";

            string[] expectedValues = {value1, value2, value3, value4};

            var envars = new EnvironmentVariables(new Dictionary<string, string>
            {
                [envarName] = value1,
            });
            var git = new TestGit();
            git.AddRepository(repositoryPath, new Dictionary<string, string>
            {
                [$"{section}.{scope1}.{property}"] = value2,
                [$"{section}.{scope2}.{property}"] = value3,
                [$"{section}.{property}"]          = value4
            });

            var settings = new Settings(envars, git)
            {
                RepositoryPath = repositoryPath,
                RemoteUri = remoteUri
            };
            string[] actualValues = settings.GetSettingValues(envarName, section, property).ToArray();

            Assert.Equal(expectedValues, actualValues);
        }
    }
}
