// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.Interop;
using Microsoft.Git.CredentialManager.Interop.MacOS;
using Microsoft.Git.CredentialManager.Interop.Posix;
using Microsoft.Git.CredentialManager.Interop.Windows;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents the execution environment for a Git credential helper command.
    /// </summary>
    public interface ICommandContext
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
    }

    /// <summary>
    /// Real command execution environment using the actual <see cref="Console"/>, file system calls and environment.
    /// </summary>
    public class CommandContext : ICommandContext
    {
        public CommandContext()
        {
            Streams = new StandardStreams();
            Trace = new Trace();
            FileSystem = new FileSystem();

            var git = new LibGit2();
            var envars = new EnvironmentVariables(Environment.GetEnvironmentVariables());
            Settings = new Settings(envars, git)
            {
                RepositoryPath = git.GetRepositoryPath(FileSystem.GetCurrentDirectory())
            };

            HttpClientFactory = new HttpClientFactory(Trace, Settings, Streams);

            if (PlatformUtils.IsWindows())
            {
                Terminal = new WindowsTerminal(Trace);
                CredentialStore = WindowsCredentialManager.Open();
            }
            else if (PlatformUtils.IsPosix())
            {
                Terminal = new PosixTerminal(Trace);

                if (PlatformUtils.IsMacOS())
                {
                    CredentialStore = MacOSKeychain.Open();
                }
                else if (PlatformUtils.IsLinux())
                {
                    throw new NotImplementedException();
                }
            }
        }

        #region ICommandContext

        public ISettings Settings { get; }

        public IStandardStreams Streams { get; }

        public ITerminal Terminal { get; }

        public ITrace Trace { get; }

        public IFileSystem FileSystem { get; }

        public ICredentialStore CredentialStore { get; }

        public IHttpClientFactory HttpClientFactory { get; }

        #endregion
    }
}
