using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace GitCredentialManager.Tests
{
    public static class GitTestUtilities
    {
        public static string GetGitPath()
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

        public static string CreateRepository() => CreateRepository(out _);

        public static string CreateRepository(out string workDirPath)
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

            ExecGit(gitDirPath, workDirPath, "init").AssertSuccess();

            return gitDirPath;
        }

        public static GitResult ExecGit(string repositoryPath, string workingDirectory, string command)
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

        public struct GitResult
        {
            public int ExitCode;
            public string StandardOutput;
            public string StandardError;

            public void AssertSuccess()
            {
                Assert.Equal(0, ExitCode);
            }
        }
    }
}
