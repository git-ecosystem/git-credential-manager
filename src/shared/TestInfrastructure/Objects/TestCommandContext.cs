using System;
using System.IO;

namespace GitCredentialManager.Tests.Objects
{
    public class TestCommandContext : ICommandContext
    {
        public TestCommandContext()
        {
            AppPath = PlatformUtils.IsWindows()
                ? @"C:\Program Files\Git Credential Manager Core\git-credential-manager.exe"
                : "/usr/local/bin/git-credential-manager";

            InstallDir = Path.GetDirectoryName(AppPath);

            Streams = new TestStandardStreams();
            Terminal = new TestTerminal();
            SessionManager = new TestSessionManager();
            Trace = new NullTrace();
            Trace2 = new NullTrace2();
            FileSystem = new TestFileSystem();
            CredentialStore = new TestCredentialStore();
            HttpClientFactory = new TestHttpClientFactory();
            Git = new TestGit();
            Environment = new TestEnvironment(FileSystem);

            Settings = new TestSettings {Environment = Environment, GitConfiguration = Git.Configuration};
        }

        public string AppPath { get; set; }
        public string InstallDir { get; set; }
        public TestSettings Settings { get; set; }
        public TestStandardStreams Streams { get; set; }
        public TestTerminal Terminal { get; set; }
        public TestSessionManager SessionManager { get; set; }
        public ITrace Trace { get; set; }
        public ITrace2 Trace2 { get; set; }
        public TestFileSystem FileSystem { get; set; }
        public TestCredentialStore CredentialStore { get; set; }
        public TestHttpClientFactory HttpClientFactory { get; set; }
        public TestGit Git { get; set; }
        public TestEnvironment Environment { get; set; }

        public IProcessManager ProcessManager { get; set; }

        #region ICommandContext

        string ICommandContext.ApplicationPath
        {
            get => AppPath;
            set => AppPath = value;
        }

        string ICommandContext.InstallationDirectory => InstallDir;

        IStandardStreams ICommandContext.Streams => Streams;

        ISettings ICommandContext.Settings => Settings;

        ITerminal ICommandContext.Terminal => Terminal;

        ISessionManager ICommandContext.SessionManager => SessionManager;

        ITrace ICommandContext.Trace => Trace;

        IFileSystem ICommandContext.FileSystem => FileSystem;

        ICredentialStore ICommandContext.CredentialStore => CredentialStore;

        IHttpClientFactory ICommandContext.HttpClientFactory => HttpClientFactory;

        IGit ICommandContext.Git => Git;

        IEnvironment ICommandContext.Environment => Environment;

        #endregion

        #region IDisposable

        void IDisposable.Dispose() { }

        #endregion
    }
}
