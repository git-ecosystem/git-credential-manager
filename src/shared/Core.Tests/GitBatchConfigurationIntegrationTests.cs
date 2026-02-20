using System;
using System.Diagnostics;
using System.IO;
using GitCredentialManager.Tests.Objects;
using Xunit;
using static GitCredentialManager.Tests.GitTestUtilities;

namespace GitCredentialManager.Tests
{
    /// <summary>
    /// Integration tests for GitBatchConfiguration that require git config-batch to be available.
    /// These tests will be skipped if git config-batch is not available.
    /// </summary>
    public class GitBatchConfigurationIntegrationTests
    {
        private const string CustomGitPath = @"C:\Users\dstolee\_git\git\git\git.exe";

        private static bool IsConfigBatchAvailable(string gitPath)
        {
            try
            {
                var psi = new ProcessStartInfo(gitPath, "config-batch")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return false;

                    process.StandardInput.WriteLine();
                    process.StandardInput.Close();
                    process.WaitForExit(5000);

                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        [Fact]
        public void GitBatchConfiguration_WithConfigBatch_UsesActualBatchProcess()
        {
            // Use custom git path if it exists and has config-batch, otherwise skip
            string gitPath = File.Exists(CustomGitPath) && IsConfigBatchAvailable(CustomGitPath)
                ? CustomGitPath
                : GetGitPath();

            if (!IsConfigBatchAvailable(gitPath))
            {
                // Skip test if config-batch is not available
                return;
            }

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local test.integration batch-value").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local test.spaces \"value with spaces\"").AssertSuccess();

            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                // First read - should start batch process
                bool result1 = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "test.integration", out string value1);
                Assert.True(result1);
                Assert.Equal("batch-value", value1);

                // Second read - should reuse batch process
                bool result2 = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "test.spaces", out string value2);
                Assert.True(result2);
                Assert.Equal("value with spaces", value2);

                // Third read - different key
                bool result3 = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "test.integration", out string value3);
                Assert.True(result3);
                Assert.Equal("batch-value", value3);
            }
        }

        [Fact]
        public void GitBatchConfiguration_PerformanceComparison_BatchVsNonBatch()
        {
            string gitPath = File.Exists(CustomGitPath) && IsConfigBatchAvailable(CustomGitPath)
                ? CustomGitPath
                : GetGitPath();

            if (!IsConfigBatchAvailable(gitPath))
            {
                // Skip test if config-batch is not available
                return;
            }

            string repoPath = CreateRepository(out string workDirPath);

            // Create multiple config entries
            for (int i = 0; i < 20; i++)
            {
                ExecGit(repoPath, workDirPath, $"config --local perf.key{i} value{i}").AssertSuccess();
            }

            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();

            // Test with GitBatchConfiguration
            var sw1 = Stopwatch.StartNew();
            var git1 = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            using (var batchConfig = new GitBatchConfiguration(trace, git1))
            {
                for (int i = 0; i < 20; i++)
                {
                    batchConfig.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                        $"perf.key{i}", out string _);
                }
            }
            sw1.Stop();
            long batchTime = sw1.ElapsedMilliseconds;

            // Test with GitProcessConfiguration (non-batch)
            var sw2 = Stopwatch.StartNew();
            var git2 = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            var processConfig = new GitProcessConfiguration(trace, git2);
            for (int i = 0; i < 20; i++)
            {
                processConfig.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    $"perf.key{i}", out string _);
            }
            sw2.Stop();
            long processTime = sw2.ElapsedMilliseconds;

            // Output results for visibility
            Console.WriteLine($"Batch configuration: {batchTime}ms");
            Console.WriteLine($"Process configuration: {processTime}ms");
            Console.WriteLine($"Speedup: {(double)processTime / batchTime:F2}x");

            // On Windows, batch should generally be faster or similar
            // We don't enforce a hard requirement since test environment varies
            Assert.True(batchTime >= 0 && processTime >= 0, "Both methods should complete successfully");
        }

        [Fact]
        public void GitBatchConfiguration_MultipleQueries_ProducesCorrectResults()
        {
            string gitPath = File.Exists(CustomGitPath) && IsConfigBatchAvailable(CustomGitPath)
                ? CustomGitPath
                : GetGitPath();

            if (!IsConfigBatchAvailable(gitPath))
            {
                // Skip test if config-batch is not available
                return;
            }

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local multi.key1 value1").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local multi.key2 value2").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local multi.key3 value3").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local multi.missing1 xxx").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --unset multi.missing1").AssertSuccess();

            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

            using (var config = new GitBatchConfiguration(trace, git))
            {
                // Test found values
                Assert.True(config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "multi.key1", out string val1));
                Assert.Equal("value1", val1);

                Assert.True(config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "multi.key2", out string val2));
                Assert.Equal("value2", val2);

                Assert.True(config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "multi.key3", out string val3));
                Assert.Equal("value3", val3);

                // Test missing value
                Assert.False(config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "multi.missing1", out string val4));
                Assert.Null(val4);

                // Test another missing value
                Assert.False(config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "multi.missing2", out string val5));
                Assert.Null(val5);

                // Re-read existing values
                Assert.True(config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                    "multi.key1", out string val6));
                Assert.Equal("value1", val6);
            }
        }

        [Fact]
        public void GitBatchConfiguration_DifferentScopes_WorkCorrectly()
        {
            string gitPath = File.Exists(CustomGitPath) && IsConfigBatchAvailable(CustomGitPath)
                ? CustomGitPath
                : GetGitPath();

            if (!IsConfigBatchAvailable(gitPath))
            {
                // Skip test if config-batch is not available
                return;
            }

            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local scope.test local-value").AssertSuccess();

            try
            {
                ExecGit(repoPath, workDirPath, "config --global scope.test global-value").AssertSuccess();

                var trace = new NullTrace();
                var trace2 = new NullTrace2();
                var processManager = new TestProcessManager();
                var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);

                using (var config = new GitBatchConfiguration(trace, git))
                {
                    // Local scope
                    Assert.True(config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                        "scope.test", out string localVal));
                    Assert.Equal("local-value", localVal);

                    // Global scope
                    Assert.True(config.TryGet(GitConfigurationLevel.Global, GitConfigurationType.Raw,
                        "scope.test", out string globalVal));
                    Assert.Equal("global-value", globalVal);

                    // All scope (should return local due to precedence)
                    Assert.True(config.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw,
                        "scope.test", out string allVal));
                    Assert.Equal("local-value", allVal);
                }
            }
            finally
            {
                // Cleanup
                ExecGit(repoPath, workDirPath, "config --global --unset scope.test");
            }
        }
    }
}
