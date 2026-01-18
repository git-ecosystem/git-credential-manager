using System;
using System.Diagnostics;
using System.IO;
using GitCredentialManager.Tests.Objects;
using Xunit;
using Xunit.Abstractions;

namespace GitCredentialManager.Tests
{
    /// <summary>
    /// Performance benchmark for git config operations.
    /// Run with: dotnet test --filter "FullyQualifiedName~GitConfigPerformanceBenchmark"
    /// </summary>
    public class GitConfigPerformanceBenchmark
    {
        private readonly ITestOutputHelper _output;

        public GitConfigPerformanceBenchmark(ITestOutputHelper output)
        {
            _output = output;
        }

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
        public void Benchmark_GitConfig_WithCustomGitAndRepo()
        {
            // Configuration
            const string customGitPath = @"C:\Users\dstolee\_git\git\git\git.exe";
            const string officeRepoPath = @"C:\office\src";

            _output.WriteLine("=== Git Config Performance Benchmark ===");
            _output.WriteLine($"Git Path: {customGitPath}");
            _output.WriteLine($"Repo Path: {officeRepoPath}");
            _output.WriteLine("");

            // Check if custom Git exists
            if (!File.Exists(customGitPath))
            {
                _output.WriteLine($"WARNING: Custom Git not found at {customGitPath}");
                _output.WriteLine("Using system Git instead");
            }

            string gitPath = File.Exists(customGitPath) ? customGitPath : "git";
            bool hasBatch = IsConfigBatchAvailable(gitPath);

            _output.WriteLine($"Git version: {GetGitVersion(gitPath)}");
            _output.WriteLine($"config-batch available: {hasBatch}");
            _output.WriteLine("");

            // Check if office repo exists
            if (!Directory.Exists(officeRepoPath) || !Directory.Exists(Path.Combine(officeRepoPath, ".git")))
            {
                _output.WriteLine($"WARNING: Office repo not found at {officeRepoPath}");
                _output.WriteLine("Test will be skipped");
                return;
            }

            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();

            // Common config keys that credential helpers typically read
            string[] commonKeys = new[]
            {
                "credential.helper",
                "credential.https://dev.azure.com.helper",
                "credential.useHttpPath",
                "credential.namespace",
                "user.name",
                "user.email",
                "core.autocrlf",
                "core.longpaths",
                "http.sslbackend",
                "http.proxy",
                "credential.interactive",
                "credential.guiPrompt",
                "credential.credentialStore",
                "credential.cacheOptions",
                "credential.gitHubAuthModes"
            };

            _output.WriteLine($"Testing with {commonKeys.Length} config keys (3 iterations each)");
            _output.WriteLine("");

            // Warmup
            var git0 = new GitProcess(trace, trace2, processManager, gitPath, officeRepoPath);
            var warmupConfig = git0.GetConfiguration();
            warmupConfig.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw, "user.name", out string _);

            // Benchmark with GitBatchConfiguration
            _output.WriteLine("--- GitBatchConfiguration (with fallback) ---");
            var batchTimes = new long[3];
            for (int iteration = 0; iteration < 3; iteration++)
            {
                var sw = Stopwatch.StartNew();
                var git = new GitProcess(trace, trace2, processManager, gitPath, officeRepoPath);
                using (var config = new GitBatchConfiguration(trace, git))
                {
                    foreach (var key in commonKeys)
                    {
                        config.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw, key, out string _);
                    }
                }
                sw.Stop();
                batchTimes[iteration] = sw.ElapsedMilliseconds;
                _output.WriteLine($"  Iteration {iteration + 1}: {sw.ElapsedMilliseconds}ms");
            }

            long avgBatch = (batchTimes[0] + batchTimes[1] + batchTimes[2]) / 3;
            _output.WriteLine($"  Average: {avgBatch}ms");
            _output.WriteLine("");

