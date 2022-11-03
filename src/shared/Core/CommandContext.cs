using System;
using System.IO;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Interop.MacOS;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Windows;

namespace GitCredentialManager
{
    /// <summary>
    /// Represents the execution environment for a Git credential helper command.
    /// </summary>
    public interface ICommandContext : IDisposable
    {
        /// <summary>
        /// Absolute path the application entry executable.
        /// </summary>
        string ApplicationPath { get; }

        /// <summary>
        /// Settings and configuration for Git Credential Manager.
        /// </summary>
        ISettings Settings { get; }

        /// <summary>
        /// Standard I/O text streams, typically connected to the parent Git process.
        /// </summary>
        IStandardStreams Streams { get; }

        /// <summary>
        /// The attached terminal (TTY) to this process tree.
        /// </summary>
        ITerminal Terminal { get; }

        /// <summary>
        /// Provides services regarding user sessions.
        /// </summary>
        ISessionManager SessionManager { get; }

        /// <summary>
        /// Application tracing system.
        /// </summary>
        ITrace Trace { get; }

        /// <summary>
        /// File system abstraction (exists mainly for testing).
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// Secure credential storage.
        /// </summary>
        ICredentialStore CredentialStore { get; }

        /// <summary>
        /// Factory for creating new <see cref="System.Net.Http.HttpClient"/> instances.
        /// </summary>
        IHttpClientFactory HttpClientFactory { get; }

        /// <summary>
        /// Component for interacting with Git.
        /// </summary>
        IGit Git { get; }

        /// <summary>
        /// The current process environment.
        /// </summary>
        IEnvironment Environment { get; }
    }

    /// <summary>
    /// Real command execution environment using the actual <see cref="Console"/>, file system calls and environment.
    /// </summary>
    public class CommandContext : DisposableObject, ICommandContext
    {
        public CommandContext(string appPath)
        {
            EnsureArgument.NotNullOrWhiteSpace(appPath, nameof (appPath));

            ApplicationPath = appPath;
            Streams = new StandardStreams();
            Trace   = new Trace();

            if (PlatformUtils.IsWindows())
            {
                FileSystem        = new WindowsFileSystem();
                SessionManager    = new WindowsSessionManager();
                Environment       = new WindowsEnvironment(FileSystem);
                Terminal          = new WindowsTerminal(Trace);
                string gitPath    = GetGitPath(Environment, FileSystem, Trace);
                Git               = new GitProcess(
                                            Trace,
                                            Environment,
                                            gitPath,
                                            FileSystem.GetCurrentDirectory()
                                        );
                Settings          = new WindowsSettings(Environment, Git, Trace);
            }
            else if (PlatformUtils.IsMacOS())
            {
                FileSystem        = new MacOSFileSystem();
                SessionManager    = new MacOSSessionManager();
                Environment       = new MacOSEnvironment(FileSystem);
                Terminal          = new MacOSTerminal(Trace);
                string gitPath    = GetGitPath(Environment, FileSystem, Trace);
                Git               = new GitProcess(
                                            Trace,
                                            Environment,
                                            gitPath,
                                            FileSystem.GetCurrentDirectory()
                                        );
                Settings          = new Settings(Environment, Git);
            }
            else if (PlatformUtils.IsLinux())
            {
                FileSystem        = new LinuxFileSystem();
                // TODO: support more than just 'Posix' or X11
                SessionManager    = new PosixSessionManager();
                Environment       = new PosixEnvironment(FileSystem);
                Terminal          = new LinuxTerminal(Trace);
                string gitPath    = GetGitPath(Environment, FileSystem, Trace);
                Git               = new GitProcess(
                                            Trace,
                                            Environment,
                                            gitPath,
                                            FileSystem.GetCurrentDirectory()
                                        );
                Settings          = new Settings(Environment, Git);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            HttpClientFactory = new HttpClientFactory(FileSystem, Trace, Settings, Streams);
            CredentialStore   = new CredentialStore(this);
        }

        private static string GetGitPath(IEnvironment environment, IFileSystem fileSystem, ITrace trace)
        {
            const string unixGitName = "git";
            const string winGitName = "git.exe";

            string gitExecPath;
            string programName = PlatformUtils.IsWindows() ? winGitName : unixGitName;

            // Use the GIT_EXEC_PATH environment variable if set
            if (environment.Variables.TryGetValue(Constants.EnvironmentVariables.GitExecutablePath,
                out gitExecPath))
            {
                // If we're invoked from WSL we must locate the UNIX Git executable
                if (PlatformUtils.IsWindows() && WslUtils.IsWslPath(gitExecPath))
                {
                    programName = unixGitName;
                }

                string candidatePath = Path.Combine(gitExecPath, programName);
                if (fileSystem.FileExists(candidatePath))
                {
                    trace.WriteLine($"Using Git executable from GIT_EXEC_PATH: {candidatePath}");
                    return candidatePath;
                }
            }

            // Otherwise try to locate the git(.exe) on the current PATH
            gitExecPath = environment.LocateExecutable(programName);
            trace.WriteLine($"Using PATH-located Git executable: {gitExecPath}");
            return gitExecPath;
        }

        #region ICommandContext

        public string ApplicationPath { get; }

        public ISettings Settings { get; }

        public IStandardStreams Streams { get; }

        public ITerminal Terminal { get; }

        public ISessionManager SessionManager { get; }

        public ITrace Trace { get; }

        public IFileSystem FileSystem { get; }

        public ICredentialStore CredentialStore { get; }

        public IHttpClientFactory HttpClientFactory { get; }

        public IGit Git { get; }

        public IEnvironment Environment { get; }

        #endregion

        #region IDisposable

        protected override void ReleaseManagedResources()
        {
            Settings?.Dispose();
            Trace?.Dispose();

            base.ReleaseManagedResources();
        }

        #endregion
    }
}
