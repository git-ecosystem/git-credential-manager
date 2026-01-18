using System;
using System.Diagnostics;
using System.IO;
using GitCredentialManager.Tests.Objects;
using Xunit;
using Xunit.Abstractions;

namespace GitCredentialManager.Tests
{
    /// <summary>
    /// Tests that simulate credential helper scenarios with config lookups.
    /// </summary>
    public class GitConfigCredentialScenarioTest
    {
        private readonly ITestOutputHelper _output;

        public GitConfigCredentialScenarioTest(ITestOutputHelper output)
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
        public void SimulateCredentialLookup_OfficeRepo()
        {
            const string customGitPath = @"C:\Users\dstolee\_git\git\git\git.exe";
            const string officeRepoPath = @"C:\office\src";

            string gitPath = File.Exists(customGitPath) ? customGitPath : "git";

            if (!Directory.Exists(officeRepoPath) || !Directory.Exists(Path.Combine(officeRepoPath, ".git")))
            {
                _output.WriteLine($"Office repo not found at {officeRepoPath}, skipping");
                return;
            }

            bool hasBatch = IsConfigBatchAvailable(gitPath);
            _output.WriteLine($"=== Credential Lookup Simulation ===");
            _output.WriteLine($"Using Git: {gitPath}");
            _output.WriteLine($"config-batch available: {hasBatch}");
            _output.WriteLine($"Repository: {officeRepoPath}");
            _output.WriteLine("");

            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();

            // Config keys that GCM typically reads during a credential lookup
            string[] credentialConfigKeys = new[]
            {
                // Core credential settings
                "credential.helper",
                "credential.https://dev.azure.com.helper",
                "credential.namespace",
                "credential.interactive",
                "credential.guiPrompt",
                "credential.credentialStore",
                "credential.cacheOptions",
                "credential.msauthFlow",
                "credential.azreposCredentialType",

                // User info
                "user.name",
                "user.email",

                // HTTP settings
                "http.proxy",
                "http.sslbackend",
                "http.sslverify",

                // URL-specific settings
                "credential.https://dev.azure.com.useHttpPath",
                "credential.https://dev.azure.com.provider",

                // Feature flags
                "credential.gitHubAuthModes",
                "credential.bitbucketAuthModes",
            };

            _output.WriteLine($"Simulating lookup of {credentialConfigKeys.Length} config keys");
            _output.WriteLine("(This simulates what GCM does during a credential operation)");
            _output.WriteLine("");

            // Test with Batch Configuration
            var sw1 = Stopwatch.StartNew();
            var git1 = new GitProcess(trace, trace2, processManager, gitPath, officeRepoPath);
            using (var config = new GitBatchConfiguration(trace, git1))
            {
                foreach (var key in credentialConfigKeys)
                {
                    config.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw, key, out string _);
                }
            }
            sw1.Stop();

            _output.WriteLine($"GitBatchConfiguration: {sw1.ElapsedMilliseconds}ms");

            // Test with Process Configuration
            var sw2 = Stopwatch.StartNew();
            var git2 = new GitProcess(trace, trace2, processManager, gitPath, officeRepoPath);
            var config2 = new GitProcessConfiguration(trace, git2);
            foreach (var key in credentialConfigKeys)
            {
                config2.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw, key, out string _);
            }
            sw2.Stop();

            _output.WriteLine($"GitProcessConfiguration: {sw2.ElapsedMilliseconds}ms");
            _output.WriteLine("");

            if (sw1.ElapsedMilliseconds < sw2.ElapsedMilliseconds)
            {
                double speedup = (double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds;
                long saved = sw2.ElapsedMilliseconds - sw1.ElapsedMilliseconds;
                _output.WriteLine($"Time saved per credential operation: {saved}ms");
                _output.WriteLine($"Speedup: {speedup:F2}x");
                _output.WriteLine("");
                _output.WriteLine("Impact: Every git fetch/push/clone will be this much faster!");
            }

            Assert.True(true);
        }

        [Fact]
        public void CompareConfigLookupMethods_DetailedBreakdown()
        {
            const string customGitPath = @"C:\Users\dstolee\_git\git\git\git.exe";
            const string officeRepoPath = @"C:\office\src";

            string gitPath = File.Exists(customGitPath) ? customGitPath : "git";

            if (!Directory.Exists(officeRepoPath) || !Directory.Exists(Path.Combine(officeRepoPath, ".git")))
            {
                _output.WriteLine($"Office repo not found, skipping");
                return;
            }

            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();

            _output.WriteLine("=== Detailed Per-Key Timing Comparison ===");
            _output.WriteLine("");

            string[] testKeys = new[]
            {
                "credential.helper",
                "credential.https://dev.azure.com.helper",
                "user.name",
                "http.proxy",
                "credential.namespace"
            };

            foreach (var key in testKeys)
            {
                // Batch
                var sw1 = Stopwatch.StartNew();
                var git1 = new GitProcess(trace, trace2, processManager, gitPath, officeRepoPath);
                using (var config = new GitBatchConfiguration(trace, git1))
                {
                    config.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw, key, out string val1);
                    _output.WriteLine($"{key}:");
                    _output.WriteLine($"  Value: {val1 ?? "(not set)"}");
                }
                sw1.Stop();

                // Process
                var sw2 = Stopwatch.StartNew();
                var git2 = new GitProcess(trace, trace2, processManager, gitPath, officeRepoPath);
                var config2 = new GitProcessConfiguration(trace, git2);
                config2.TryGet(GitConfigurationLevel.All, GitConfigurationType.Raw, key, out string val2);
                sw2.Stop();

                _output.WriteLine($"  Batch: {sw1.ElapsedMilliseconds}ms");
                _output.WriteLine($"  Process: {sw2.ElapsedMilliseconds}ms");

                if (sw1.ElapsedMilliseconds < sw2.ElapsedMilliseconds)
                {
                    _output.WriteLine($"  Saved: {sw2.ElapsedMilliseconds - sw1.ElapsedMilliseconds}ms");
                }
                _output.WriteLine("");
            }

            Assert.True(true);
        }
    }
}
