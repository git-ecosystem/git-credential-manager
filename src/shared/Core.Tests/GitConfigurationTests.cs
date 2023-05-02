using System;
using System.Collections.Generic;
using System.IO;
using GitCredentialManager.Tests.Objects;
using Xunit;
using static GitCredentialManager.Tests.GitTestUtilities;

namespace GitCredentialManager.Tests
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
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath);
            var config = git.GetConfiguration();
            Assert.NotNull(config);
        }

        [Fact]
        public void GitConfiguration_Enumerate_CallbackReturnsTrue_InvokesCallbackForEachEntry()
        {
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local foo.name lancelot").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local foo.quest seek-holy-grail").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local foo.favcolor blue").AssertSuccess();

            var expectedVisitedEntries = new List<(string name, string value)>
            {
                ("foo.name", "lancelot"),
                ("foo.quest", "seek-holy-grail"),
                ("foo.favcolor", "blue")
            };

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            var actualVisitedEntries = new List<(string name, string value)>();

            bool cb(GitConfigurationEntry entry)
            {
                if (entry.Key.StartsWith("foo."))
                {
                    actualVisitedEntries.Add((entry.Key, entry.Value));
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
            ExecGit(repoPath, workDirPath, "config --local foo.name lancelot").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local foo.quest seek-holy-grail").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local foo.favcolor blue").AssertSuccess();

            var expectedVisitedEntries = new List<(string name, string value)>
            {
                ("foo.name", "lancelot"),
                ("foo.quest", "seek-holy-grail")
            };

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            var actualVisitedEntries = new List<(string name, string value)>();

            bool cb(GitConfigurationEntry entry)
            {
                if (entry.Key.StartsWith("foo."))
                {
                    actualVisitedEntries.Add((entry.Key, entry.Value));
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
            ExecGit(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();

            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            bool result = config.TryGet("user.name", false, out string value);
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

            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string randomName = $"{Guid.NewGuid():N}.{Guid.NewGuid():N}";
            bool result = config.TryGet(randomName, false, out string value);
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void GitConfiguration_TryGet_IsPath_True_ReturnsCanonicalPath()
        {
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local example.path ~/test").AssertSuccess();

            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            bool result = config.TryGet("example.path", true, out string value);
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Equal(Path.GetFullPath($"{homeDirectory}/test").Replace("\\", "/"), value);
        }

        [Fact]
        public void GitConfiguration_TryGet_IsPath_False_ReturnsRawConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local example.path ~/test").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            bool result = config.TryGet("example.path", false, out string value);
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Equal($"~/test", value);
        }

        [Fact]
        public void GitConfiguration_TryGet_BoolType_ReturnsCanonicalBool()
        {
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local example.bool fAlSe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            bool result = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Bool,
                "example.bool", out string value);
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Equal("false", value);
        }

        [Fact]
        public void GitConfiguration_TryGet_BoolWithoutType_ReturnsRawConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local example.bool fAlSe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            bool result = config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                "example.bool", out string value);
            Assert.True(result);
            Assert.NotNull(value);
            Assert.Equal("fAlSe", value);
        }

        [Fact]
        public void GitConfiguration_Get_Name_Exists_ReturnsString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();

            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
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
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            string randomName = $"{Guid.NewGuid():N}.{Guid.NewGuid():N}";
            Assert.Throws<KeyNotFoundException>(() => config.Get(randomName));
        }

        [Fact]
        public void GitConfiguration_Set_Local_SetsLocalConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);;
            IGitConfiguration config = git.GetConfiguration();

            config.Set(GitConfigurationLevel.Local, "core.foobar", "foo123");

            GitResult localResult = ExecGit(repoPath, workDirPath, "config --local core.foobar");

            Assert.Equal("foo123", localResult.StandardOutput.Trim());
        }

        [Fact]
        public void GitConfiguration_Set_All_ThrowsException()
        {
            string repoPath = CreateRepository(out _);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            Assert.Throws<InvalidOperationException>(() =>
                config.Set(GitConfigurationLevel.All, "core.foobar", "test123"));
        }

        [Fact]
        public void GitConfiguration_Unset_Global_UnsetsGlobalConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);
            try
            {
                ExecGit(repoPath, workDirPath, "config --global core.foobar alice").AssertSuccess();
                ExecGit(repoPath, workDirPath, "config --local core.foobar bob").AssertSuccess();

                string gitPath = GetGitPath();
                var trace = new NullTrace();
                var trace2 = new NullTrace2();
                var processManager = new TestProcessManager();
                var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
                IGitConfiguration config = git.GetConfiguration();

                config.Unset(GitConfigurationLevel.Global, "core.foobar");

                GitResult globalResult = ExecGit(repoPath, workDirPath, "config --global core.foobar");
                GitResult localResult = ExecGit(repoPath, workDirPath, "config --local core.foobar");

                Assert.Equal(string.Empty, globalResult.StandardOutput.Trim());
                Assert.Equal("bob", localResult.StandardOutput.Trim());
            }
            finally
            {
                // Cleanup global config changes
                ExecGit(repoPath, workDirPath, "config --global --unset core.foobar");
            }
        }

        [Fact]
        public void GitConfiguration_Unset_Local_UnsetsLocalConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);

            try
            {
                ExecGit(repoPath, workDirPath, "config --global core.foobar alice").AssertSuccess();
                ExecGit(repoPath, workDirPath, "config --local core.foobar bob").AssertSuccess();

                string gitPath = GetGitPath();
                var trace = new NullTrace();
                var trace2 = new NullTrace2();
                var processManager = new TestProcessManager();
                var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
                IGitConfiguration config = git.GetConfiguration();

                config.Unset(GitConfigurationLevel.Local, "core.foobar");

                GitResult globalResult = ExecGit(repoPath, workDirPath, "config --global core.foobar");
                GitResult localResult = ExecGit(repoPath, workDirPath, "config --local core.foobar");

                Assert.Equal("alice", globalResult.StandardOutput.Trim());
                Assert.Equal(string.Empty, localResult.StandardOutput.Trim());
            }
            finally
            {
                // Cleanup global config changes
                ExecGit(repoPath, workDirPath, "config --global --unset core.foobar");
            }
        }

        [Fact]
        public void GitConfiguration_Unset_All_ThrowsException()
        {
            string repoPath = CreateRepository(out _);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            Assert.Throws<InvalidOperationException>(() => config.Unset(GitConfigurationLevel.All, "core.foobar"));
        }

        [Fact]
        public void GitConfiguration_UnsetAll_UnsetsAllConfig()
        {
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local --add core.foobar foo1").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local --add core.foobar foo2").AssertSuccess();
            ExecGit(repoPath, workDirPath, "config --local --add core.foobar bar1").AssertSuccess();

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            config.UnsetAll(GitConfigurationLevel.Local, "core.foobar", "foo*");

            GitResult result = ExecGit(repoPath, workDirPath, "config --local --get-all core.foobar");

            Assert.Equal("bar1", result.StandardOutput.Trim());
        }

        [Fact]
        public void GitConfiguration_UnsetAll_All_ThrowsException()
        {
            string repoPath = CreateRepository(out _);

            string gitPath = GetGitPath();
            var trace = new NullTrace();

            var trace2 = new NullTrace2();
            var processManager = new TestProcessManager();
            var git = new GitProcess(trace, trace2, processManager, gitPath, repoPath);
            IGitConfiguration config = git.GetConfiguration();

            Assert.Throws<InvalidOperationException>(() =>
                config.UnsetAll(GitConfigurationLevel.All, "core.foobar", Constants.RegexPatterns.Any));
        }
    }
}
