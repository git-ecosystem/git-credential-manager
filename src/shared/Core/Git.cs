using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitCredentialManager
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
        ChildProcess CreateProcess(string args);

        /// <summary>
        /// Returns true if the current Git instance is scoped to a local repository.
        /// </summary>
        /// <returns>True if inside a local Git repository, false otherwise.</returns>
        bool IsInsideRepository();

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
        private readonly ITrace2 _trace2;
        private readonly IProcessManager _processManager;
        private readonly string _gitPath;
        private readonly string _workingDirectory;

        public GitProcess(ITrace trace, ITrace2 trace2, IProcessManager processManager, string gitPath, string workingDirectory = null)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(trace2, nameof(trace2));
            EnsureArgument.NotNull(processManager, nameof(processManager));
            EnsureArgument.NotNullOrWhiteSpace(gitPath, nameof(gitPath));

            _trace = trace;
            _trace2 = trace2;
            _processManager = processManager;
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
                        git.Start(Trace2ProcessClass.Git);

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

        public bool IsInsideRepository()
        {
            return !string.IsNullOrWhiteSpace(GetCurrentRepositoryInternal(suppressStreams: true));
        }

        public string GetCurrentRepository()
        {
            return GetCurrentRepositoryInternal(suppressStreams: false);
        }

        private string GetCurrentRepositoryInternal(bool suppressStreams)
        {
            using (var git = CreateProcess("rev-parse --absolute-git-dir"))
            {
                // Redirect standard error to ensure any error messages are captured and not exposed to the user's console
                if (suppressStreams)
                {
                    git.StartInfo.RedirectStandardError = true;
                }

                git.Start(Trace2ProcessClass.Git);
                string data = git.StandardOutput.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        return data.TrimEnd();
                    case 128: // Not inside a Git repository
                        return null;
                    default:
                        var message = "Failed to get current Git repository";
                        _trace.WriteLine($"{message} (exit={git.ExitCode})");
                        throw CreateGitException(git, message, _trace2);
                }
            }
        }

        public IEnumerable<GitRemote> GetRemotes()
        {
            using (var git = CreateProcess("remote -v show"))
            {
                git.Start(Trace2ProcessClass.Git);
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
                        var message = "Failed to enumerate Git remotes";
                        _trace.WriteLine($"{message} (exit={git.ExitCode})");
                        throw CreateGitException(git, message, _trace2);
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

        public ChildProcess CreateProcess(string args)
        {
            return _processManager.CreateProcess(_gitPath, args, false, _workingDirectory);
        }

        // This code was originally copied from
        // src/shared/Core/Authentication/AuthenticationBase.cs
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

            var process = _processManager.CreateProcess(procStartInfo);
            if (!process.Start(Trace2ProcessClass.Git))
            {
                var format = "Failed to start Git helper '{0}'";
                var message = string.Format(format, args);
                throw new Trace2Exception(_trace2, message, format);
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

        public static GitException CreateGitException(ChildProcess git, string message, ITrace2 trace2 = null)
        {
            var gitMessage = git.StandardError.ReadToEnd();

            if (trace2 != null)
                throw new Trace2GitException(trace2, message, git.ExitCode, gitMessage);

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
    }
}
