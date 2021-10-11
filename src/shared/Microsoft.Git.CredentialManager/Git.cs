using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    public interface IGit
    {
        /// <summary>
        /// The version of the Git executable tied to this instance.
        /// </summary>
        GitVersion Version { get; }

        /// <summary>
        /// Create a Git process object with the specified arguments.
        /// </summary>
        /// <param name="args">Arguments to pass to the Git process.</param>
        /// <returns>Process object ready to be started.</returns>
        Process CreateProcess(string args);

        /// <summary>
        /// Return the path to the current repository, or null if this instance is not
        /// scoped to a Git repository.
        /// </summary>
        /// <returns>Absolute path to the current Git repository, or null.</returns>
        string GetCurrentRepository();

        /// <summary>
        /// Get all remotes for the current repository.
        /// </summary>
        /// <returns>Names of all remotes in the current repository.</returns>
        IEnumerable<GitRemote> GetRemotes();

        /// <summary>
        /// Get the configuration object.
        /// </summary>
        /// <returns>Git configuration.</returns>
        IGitConfiguration GetConfiguration();

        /// <summary>
        /// Run a Git helper process which expects and returns key-value maps
        /// </summary>
        /// <param name="args">Arguments to the executable</param>
        /// <param name="standardInput">key-value map to pipe into stdin</param>
        /// <returns>stdout from helper executable as key-value map</returns>
        Task<IDictionary<string, string>> InvokeHelperAsync(string args, IDictionary<string, string> standardInput);
    }

    public class GitRemote
    {
        public GitRemote(string name, string fetchUrl, string pushUrl)
        {
            Name = name;
            FetchUrl = fetchUrl;
            PushUrl = pushUrl;
        }

        public string Name { get; }
        public string FetchUrl { get; }
        public string PushUrl { get; }
    }

    public class GitProcess : IGit
    {
        private readonly ITrace _trace;
        private readonly IEnvironment _environment;
        private readonly string _gitPath;
        private readonly string _workingDirectory;

        public GitProcess(ITrace trace, IEnvironment environment, string gitPath, string workingDirectory = null)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(environment, nameof(environment));
            EnsureArgument.NotNullOrWhiteSpace(gitPath, nameof(gitPath));

            _trace = trace;
            _environment = environment;
            _gitPath = gitPath;
            _workingDirectory = workingDirectory;
        }

        private GitVersion _version;
        public GitVersion Version
        {
            get
            {
                if (_version is null)
                {
                    using (var git = CreateProcess("version"))
                    {
                        git.Start();

                        string data = git.StandardOutput.ReadToEnd();
                        git.WaitForExit();

                        Match match = Regex.Match(data, @"^git version (?'value'.*)");
                        if (match.Success)
                        {
                            _version = new GitVersion(match.Groups["value"].Value);
                        }
                        else
                        {
                            _version = new GitVersion();
                        }
                    }
                }

                return _version;
            }
        }

        public IGitConfiguration GetConfiguration()
        {
            return new GitProcessConfiguration(_trace, this);
        }

        public string GetCurrentRepository()
        {
            using (var git = CreateProcess("rev-parse --absolute-git-dir"))
            {
                git.Start();
                // To avoid deadlocks, always read the output stream first and then wait
                // TODO: don't read in all the data at once; stream it
                string data = git.StandardOutput.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        return data.TrimEnd();
                    case 128: // Not inside a Git repository
                        return null;
                    default:
                        _trace.WriteLine($"Failed to get current Git repository (exit={git.ExitCode})");
                        throw CreateGitException(git, "Failed to get current Git repository");
                }
            }
        }

        public IEnumerable<GitRemote> GetRemotes()
        {
            using (var git = CreateProcess("remote -v show"))
            {
                git.Start();
                // To avoid deadlocks, always read the output stream first and then wait
                // TODO: don't read in all the data at once; stream it
                string data = git.StandardOutput.ReadToEnd();
                string stderr = git.StandardError.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    case 128 when stderr.Contains("not a git repository"): // Not inside a Git repository
                        yield break;
                    default:
                        _trace.WriteLine($"Failed to enumerate Git remotes (exit={git.ExitCode})");
                        throw CreateGitException(git, "Failed to enumerate Git remotes");
                }

                string[] lines = data.Split('\n');

                // Remotes are always output in groups of two (fetch and push)
                for (int i = 0; i + 1 < lines.Length; i += 2)
                {
                    // The fetch URL is written first, followed by the push URL
                    string[] fetchLine = lines[i].Split();
                    string[] pushLine = lines[i + 1].Split();

                    // Remote name is always first (and should match between fetch/push)
                    string remoteName = fetchLine[0];

                    // The next part, if present, is the URL
                    string fetchUrl = null;
                    string pushUrl = null;
                    if (fetchLine.Length > 1 && !string.IsNullOrWhiteSpace(fetchLine[1])) fetchUrl = fetchLine[1].TrimEnd();
                    if (pushLine.Length > 1 && !string.IsNullOrWhiteSpace(pushLine[1]))   pushUrl  = pushLine[1].TrimEnd();

                    yield return new GitRemote(remoteName, fetchUrl, pushUrl);
                }
            }
        }

        public Process CreateProcess(string args)
        {
            return _environment.CreateProcess(_gitPath, args, false, _workingDirectory);
        }

        // This code was originally copied from
        // src/shared/Microsoft.Git.CredentialManager/Authentication/AuthenticationBase.cs
        // That code is for GUI helpers in this codebase, while the below is for
        // communicating over Git's stdin/stdout helper protocol. The GUI helper
        // protocol will one day use a different IPC mechanism, whereas this code
        // has to follow what upstream Git does.
        public async Task<IDictionary<string, string>> InvokeHelperAsync(string args, IDictionary<string, string> standardInput = null)
        {
            var procStartInfo = new ProcessStartInfo(_gitPath)
            {
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
                UseShellExecute = false
            };

            var process = Process.Start(procStartInfo);
            if (process is null)
            {
                throw new Exception($"Failed to start Git helper '{args}'");
            }

            if (!(standardInput is null))
            {
                await process.StandardInput.WriteDictionaryAsync(standardInput);
                // some helpers won't continue until they see EOF
                // cf git-credential-cache
                process.StandardInput.Close();
            }

            IDictionary<string, string> resultDict = await process.StandardOutput.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);

            await Task.Run(() => process.WaitForExit());
            int exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                if (!resultDict.TryGetValue("error", out string errorMessage))
                {
                    errorMessage = "Unknown";
                }

                throw new Exception($"helper error ({exitCode}): {errorMessage}");
            }

            return resultDict;
        }

        public static GitException CreateGitException(Process git, string message)
        {
            string gitMessage = git.StandardError.ReadToEnd();
            throw new GitException(message, gitMessage, git.ExitCode);
        }
    }

    public class GitException : Exception
    {
        public string GitErrorMessage { get; }

        public int ExitCode { get; }

        public GitException(string message, string gitErrorMessage, int exitCode)
            : base(message)
        {
            GitErrorMessage = gitErrorMessage;
            ExitCode = exitCode;
        }
    }

    public static class GitExtensions
    {
        /// <summary>
        /// Returns true if the current Git instance is scoped to a local repository.
        /// </summary>
        /// <param name="git">Git object.</param>
        /// <returns>True if inside a local Git repository, false otherwise.</returns>
        public static bool IsInsideRepository(this IGit git)
        {
            return !string.IsNullOrWhiteSpace(git.GetCurrentRepository());
        }
    }
}
