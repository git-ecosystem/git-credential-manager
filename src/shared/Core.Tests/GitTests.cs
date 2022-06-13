using System;
using System.IO;
using System.Linq;
using GitCredentialManager.Tests.Objects;
using Xunit;
using static GitCredentialManager.Tests.GitTestUtilities;

namespace GitCredentialManager.Tests
{
    public class GitTests
    {
        [Fact]
        public void Git_GetCurrentRepository_NoLocalRepo_ReturnsNull()
        {
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();
            var git = new GitProcess(trace, env, gitPath, Path.GetTempPath());

            string actual = git.GetCurrentRepository();

            Assert.Null(actual);
        }

        [Fact]
        public void Git_GetCurrentRepository_LocalRepo_ReturnsNotNull()
        {
            CreateRepository(out string workDirPath);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();
            var git = new GitProcess(trace, env, gitPath, workDirPath);

            string actual = git.GetCurrentRepository();

            Assert.NotNull(actual);
        }

        [Fact]
        public void Git_GetRemotes_NoLocalRepo_ReturnsEmpty()
        {
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();
            var git = new GitProcess(trace, env, gitPath, Path.GetTempPath());

            GitRemote[] remotes = git.GetRemotes().ToArray();

            Assert.Empty(remotes);
        }

        [Fact]
        public void Git_GetRemotes_NoRemotes_ReturnsEmpty()
        {
            CreateRepository(out string workDirPath);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();
            var git = new GitProcess(trace, env, gitPath, workDirPath);

            GitRemote[] remotes = git.GetRemotes().ToArray();

            Assert.Empty(remotes);
        }

        [Fact]
        public void Git_GetRemotes_OneRemote_ReturnsRemote()
        {
            string name = "origin";
            string url = "https://example.com";
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, $"remote add {name} {url}");

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            GitRemote[] remotes = git.GetRemotes().ToArray();

            Assert.Single(remotes);
            AssertRemote(name, url, remotes[0]);
        }

        [Fact]
        public void Git_GetRemotes_OneRemoteFetchAndPull_ReturnsRemote()
        {
            string name = "origin";
            string fetchUrl = "https://fetch.example.com";
            string pushUrl = "https://push.example.com";
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, $"remote add {name} {fetchUrl}");
            ExecGit(repoPath, workDirPath, $"remote set-url --push {name} {pushUrl}");

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            GitRemote[] remotes = git.GetRemotes().ToArray();

