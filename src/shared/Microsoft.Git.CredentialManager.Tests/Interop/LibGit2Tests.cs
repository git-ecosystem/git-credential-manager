// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.Interop;
using Microsoft.Git.CredentialManager.Interop.Posix.Native;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Interop
{
    public class LibGit2Tests
    {
        [Fact]
        public void LibGit2_GetRepositoryPath_NotInsideRepository_ReturnsNull()
        {
            var git = new LibGit2();
            string randomPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}");
            Directory.CreateDirectory(randomPath);

            string repositoryPath = git.GetRepositoryPath(randomPath);

            Assert.Null(repositoryPath);
        }

        [Fact]
        public void LibGit2_GetConfiguration_ReturnsConfiguration()
        {
            var git = new LibGit2();
            using (var config = git.GetConfiguration())
            {
                Assert.NotNull(config);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetString_Name_Exists_ReturnsTrueOutString()
        {
            string repoPath = CreateRepository();
            Git(repoPath, "config --local user.name john.doe").AssertSuccess();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                bool result = config.TryGetString("user.name", out string value);
                Assert.True(result);
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetString_Name_DoesNotExists_ReturnsFalse()
        {
            string repoPath = CreateRepository();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomName = $"{Guid.NewGuid():N}.{Guid.NewGuid():N}";
                bool result = config.TryGetString(randomName, out string value);
                Assert.False(result);
                Assert.Null(value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetString_SectionProperty_Exists_ReturnsTrueOutString()
        {
            string repoPath = CreateRepository();
            Git(repoPath, "config --local user.name john.doe").AssertSuccess();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                bool result = config.TryGetString("user", "name", out string value);
                Assert.True(result);
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetString_SectionProperty_DoesNotExists_ReturnsFalse()
        {
            string repoPath = CreateRepository();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomSection  = Guid.NewGuid().ToString("N");
                string randomProperty = Guid.NewGuid().ToString("N");
                bool result = config.TryGetString(randomSection, randomProperty, out string value);
                Assert.False(result);
                Assert.Null(value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetString_SectionScopeProperty_Exists_ReturnsTrueOutString()
        {
            string repoPath = CreateRepository();
            Git(repoPath, "config --local user.example.com.name john.doe").AssertSuccess();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                bool result = config.TryGetString("user", "example.com", "name", out string value);
                Assert.True(result);
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetString_SectionScopeProperty_NullScope_ReturnsTrueOutUnscopedString()
        {
            string repoPath = CreateRepository();
            Git(repoPath, "config --local user.name john.doe").AssertSuccess();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                bool result = config.TryGetString("user", null, "name", out string value);
                Assert.True(result);
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_TryGetString_SectionScopeProperty_DoesNotExists_ReturnsFalse()
        {
            string repoPath = CreateRepository();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomSection  = Guid.NewGuid().ToString("N");
                string randomScope    = Guid.NewGuid().ToString("N");
                string randomProperty = Guid.NewGuid().ToString("N");
                bool result = config.TryGetString(randomSection, randomScope, randomProperty, out string value);
                Assert.False(result);
                Assert.Null(value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_Name_Exists_ReturnsString()
        {
            string repoPath = CreateRepository();
            Git(repoPath, "config --local user.name john.doe").AssertSuccess();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string value = config.GetString("user.name");
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_Name_DoesNotExists_ThrowsException()
        {
            string repoPath = CreateRepository();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomName = $"{Guid.NewGuid():N}.{Guid.NewGuid():N}";
                Assert.Throws<KeyNotFoundException>(() => config.GetString(randomName));
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionProperty_Exists_ReturnsString()
        {
            string repoPath = CreateRepository();
            Git(repoPath, "config --local user.name john.doe").AssertSuccess();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string value = config.GetString("user", "name");
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionProperty_DoesNotExists_ThrowsException()
        {
            string repoPath = CreateRepository();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomSection  = Guid.NewGuid().ToString("N");
                string randomProperty = Guid.NewGuid().ToString("N");
                Assert.Throws<KeyNotFoundException>(() => config.GetString(randomSection, randomProperty));
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionScopeProperty_Exists_ReturnsString()
        {
            string repoPath = CreateRepository();
            Git(repoPath, "config --local user.example.com.name john.doe").AssertSuccess();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string value = config.GetString("user", "example.com", "name");
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionScopeProperty_NullScope_ReturnsUnscopedString()
        {
            string repoPath = CreateRepository();
            Git(repoPath, "config --local user.name john.doe").AssertSuccess();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string value = config.GetString("user", null, "name");
                Assert.NotNull(value);
                Assert.Equal("john.doe", value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_SectionScopeProperty_DoesNotExists_ThrowsException()
        {
            string repoPath = CreateRepository();

            var git = new LibGit2();
            using (var config = git.GetConfiguration(repoPath))
            {
                string randomSection  = Guid.NewGuid().ToString("N");
                string randomScope    = Guid.NewGuid().ToString("N");
                string randomProperty = Guid.NewGuid().ToString("N");
                Assert.Throws<KeyNotFoundException>(() => config.GetString(randomSection, randomScope, randomProperty));
            }
        }

        [Fact]
        public void LibGit2Configuration_GetRepositoryPath_ReturnsRepositoryPath()
        {
            string repoBasePath = CreateRepository();
            string expectedRepoGitPath = Path.Combine(repoBasePath, ".git") + "/";
            string fileL0Path = Path.Combine(repoBasePath, "file.txt");
            string directoryL0Path = Path.Combine(repoBasePath, "directory");
            string fileL1Path = Path.Combine(directoryL0Path, "inner-file.txt");
            string directoryL1Path = Path.Combine(directoryL0Path, "sub-directory");

            var git = new LibGit2();

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

        private static string CreateRepository()
        {
            string tempDirectory = Path.GetTempPath();
            string repoName = $"repo-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            string repoPath = Path.Combine(tempDirectory, repoName);

            if (Directory.Exists(repoPath))
            {
                Directory.Delete(repoPath);
            }

            Directory.CreateDirectory(repoPath);

            Git(repoPath, "init").AssertSuccess();

            return repoPath;
        }

        private static GitResult Git(string repositoryPath, string command)
        {
            var procInfo = new ProcessStartInfo("git", command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            string gitDirectory = Path.Combine(repositoryPath, ".git");
            procInfo.Environment["GIT_DIR"] = gitDirectory;

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