            // Benchmark with GitProcessConfiguration (traditional)
            _output.WriteLine("--- GitProcessConfiguration (traditional) ---");
            var processTimes = new long[3];
            for (int iteration = 0; iteration < 3; iteration++)
            {
                var sw = Stopwatch.StartNew();
                var git = new GitProcess(trace, trace2, processManager, gitPath, officeRepoPath);
                var config = new GitProcessConfiguration(trace, git);
                foreach (var key in commonKeys)
                {
                    config.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw, key, out string _);
                }
                sw.Stop();
                processTimes[iteration] = sw.ElapsedMilliseconds;
                _output.WriteLine($"  Iteration {iteration + 1}: {sw.ElapsedMilliseconds}ms");
            }

            long avgProcess = (processTimes[0] + processTimes[1] + processTimes[2]) / 3;
            _output.WriteLine($"  Average: {avgProcess}ms");
            _output.WriteLine("");

            // Results
            _output.WriteLine("=== RESULTS ===");
            _output.WriteLine($"GitBatchConfiguration average: {avgBatch}ms");
            _output.WriteLine($"GitProcessConfiguration average: {avgProcess}ms");

            if (avgBatch < avgProcess)
            {
                double speedup = (double)avgProcess / avgBatch;
                long improvement = avgProcess - avgBatch;
                _output.WriteLine($"SPEEDUP: {speedup:F2}x faster ({improvement}ms improvement)");
            }
            else if (avgProcess < avgBatch)
            {
                double slowdown = (double)avgBatch / avgProcess;
                long regression = avgBatch - avgProcess;
                _output.WriteLine($"REGRESSION: {slowdown:F2}x slower ({regression}ms regression)");
            }
            else
            {
                _output.WriteLine("RESULT: Same performance");
            }

            _output.WriteLine("");
            _output.WriteLine($"Total config reads: {commonKeys.Length * 3 * 2} ({commonKeys.Length} keys × 3 iterations × 2 methods)");

            // The test always passes - this is just for benchmarking
            Assert.True(true);
        }

        [Fact]
        public void Benchmark_ManySequentialReads()
        {
            const string customGitPath = @"C:\Users\dstolee\_git\git\git\git.exe";
            const string officeRepoPath = @"C:\office\src";

            string gitPath = File.Exists(customGitPath) ? customGitPath : "git";

            if (!Directory.Exists(officeRepoPath) || !Directory.Exists(Path.Combine(officeRepoPath, ".git")))
            {
                _output.WriteLine($"Office repo not found at {officeRepoPath}, skipping");
                return;
            }

            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();

            const int numReads = 50;
            const string testKey = "user.name";

            _output.WriteLine($"=== Sequential Reads Benchmark ({numReads} reads) ===");
            _output.WriteLine("");

            // Batch
            var sw1 = Stopwatch.StartNew();
            var git1 = new GitProcess(trace, trace2, processManager, gitPath, officeRepoPath);
            using (var config = new GitBatchConfiguration(trace, git1))
            {
                for (int i = 0; i < numReads; i++)
                {
                    config.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw, testKey, out string _);
                }
            }
            sw1.Stop();

            _output.WriteLine($"GitBatchConfiguration: {sw1.ElapsedMilliseconds}ms ({(double)sw1.ElapsedMilliseconds / numReads:F2}ms per read)");

            // Process
            var sw2 = Stopwatch.StartNew();
            var git2 = new GitProcess(trace, trace2, processManager, gitPath, officeRepoPath);
            var config2 = new GitProcessConfiguration(trace, git2);
            for (int i = 0; i < numReads; i++)
            {
                config2.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw, testKey, out string _);
            }
            sw2.Stop();

            _output.WriteLine($"GitProcessConfiguration: {sw2.ElapsedMilliseconds}ms ({(double)sw2.ElapsedMilliseconds / numReads:F2}ms per read)");
            _output.WriteLine("");

            if (sw1.ElapsedMilliseconds < sw2.ElapsedMilliseconds)
            {
                double speedup = (double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds;
                _output.WriteLine($"Batch is {speedup:F2}x faster");
            }

            Assert.True(true);
        }

        private string GetGitVersion(string gitPath)
        {
            try
            {
                var psi = new ProcessStartInfo(gitPath, "version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return "unknown";
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output.Trim();
                }
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
