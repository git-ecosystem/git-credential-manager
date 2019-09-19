// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.Interop;
using Microsoft.Git.CredentialManager.Interop.Posix.Native;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Interop
{
    public class LibGit2Tests
    {
        [Fact]
        public void LibGit2_GetRepositoryPath_NotInsideRepository_ReturnsNull()
        {
            var trace = new NullTrace();
            var git = new LibGit2(trace);
            string randomPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}");
            Directory.CreateDirectory(randomPath);

            string repositoryPath = git.GetRepositoryPath(randomPath);

            Assert.Null(repositoryPath);
        }

        [Fact]
        public void LibGit2_GetConfiguration_ReturnsConfiguration()
        {
            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration())
            {
                Assert.NotNull(config);
            }
        }

        [Fact]
        public void LibGit2Configuration_Enumerate_CallbackReturnsTrue_InvokesCallbackForEachEntry()
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

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
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
        }

        [Fact]
        public void LibGit2Configuration_Enumerate_CallbackReturnsFalse_InvokesCallbackForEachEntryUntilReturnsFalse()
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

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
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
        }

        [Fact]
        public void LibGit2Configuration_TryGetValue_Name_Exists_ReturnsTrueOutString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                bool result = config.TryGetValue("user.name", out string value);
                Assert.True(result);
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetValue_Name_DoesNotExists_ReturnsFalse()
        {
            string repoPath = CreateRepository();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomName = $"{Guid.NewGuid():N}.{Guid.NewGuid():N}";
                bool result = config.TryGetValue(randomName, out string value);
                Assert.False(result);
                Assert.Null(value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetValue_SectionProperty_Exists_ReturnsTrueOutString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                bool result = config.TryGetValue("user", "name", out string value);
                Assert.True(result);
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetValue_SectionProperty_DoesNotExists_ReturnsFalse()
        {
            string repoPath = CreateRepository();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomSection  = Guid.NewGuid().ToString("N");
                string randomProperty = Guid.NewGuid().ToString("N");
                bool result = config.TryGetValue(randomSection, randomProperty, out string value);
                Assert.False(result);
                Assert.Null(value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetValue_SectionScopeProperty_Exists_ReturnsTrueOutString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.example.com.name john.doe").AssertSuccess();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                bool result = config.TryGetValue("user", "example.com", "name", out string value);
                Assert.True(result);
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetValue_SectionScopeProperty_NullScope_ReturnsTrueOutUnscopedString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                bool result = config.TryGetValue("user", null, "name", out string value);
                Assert.True(result);
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetValue_SectionScopeProperty_DoesNotExists_ReturnsFalse()
        {
            string repoPath = CreateRepository();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomSection  = Guid.NewGuid().ToString("N");
                string randomScope    = Guid.NewGuid().ToString("N");
                string randomProperty = Guid.NewGuid().ToString("N");
                bool result = config.TryGetValue(randomSection, randomScope, randomProperty, out string value);
                Assert.False(result);
                Assert.Null(value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_Name_Exists_ReturnsString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string value = config.GetValue("user.name");
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_Name_DoesNotExists_ThrowsException()
        {
            string repoPath = CreateRepository();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomName = $"{Guid.NewGuid():N}.{Guid.NewGuid():N}";
                Assert.Throws<KeyNotFoundException>(() => config.GetValue(randomName));
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionProperty_Exists_ReturnsString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string value = config.GetValue("user", "name");
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionProperty_DoesNotExists_ThrowsException()
        {
            string repoPath = CreateRepository();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomSection  = Guid.NewGuid().ToString("N");
                string randomProperty = Guid.NewGuid().ToString("N");
                Assert.Throws<KeyNotFoundException>(() => config.GetValue(randomSection, randomProperty));
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionScopeProperty_Exists_ReturnsString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.example.com.name john.doe").AssertSuccess();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string value = config.GetValue("user", "example.com", "name");
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionScopeProperty_NullScope_ReturnsUnscopedString()
        {
            string repoPath = CreateRepository(out string workDirPath);
            Git(repoPath, workDirPath, "config --local user.name john.doe").AssertSuccess();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string value = config.GetValue("user", null, "name");
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionScopeProperty_DoesNotExists_ThrowsException()
        {
            string repoPath = CreateRepository();

            var trace = new NullTrace();
            var git = new LibGit2(trace);
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomSection  = Guid.NewGuid().ToString("N");
                string randomScope    = Guid.NewGuid().ToString("N");
                string randomProperty = Guid.NewGuid().ToString("N");
                Assert.Throws<KeyNotFoundException>(() => config.GetValue(randomSection, randomScope, randomProperty));
            }
        }

        [Fact]
        public void LibGit2Configuration_GetRepositoryPath_ReturnsRepositoryPath()
        {
            string expectedRepoGitPath = CreateRepository(out string workDirPath) + "/";
            string fileL0Path = Path.Combine(workDirPath, "file.txt");
            string directoryL0Path = Path.Combine(workDirPath, "directory");
            string fileL1Path = Path.Combine(directoryL0Path, "inner-file.txt");
            string directoryL1Path = Path.Combine(directoryL0Path, "sub-directory");

            var trace = new NullTrace();
            var git = new LibGit2(trace);

            // Create files and directories
            Directory.CreateDirectory(directoryL0Path);
            Directory.CreateDirectory(directoryL1Path);
            File.WriteAllText(fileL0Path, string.Empty);
            File.WriteAllText(fileL1Path, string.Empty);

            // Check from L0 file
            string fileL0RepoPath = git.GetRepositoryPath(fileL0Path);
            AssertPathsEquivalent(expectedRepoGitPath, fileL0RepoPath);

            // Check from L0 directory
            string dirL0RepoPath = git.GetRepositoryPath(directoryL0Path);
            AssertPathsEquivalent(expectedRepoGitPath, dirL0RepoPath);

            // Check from L1 file
            string fileL1RepoPath = git.GetRepositoryPath(fileL1Path);
            AssertPathsEquivalent(expectedRepoGitPath, fileL1RepoPath);

            // Check from L1 directory
            string dirL1RepoPath = git.GetRepositoryPath(directoryL1Path);
            AssertPathsEquivalent(expectedRepoGitPath, dirL1RepoPath);
        }

        #region Test helpers

        private static void AssertPathsEquivalent(string expected, string actual)
        {
            string realExpected = RealPath(expected);
            string realActual   = RealPath(actual);

            Assert.Equal(realExpected, realActual);
        }

        /// <summary>
        /// Resolve symlinks and canonicalize the path (including "/" -> "\" on Windows)
        /// </summary>
        private static string RealPath(string path)
        {
            if (PlatformUtils.IsPosix())
            {
                bool trailingSlash = path.EndsWith("/");

                string resolvedPath;
                byte[] pathBytes = Encoding.UTF8.GetBytes(path);
                unsafe
                {
                    byte* resolvedPtr;
                    fixed (byte* pathPtr = pathBytes)
                    {
                        if ((resolvedPtr = Stdlib.realpath(pathPtr, (byte*) IntPtr.Zero)) == (byte*) IntPtr.Zero)
                        {
                            return null;
                        }
                    }

                    resolvedPath = U8StringConverter.ToManaged(resolvedPtr);
                }

                // Preserve the trailing slash if there was one present initially
                return trailingSlash ? $"{resolvedPath}/" : resolvedPath;
            }

            if (PlatformUtils.IsWindows())
            {
                // GetFullPath on Windows already preserves trailing slashes
                return Path.GetFullPath(path);
            }

            throw new PlatformNotSupportedException();
        }

        private static string CreateBareRepository()
        {
            string tempDirectory = Path.GetTempPath();
            string repoName = $"repo-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var gitDirPath = Path.Combine(tempDirectory, repoName);

            if (Directory.Exists(gitDirPath))
            {
                Directory.Delete(gitDirPath);
            }

            Directory.CreateDirectory(gitDirPath);

            Git(gitDirPath, gitDirPath, "init --bare").AssertSuccess();

            return gitDirPath;
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
