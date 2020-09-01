// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
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
        public CommandContext()
        {
            Streams = new StandardStreams();
            Trace   = new Trace();

            if (PlatformUtils.IsWindows())
            {
                FileSystem        = new WindowsFileSystem();
                SessionManager    = new WindowsSessionManager();
                SystemPrompts     = new WindowsSystemPrompts();
                Environment       = new WindowsEnvironment(FileSystem);
                Terminal          = new WindowsTerminal(Trace);
                Git               = new GitProcess(
                                            Trace,
                                            Environment.LocateExecutable("git.exe"),
                                            FileSystem.GetCurrentDirectory()
                                        );
                Settings          = new Settings(Environment, Git);
                CredentialStore   = WindowsCredentialManager.Open();
            }
            else if (PlatformUtils.IsMacOS())
            {
                FileSystem        = new MacOSFileSystem();
                SessionManager    = new MacOSSessionManager();
                SystemPrompts     = new MacOSSystemPrompts();
                Environment       = new PosixEnvironment(FileSystem);
                Terminal          = new PosixTerminal(Trace);
                Git               = new GitProcess(
                                            Trace,
                                            Environment.LocateExecutable("git"),
                                            FileSystem.GetCurrentDirectory()
                                        );
                Settings          = new Settings(Environment, Git);
                CredentialStore   = MacOSKeychain.Open(Constants.CredentialNamespace);
            }
            else if (PlatformUtils.IsLinux())
            {
                FileSystem        = new LinuxFileSystem();
                // TODO: support more than just 'Posix' or X11
                SessionManager    = new PosixSessionManager();
                SystemPrompts     = new LinuxSystemPrompts();
                Environment       = new PosixEnvironment(FileSystem);
                Terminal          = new PosixTerminal(Trace);
                Git               = new GitProcess(
                                            Trace,
                                            Environment.LocateExecutable("git"),
                                            FileSystem.GetCurrentDirectory()
                                        );
                Settings          = new Settings(Environment, Git);
                CredentialStore   = new LinuxCredentialStore(Settings, Git);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            HttpClientFactory = new HttpClientFactory(Trace, Settings, Streams);

            // Set the parent window handle/ID
            SystemPrompts.ParentWindowId = Settings.ParentWindowId;
        }

        #region ICommandContext

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
