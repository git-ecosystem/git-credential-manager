// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Git.CredentialManager.Interop;
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
        public CommandContext()
        {
            Streams = new StandardStreams();
            Trace   = new Trace();
            Git     = new LibGit2(Trace);

            if (PlatformUtils.IsWindows())
            {
                FileSystem      = new WindowsFileSystem();
                Environment     = new WindowsEnvironment(FileSystem);
                Terminal        = new WindowsTerminal(Trace);
                CredentialStore = WindowsCredentialManager.Open();
            }
            else if (PlatformUtils.IsPosix())
            {
                if (PlatformUtils.IsMacOS())
                {
                    FileSystem      = new MacOSFileSystem();
                    CredentialStore = MacOSKeychain.Open();
                }
                else if (PlatformUtils.IsLinux())
                {
                    throw new NotImplementedException();
                }

                Environment = new PosixEnvironment(FileSystem);
                Terminal    = new PosixTerminal(Trace);
            }

            string repoPath   = Git.GetRepositoryPath(FileSystem.GetCurrentDirectory());
            Settings          = new Settings(Environment, Git, repoPath);
            HttpClientFactory = new HttpClientFactory(Trace, Settings, Streams);
        }

        #region ICommandContext

        public ISettings Settings { get; }

        public IStandardStreams Streams { get; }

        public ITerminal Terminal { get; }

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
            Git?.Dispose();
            Trace?.Dispose();

            base.ReleaseManagedResources();
        }

        #endregion
    }
}
