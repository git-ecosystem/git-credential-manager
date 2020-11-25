// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class GitConfigurationTests
    {
        [Theory]
        [InlineData(null, "\"\"")]
        [InlineData("", "\"\"")]
        [InlineData("hello", "hello")]
        [InlineData("hello world", "\"hello world\"")]
        [InlineData("C:\\app.exe", "C:\\app.exe")]
        [InlineData("C:\\path with space\\app.exe", "\"C:\\path with space\\app.exe\"")]
        [InlineData("''", "\"''\"")]
        [InlineData("'hello'", "\"'hello'\"")]
        [InlineData("'hello world'", "\"'hello world'\"")]
        [InlineData("'C:\\app.exe'", "\"'C:\\app.exe'\"")]
        [InlineData("'C:\\path with space\\app.exe'", "\"'C:\\path with space\\app.exe'\"")]
        [InlineData("\"\"", "\"\\\"\\\"\"")]
        [InlineData("\"hello\"", "\"\\\"hello\\\"\"")]
        [InlineData("\"hello world\"", "\"\\\"hello world\\\"\"")]
        [InlineData("\"C:\\app.exe\"", "\"\\\"C:\\app.exe\\\"\"")]
        [InlineData("\"C:\\path with space\\app.exe\"", "\"\\\"C:\\path with space\\app.exe\\\"\"")]
        [InlineData("\\", "\\")]
        [InlineData("\\\\", "\\\\")]
        [InlineData("\\\\\\", "\\\\\\")]
        [InlineData("\"", "\"\\\"\"")]
        [InlineData("\\\"", "\"\\\\\\\"\"")]
        [InlineData("\\\\\"", "\"\\\\\\\\\\\"\"")]
        [InlineData("\"\\", "\"\\\"\\\\\"")]
        [InlineData("\"\\\\", "\"\\\"\\\\\\\\\"")]
        [InlineData("ab\\", "ab\\")]
        [InlineData("a b\\", "\"a b\\\\\"")]
        public void GitConfiguration_QuoteCmdArg(string input, string expected)
        {
            string actual = GitProcessConfiguration.QuoteCmdArg(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GitProcess_GetConfiguration_ReturnsConfiguration()
        {
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath);
            var config = git.GetConfiguration();
            Assert.NotNull(config);
        }

        [Fact]
        public void GitConfiguration_Enumerate_CallbackReturnsTrue_InvokesCallbackForEachEntry()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local foo.name lancelot").AssertSuccess();
            Git(repoPath, workDirPath, "config --local foo.quest seek-holy-grail").AssertSuccess();
            Git(repoPath, workDirPath, "config --local foo.favcolor blue").AssertSuccess();

            var expectedVisitedEntries = new List<(string name, string value)>
            {
                ("foo.name", "lancelot"),
                ("foo.quest", "seek-holy-grail"),
                ("foo.favcolor", "blue")
            };

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            var actualVisitedEntries = new List<(string name, string value)>();

            bool cb(string name, string value)
            {
                if (name.StartsWith("foo."))
                {
                    actualVisitedEntries.Add((name, value));
                }

                // Continue enumeration
                return true;
            }

            config.Enumerate(cb);

            Assert.Equal(expectedVisitedEntries, actualVisitedEntries);
        }

        [Fact]
        public void GitConfiguration_Enumerate_CallbackReturnsFalse_InvokesCallbackForEachEntryUntilReturnsFalse()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local foo.name lancelot").AssertSuccess();
            Git(repoPath, workDirPath, "config --local foo.quest seek-holy-grail").AssertSuccess();
            Git(repoPath, workDirPath, "config --local foo.favcolor blue").AssertSuccess();

            var expectedVisitedEntries = new List<(string name, string value)>
            {
                ("foo.name", "lancelot"),
                ("foo.quest", "seek-holy-grail")
            };

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            var actualVisitedEntries = new List<(string name, string value)>();

            bool cb(string name, string value)
            {
                if (name.StartsWith("foo."))
                {
                    actualVisitedEntries.Add((name, value));
                }

                // Stop enumeration after 2 'foo' entries
                return actualVisitedEntries.Count < 2;
            }

            config.Enumerate(cb);

            Assert.Equal(expectedVisitedEntries, actualVisitedEntries);
        }

        [Fact]
        public void GitConfiguration_TryGet_Name_Exists_ReturnsTrueOutString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            bool result = config.TryGet("user.name", out string value);
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Equal("john.doe", value);
        }

        [Fact]
        public void GitConfiguration_TryGet_Name_DoesNotExists_ReturnsFalse()
        {
            string repoPath = CreateRepository();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string randomName = $"{Guid.NewGuid():N}.{Guid.NewGuid():N}";
            bool result = config.TryGet(randomName, out string value);
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void GitConfiguration_TryGet_SectionProperty_Exists_ReturnsTrueOutString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            bool result = config.TryGet("user", "name", out string value);
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Equal("john.doe", value);
        }

        [Fact]
        public void GitConfiguration_TryGet_SectionProperty_DoesNotExists_ReturnsFalse()
        {
            string repoPath = CreateRepository();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string randomSection = Guid.NewGuid().ToString("N");
            string randomProperty = Guid.NewGuid().ToString("N");
            bool result = config.TryGet(randomSection, randomProperty, out string value);
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void GitConfiguration_TryGet_SectionScopeProperty_Exists_ReturnsTrueOutString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.example.com.name john.doe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            bool result = config.TryGet("user", "example.com", "name", out string value);
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Equal("john.doe", value);
        }

        [Fact]
        public void GitConfiguration_TryGet_SectionScopeProperty_NullScope_ReturnsTrueOutUnscopedString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            bool result = config.TryGet("user", null, "name", out string value);
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Equal("john.doe", value);
        }

        [Fact]
        public void GitConfiguration_TryGet_SectionScopeProperty_DoesNotExists_ReturnsFalse()
        {
            string repoPath = CreateRepository();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string randomSection = Guid.NewGuid().ToString("N");
            string randomScope = Guid.NewGuid().ToString("N");
            string randomProperty = Guid.NewGuid().ToString("N");
            bool result = config.TryGet(randomSection, randomScope, randomProperty, out string value);
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void GitConfiguration_Get_Name_Exists_ReturnsString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string value = config.Get("user.name");
            Assert.NotNull(value);
            Assert.Equal("john.doe", value);
        }

        [Fact]
        public void GitConfiguration_Get_Name_DoesNotExists_ThrowsException()
        {
            string repoPath = CreateRepository();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string randomName = $"{Guid.NewGuid():N}.{Guid.NewGuid():N}";
            Assert.Throws<KeyNotFoundException>(() => config.Get(randomName));
        }

        [Fact]
        public void GitConfiguration_Get_SectionProperty_Exists_ReturnsString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string value = config.Get("user", "name");
            Assert.NotNull(value);
            Assert.Equal("john.doe", value);
        }

        [Fact]
        public void GitConfiguration_Get_SectionProperty_DoesNotExists_ThrowsException()
        {
            string repoPath = CreateRepository();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string randomSection = Guid.NewGuid().ToString("N");
            string randomProperty = Guid.NewGuid().ToString("N");
            Assert.Throws<KeyNotFoundException>(() => config.Get(randomSection, randomProperty));
        }

        [Fact]
        public void GitConfiguration_Get_SectionScopeProperty_Exists_ReturnsString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.example.com.name john.doe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string value = config.Get("user", "example.com", "name");
            Assert.NotNull(value);
            Assert.Equal("john.doe", value);
        }

        [Fact]
        public void GitConfiguration_Get_SectionScopeProperty_NullScope_ReturnsUnscopedString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string value = config.Get("user", null, "name");
            Assert.NotNull(value);
            Assert.Equal("john.doe", value);
        }

        [Fact]
        public void GitConfiguration_Get_SectionScopeProperty_DoesNotExists_ThrowsException()
        {
            string repoPath = CreateRepository();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string randomSection = Guid.NewGuid().ToString("N");
            string randomScope = Guid.NewGuid().ToString("N");
            string randomProperty = Guid.NewGuid().ToString("N");
            Assert.Throws<KeyNotFoundException>(() => config.Get(randomSection, randomScope, randomProperty));
        }

        [Fact]
        public void GitConfiguration_Set_Local_SetsLocalConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration(GitConfigurationLevel.Local);

            config.Set("core.foobar", "foo123");

            GitResult localResult = Git(repoPath, workDirPath, "config --local core.foobar");

            Assert.Equal("foo123",     localResult.StandardOutput.Trim());
        }

        [Fact]
        public void GitConfiguration_Set_All_ThrowsException()
        {
            string repoPath = CreateRepository(out _);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration(GitConfigurationLevel.All);

            Assert.Throws<InvalidOperationException>(() => config.Set("core.foobar", "test123"));
        }

        [Fact]
        public void GitConfiguration_Unset_Global_UnsetsGlobalConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);
            try
            {

                Git(repoPath, workDirPath, "config --global core.foobar alice").AssertSuccess();
                Git(repoPath, workDirPath, "config --local core.foobar bob").AssertSuccess();

                string gitPath = GetGitPath();
                var trace = new NullTrace();
                var git = new GitProcess(trace, gitPath, repoPath);
                IGitConfiguration config = git.GetConfiguration(GitConfigurationLevel.Global);

                config.Unset("core.foobar");

                GitResult globalResult = Git(repoPath, workDirPath, "config --global core.foobar");
                GitResult localResult = Git(repoPath, workDirPath, "config --local core.foobar");

                Assert.Equal(string.Empty, globalResult.StandardOutput.Trim());
                Assert.Equal("bob", localResult.StandardOutput.Trim());
            }
            finally
            {
                // Cleanup global config changes
                Git(repoPath, workDirPath, "config --global --unset core.foobar");
            }
        }

        [Fact]
        public void GitConfiguration_Unset_Local_UnsetsLocalConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);

            try
            {
                Git(repoPath, workDirPath, "config --global core.foobar alice").AssertSuccess();
                Git(repoPath, workDirPath, "config --local core.foobar bob").AssertSuccess();

                string gitPath = GetGitPath();
                var trace = new NullTrace();
                var git = new GitProcess(trace, gitPath, repoPath);
                IGitConfiguration config = git.GetConfiguration(GitConfigurationLevel.Local);

                config.Unset("core.foobar");

                GitResult globalResult = Git(repoPath, workDirPath, "config --global core.foobar");
                GitResult localResult = Git(repoPath, workDirPath, "config --local core.foobar");

                Assert.Equal("alice", globalResult.StandardOutput.Trim());
                Assert.Equal(string.Empty, localResult.StandardOutput.Trim());
            }
            finally
            {
                // Cleanup global config changes
                Git(repoPath, workDirPath, "config --global --unset core.foobar");
            }
        }

        [Fact]
        public void GitConfiguration_Unset_All_ThrowsException()
        {
            string repoPath = CreateRepository(out _);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration(GitConfigurationLevel.All);

            Assert.Throws<InvalidOperationException>(() => config.Unset("core.foobar"));
        }

        [Fact]
        public void GitConfiguration_UnsetAll_UnsetsAllConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local --add core.foobar foo1").AssertSuccess();
            Git(repoPath, workDirPath, "config --local --add core.foobar foo2").AssertSuccess();
            Git(repoPath, workDirPath, "config --local --add core.foobar bar1").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration(GitConfigurationLevel.Local);

            config.UnsetAll("core.foobar", "foo*");

            GitResult result = Git(repoPath, workDirPath, "config --local --get-all core.foobar");

            Assert.Equal("bar1", result.StandardOutput.Trim());
        }

        [Fact]
        public void GitConfiguration_UnsetAll_All_ThrowsException()
        {
            string repoPath = CreateRepository(out _);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var git = new GitProcess(trace, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration(GitConfigurationLevel.All);

            Assert.Throws<InvalidOperationException>(() => config.UnsetAll("core.foobar", Constants.RegexPatterns.Any));
        }

        #region Test helpers

        private static string GetGitPath()
        {
            ProcessStartInfo psi;
            if (PlatformUtils.IsWindows())
            {
                psi = new ProcessStartInfo(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.System),
                        "where.exe"),
                    "git.exe"
                );
            }
            else
            {
                psi = new ProcessStartInfo("/usr/bin/which", "git");
            }

            psi.RedirectStandardOutput = true;

            using (var which = new Process {StartInfo = psi})
            {
                which.Start();
                which.WaitForExit();

                if (which.ExitCode != 0)
                {
                    throw new Exception("Failed to locate Git");
                }

                string data = which.StandardOutput.ReadLine();

                if (string.IsNullOrWhiteSpace(data))
                {
                    throw new Exception("Failed to locate Git on the PATH");
                }

                return data;
            }
        }

        private static string CreateRepository() => CreateRepository(out _);

        private static string CreateRepository(out string workDirPath)
        {
            string tempDirectory = Path.GetTempPath();
            string repoName = $"repo-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            workDirPath = Path.Combine(tempDirectory, repoName);
            string gitDirPath = Path.Combine(workDirPath, ".git");

            if (Directory.Exists(workDirPath))
            {
                Directory.Delete(workDirPath);
            }

            Directory.CreateDirectory(workDirPath);

            Git(gitDirPath, workDirPath, "init").AssertSuccess();

            return gitDirPath;
        }

        private static GitResult Git(string repositoryPath, string workingDirectory, string command)
        {
            var procInfo = new ProcessStartInfo("git", command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };

            procInfo.Environment["GIT_DIR"] = repositoryPath;

            Process proc = Process.Start(procInfo);
            if (proc is null)
            {
                throw new Exception("Failed to start Git process");
            }

            proc.WaitForExit();

            var result = new GitResult
            {
                ExitCode = proc.ExitCode,
                StandardOutput = proc.StandardOutput.ReadToEnd(),
                StandardError = proc.StandardError.ReadToEnd()
            };

            return result;
        }

        private struct GitResult
        {
            public int ExitCode;
            public string StandardOutput;
            public string StandardError;

            public void AssertSuccess()
            {
                Assert.Equal(0, ExitCode);
            }
        }

        #endregion
    }
}
