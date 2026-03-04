using System;
using System.Diagnostics;
using System.IO;
using GitCredentialManager.Tests.Objects;
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

            using (var which = new ChildProcess(new NullTrace2(), psi))
            {
                which.Start(Trace2ProcessClass.None);
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

            // Use an isolated global config file in the test's working directory to avoid
            // failures when HOME is not set to a valid directory (e.g., in package build
            // environments). This also prevents tests from polluting the user's real global config.
            procInfo.Environment["GIT_CONFIG_GLOBAL"] = Path.Combine(workingDirectory, ".gitconfig-global");

            var proc = ChildProcess.Start(new NullTrace2(), procInfo, Trace2ProcessClass.None);
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

        /// <summary>
        /// Sets an environment variable for the scope of a using block, restoring the original value on dispose.
        /// </summary>
        /// <remarks>
        /// This class modifies the process-level environment, which is shared across all threads.
        /// To avoid interference, tests using this class should not run concurrently. Tests within
        /// the same xUnit test class are guaranteed to run serially by default.
        /// </remarks>
        public sealed class EnvVarScope : IDisposable
        {
            private readonly string _name;
            private readonly string _previousValue;

            public EnvVarScope(string name, string value)
            {
                _name = name;
                _previousValue = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(_name, _previousValue);
            }
        }
    }
}
