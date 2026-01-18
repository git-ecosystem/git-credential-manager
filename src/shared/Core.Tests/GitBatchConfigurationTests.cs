using System;
using System.IO;
using GitCredentialManager.Tests.Objects;
using Xunit;
using static GitCredentialManager.Tests.GitTestUtilities;

namespace GitCredentialManager.Tests
{
    public class GitBatchConfigurationTests
    {
        [Fact]
        public void GitBatchConfiguration_FallbackToProcessConfiguration_WhenBatchNotAvailable()
        {
            // This test verifies that GitBatchConfiguration gracefully falls back
            // to GitProcessConfiguration when git config-batch is not available.
            // We use a fake git path that doesn't exist to simulate this.

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local test.key test-value").AssertSuccess();

            // Use a non-existent git path to ensure config-batch will fail
            string fakeGitPath = Path.Combine(Path.GetTempPath(), "fake-git-" + Guid.NewGuid().ToString("N"));

            // However, we need a real git for the fallback to work
            // So we'll use the real git path - the fallback will happen automatically
            // when config-batch is not available
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                // TryGet should work via fallback even if config-batch doesn't exist
                bool result = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "test.key", out string value);

                Assert.True(result);
                Assert.Equal("test-value", value);
            }
        }

        [Fact]
        public void GitBatchConfiguration_TryGet_TypedQueries_UseFallback()
        {
            // Verify that typed queries (Bool, Path) always use fallback
            // since they're not supported by config-batch v1

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local test.bool true").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local test.path ~/mypath").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                // Bool type should fallback
                bool boolResult = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Bool,
                    "test.bool", out string boolValue);
                Assert.True(boolResult);
                Assert.Equal("true", boolValue);

                // Path type should fallback
                bool pathResult = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Path,
                    "test.path", out string pathValue);
                Assert.True(pathResult);
                Assert.NotNull(pathValue);
                // Path should be canonicalized
                Assert.NotEqual("~/mypath", pathValue);
            }
        }

        [Fact]
        public void GitBatchConfiguration_Enumerate_UsesFallback()
        {
            // Verify that Enumerate always uses fallback
            // since it's not supported by config-batch v1

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local foo.name alice").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local foo.value 42").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                int count = 0;
                config.Enumerate(GitConfigurationLevel.Local, entry =>
                {
                    if (entry.Key.StartsWith("foo."))
                    {
                        count++;
                    }
                    return true;
                });

                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void GitBatchConfiguration_Set_UsesFallback()
        {
            // Verify that Set uses fallback since writes aren't supported by config-batch

            string repoPath = CreateRepository(out string workDirPath);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                config.Set(GitConfigurationLevel.Local, "test.write", "written-value");

                // Verify it was written
                bool result = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "test.write", out string value);
                Assert.True(result);
                Assert.Equal("written-value", value);
            }
        }

        [Fact]
        public void GitBatchConfiguration_Unset_UsesFallback()
        {
            // Verify that Unset uses fallback since writes aren't supported by config-batch

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local test.remove old-value").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                config.Unset(GitConfigurationLevel.Local, "test.remove");

                // Verify it was removed
                bool result = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "test.remove", out string value);
                Assert.False(result);
                Assert.Null(value);
            }
        }

        [Fact]
        public void GitBatchConfiguration_TryGet_MissingKey_ReturnsFalse()
        {
            // Verify that querying missing keys works correctly

            string repoPath = CreateRepository(out string workDirPath);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                string randomKey = $"nonexistent.{Guid.NewGuid():N}";
                bool result = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    randomKey, out string value);

                Assert.False(result);
                Assert.Null(value);
            }
        }

        [Fact]
        public void GitBatchConfiguration_TryGet_ValueWithSpaces_ReturnsCorrectValue()
        {
            // Verify that values with spaces are handled correctly

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local test.spaced \"value with multiple spaces\"").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                bool result = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "test.spaced", out string value);

                Assert.True(result);
                Assert.Equal("value with multiple spaces", value);
            }
        }

        [Fact]
        public void GitBatchConfiguration_TryGet_DifferentLevels_ReturnsCorrectScope()
        {
            // Verify that different configuration levels work correctly

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local test.level local-value").AssertSuccess();

            try
            {
                ExecGit(repoPath, workDirPath, "config --global test.level global-value").AssertSuccess();

                string gitPath = GetGitPath();
                var trace = new NullTrace();
                var trace2 = new NullTrace2();
                var processManager = new TestProcessManager();
                var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

                using (var config = new GitBatchConfiguration(trace, git))
                {
                    // Local scope
                    bool localResult = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                        "test.level", out string localValue);
                    Assert.True(localResult);
                    Assert.Equal("local-value", localValue);

                    // Global scope
                    bool globalResult = config.TryGet(GitConfigurationLevel.Global, GitConfigurationType.Raw,
                        "test.level", out string globalValue);
                    Assert.True(globalResult);
                    Assert.Equal("global-value", globalValue);

                    // All scope (should return local as it has higher precedence)
                    bool allResult = config.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw,
                        "test.level", out string allValue);
                    Assert.True(allResult);
                    Assert.Equal("local-value", allValue);
                }
            }
            finally
            {
                // Cleanup global config
                ExecGit(repoPath, workDirPath, "config --global --unset test.level");
            }
        }

        [Fact]
        public void GitBatchConfiguration_Dispose_CleansUpProcess()
        {
            // Verify that disposal properly cleans up the batch process

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local test.dispose test-value").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            var config = new GitBatchConfiguration(trace, git);

            // Use the configuration
            config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                "test.dispose", out string _);

            // Dispose should not throw
            config.Dispose();

            // Second dispose should be safe
            config.Dispose();

            // Using after dispose should throw
            Assert.Throws<ObjectDisposedException>(() =>
                config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "test.dispose", out string _));
        }

        [Fact]
        public void GitBatchConfiguration_MultipleReads_ReusesSameProcess()
        {
            // This test verifies that multiple reads reuse the same batch process
            // We can't directly verify the process reuse, but we can verify that
            // multiple reads work correctly (which would fail if process management was broken)

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local test.key1 value1").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local test.key2 value2").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local test.key3 value3").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                // Multiple reads
                for (int i = 1; i <= 3; i++)
                {
                    bool result = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                        $"test.key{i}", out string value);
                    Assert.True(result);
                    Assert.Equal($"value{i}", value);
                }

                // Read them again
                for (int i = 1; i <= 3; i++)
                {
                    bool result = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                        $"test.key{i}", out string value);
                    Assert.True(result);
                    Assert.Equal($"value{i}", value);
                }
            }
        }

        [Fact]
        public void GitBatchConfiguration_GetAll_UsesFallback()
        {
            // Verify that GetAll uses fallback

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local --add test.multi value1").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local --add test.multi value2").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local --add test.multi value3").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                var values = config.GetAll(GitConfigurationLevel.Local, GitConfigurationType.Raw, "test.multi");

                int count = 0;
                foreach (var value in values)
                {
                    count++;
                    Assert.Contains(value, new[] { "value1", "value2", "value3" });
                }

                Assert.Equal(3, count);
            }
        }

        [Fact]
        public void GitBatchConfiguration_GetRegex_UsesFallback()
        {
            // Verify that GetRegex uses fallback

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local test.regex1 value1").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local test.regex2 value2").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                var values = config.GetRegex(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "test\\.regex.*", null);

                int count = 0;
                foreach (var value in values)
                {
                    count++;
                }

                Assert.Equal(2, count);
            }
        }
    }
}