            Assert.Single(remotes);
            AssertRemote(name, fetchUrl, pushUrl, remotes[0]);
        }

        [Theory]
        [InlineData("ssh://user@example.com/account/repo.git")]
        [InlineData("user@example.com:account/repo.git")]
        [InlineData("git://example.com/path/to/repo.git")]
        [InlineData("file:///path/to/repo.git")]
        [InlineData("/path/to/repo.git")]
        public void Git_GetRemotes_NonHttpRemote_ReturnsRemote(string url)
        {
            string name = "origin";
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, $"remote add {name} {url}");

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            GitRemote[] remotes = git.GetRemotes().ToArray();

            Assert.Single(remotes);
            AssertRemote(name, url, remotes[0]);
        }

        [Fact]
        public void Git_GetRemotes_MultipleRemotes_ReturnsAllRemotes()
        {
            string name1 = "origin";
            string name2 = "test";
            string name3 = "upstream";
            string url1 = "https://example.com/origin";
            string url2 = "https://example.com/test";
            string url3 = "https://example.com/upstream";
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, $"remote add {name1} {url1}");
            ExecGit(repoPath, workDirPath, $"remote add {name2} {url2}");
            ExecGit(repoPath, workDirPath, $"remote add {name3} {url3}");

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            GitRemote[] remotes = git.GetRemotes().ToArray();

            Assert.Equal(3, remotes.Length);

            AssertRemote(name1, url1, remotes[0]);
            AssertRemote(name2, url2, remotes[1]);
            AssertRemote(name3, url3, remotes[2]);
        }

        [Fact]
        public void Git_GetRemotes_RemoteNoFetchOnlyPull_ReturnsRemote()
        {
            string name = "origin";
            string pushUrl = "https://example.com";
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, $"remote add {name} {pushUrl}");
            ExecGit(repoPath, workDirPath, $"remote set-url --push {name} {pushUrl}");
            ExecGit(repoPath, workDirPath, $"config --unset --local remote.{name}.url");

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            GitRemote[] remotes = git.GetRemotes().ToArray();

            Assert.Single(remotes);

            AssertRemote(name, null, pushUrl, remotes[0]);
        }

        [Fact]
        public void Git_Version_ReturnsVersion()
        {
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            var git = new GitProcess(trace, env, gitPath, Path.GetTempPath());
            GitVersion version = git.Version;

            Assert.NotEqual(new GitVersion(), version);
        }

        [Fact]
        public void Git_GitPath_ReturnsGitPath()
        {
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            var git = new GitProcess(trace, env, gitPath, Path.GetTempPath());

            Assert.Equal(gitPath, git.GitPath);
        }

        [Fact]
        public void Git_AddFile_FileExists_AddsFileToIndex()
        {
            string repoPath = CreateRepository(out string workDirPath);
            string filePath = Path.Combine(workDirPath, "file1.txt");
            File.WriteAllText(filePath, "test file contents");

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            git.AddFile(filePath);

            GitResult lsResult = ExecGit(repoPath, workDirPath, "ls-files --cached");
            string lsOutput = lsResult.StandardOutput.Trim();
            Assert.Equal("file1.txt", lsOutput);
        }

        [Fact]
        public void Git_AddFile_MissingFile_ThrowsException()
        {
            string repoPath = CreateRepository(out string workDirPath);
            string filePath = Path.Combine(workDirPath, "file1.txt");

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            Assert.Throws<GitException>(() => git.AddFile(filePath));
        }

        [Fact]
        public void Git_AddFile_NotInsideRepository_ThrowsException()
        {
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N").Substring(8));
            Directory.CreateDirectory(tempDir);
            string filePath = Path.Combine(tempDir, "file1.txt");
            File.WriteAllText(filePath, "test file contents");

            var git = new GitProcess(trace, env, gitPath, tempDir);
            Assert.Throws<GitException>(() => git.AddFile(filePath));
        }

        [Fact]
        public void Git_Commit_Message_FilesInIndex_CreatesCommit()
        {
            string repoPath = CreateRepository(out string workDirPath);
            ExecGit(repoPath, workDirPath, "config --local user.name 'John Doe'");
            ExecGit(repoPath, workDirPath, "config --local user.email john.doe@example.com");

            string filePath = Path.Combine(workDirPath, "file1.txt");
            File.WriteAllText(filePath, "test file contents");
            ExecGit(repoPath, workDirPath, "add file1.txt");

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            const string message = "test message";

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            git.Commit(message);

            GitResult showResult = ExecGit(repoPath, workDirPath, "show HEAD");
            string showOutput = showResult.StandardOutput.Trim();

            Assert.Contains(message, showOutput);
        }

        [Fact]
        public void Git_Commit_EmptyMessage_FilesInIndex_ThrowsException()
        {
            string repoPath = CreateRepository(out string workDirPath);
            string filePath = Path.Combine(workDirPath, "file1.txt");
            File.WriteAllText(filePath, "test file contents");
            ExecGit(repoPath, workDirPath, "add file1.txt");

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            const string message = null;

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            Assert.Throws<ArgumentNullException>(() => git.Commit(message));
        }

        [Fact]
        public void Git_Commit_Message_NoFilesInIndex_ThrowsException()
        {
            string repoPath = CreateRepository(out string workDirPath);

            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            const string message = "test message";

            var git = new GitProcess(trace, env, gitPath, workDirPath);
            Assert.Throws<GitException>(() => git.Commit(message));
        }

        [Fact]
        public void Git_Commit_Message_NotInsideRepository_ThrowsException()
        {
            string gitPath = GetGitPath();
            var trace = new NullTrace();
            var env = new TestEnvironment();

            const string message = "test message";

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N").Substring(8));
            Directory.CreateDirectory(tempDir);

            var git = new GitProcess(trace, env, gitPath, tempDir);
            Assert.Throws<GitException>(() => git.Commit(message));
        }


        #region Test Helpers

        private static void AssertRemote(string expectedName, string expectedUrl, GitRemote remote)
        {
            Assert.Equal(expectedName, remote.Name);
            Assert.Equal(expectedUrl, remote.FetchUrl);
            Assert.Equal(expectedUrl, remote.PushUrl);
        }

        private static void AssertRemote(string expectedName, string expectedFetchUrl, string expectedPushUrl, GitRemote remote)
        {
            Assert.Equal(expectedName, remote.Name);
            Assert.Equal(expectedFetchUrl, remote.FetchUrl);
            Assert.Equal(expectedPushUrl, remote.PushUrl);
        }

        #endregion
    }
}
