using System;
using System.IO;
using Microsoft.Git.CredentialManager.Interop.Linux;
using Microsoft.Git.CredentialManager.Interop.MacOS;
using Microsoft.Git.CredentialManager.Interop.Posix;
using Microsoft.Git.CredentialManager.Interop.Windows;

namespace Microsoft.Git.CredentialManager
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

        /// <summary>
        /// Native UI prompts.
        /// </summary>
        ISystemPrompts SystemPrompts { get; }
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
                SystemPrompts     = new WindowsSystemPrompts();
                Environment       = new WindowsEnvironment(FileSystem);
                Terminal          = new WindowsTerminal(Trace);
                string gitPath    = GetGitPath(Environment, FileSystem, Trace);
                Git               = new GitProcess(
                                            Trace,
                                            gitPath,
                                            FileSystem.GetCurrentDirectory()
                                        );
                Settings          = new WindowsSettings(Environment, Git, Trace);
                CredentialStore   = new WindowsCredentialManager(Settings.CredentialNamespace);
            }
            else if (PlatformUtils.IsMacOS())
            {
                FileSystem        = new MacOSFileSystem();
                SessionManager    = new MacOSSessionManager();
                SystemPrompts     = new MacOSSystemPrompts();
                Environment       = new PosixEnvironment(FileSystem);
                Terminal          = new PosixTerminal(Trace);
                string gitPath    = GetGitPath(Environment, FileSystem, Trace);
                Git               = new GitProcess(
                                            Trace,
                                            gitPath,
                                            FileSystem.GetCurrentDirectory()
                                        );
                Settings          = new Settings(Environment, Git);
                CredentialStore   = new MacOSKeychain(Settings.CredentialNamespace);
            }
            else if (PlatformUtils.IsLinux())
            {
                FileSystem        = new LinuxFileSystem();
                // TODO: support more than just 'Posix' or X11
                SessionManager    = new PosixSessionManager();
                SystemPrompts     = new LinuxSystemPrompts();
                Environment       = new PosixEnvironment(FileSystem);
                Terminal          = new PosixTerminal(Trace);
                string gitPath    = GetGitPath(Environment, FileSystem, Trace);
                Git               = new GitProcess(
                                            Trace,
                                            gitPath,
                                            FileSystem.GetCurrentDirectory()
                                        );
                Settings          = new Settings(Environment, Git);
                string gpgPath    = GetGpgPath(Environment, FileSystem, Trace);
                IGpg gpg          = new Gpg(gpgPath, SessionManager);
                CredentialStore   = new LinuxCredentialStore(FileSystem, Settings, SessionManager, gpg, Environment, Git);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            HttpClientFactory = new HttpClientFactory(FileSystem, Trace, Settings, Streams);

            // Set the parent window handle/ID
            SystemPrompts.ParentWindowId = Settings.ParentWindowId;
        }

        private static string GetGitPath(IEnvironment environment, IFileSystem fileSystem, ITrace trace)
        {
            string gitExecPath;
            string programName = PlatformUtils.IsWindows() ? "git.exe" : "git";

            // Use the GIT_EXEC_PATH environment variable if set
            if (environment.Variables.TryGetValue(Constants.EnvironmentVariables.GitExecutablePath,
                out gitExecPath))
            {
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

        private static string GetGpgPath(IEnvironment environment, IFileSystem fileSystem, ITrace trace)
        {
            string gpgPath;

            // Use the GCM_GPG_PATH environment variable if set
            if (environment.Variables.TryGetValue(Constants.EnvironmentVariables.GpgExecutablePath,
                out gpgPath))
            {
                if (fileSystem.FileExists(gpgPath))
                {
                    trace.WriteLine($"Using Git executable from GCM_GPG_PATH: {gpgPath}");
                    return gpgPath;
                }
                else
                {
                    throw new Exception($"GPG executable does not exist with path '{gpgPath}'");
                }

            }

            // If no explicit GPG path is specified, mimic the way `pass`
            // determines GPG dependency (use gpg2 if available, otherwise gpg)
            if (environment.TryLocateExecutable("gpg2", out string gpg2Path))
            {
                trace.WriteLine($"Using PATH-located GPG (gpg2) executable: {gpg2Path}");
                return gpg2Path;
            }
            else
            {
                gpgPath = environment.LocateExecutable("gpg");
                trace.WriteLine($"Using PATH-located GPG (gpg) executable: {gpgPath}");
                return gpgPath;
            }
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

        public ISystemPrompts SystemPrompts { get; }

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
