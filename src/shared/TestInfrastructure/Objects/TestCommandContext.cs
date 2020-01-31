// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestCommandContext : ICommandContext
    {
        public TestCommandContext()
        {
            Streams = new TestStandardStreams();
            Terminal = new TestTerminal();
            SessionManager = new TestSessionManager();
            Trace = new NullTrace();
            FileSystem = new TestFileSystem();
            CredentialStore = new TestCredentialStore();
            HttpClientFactory = new TestHttpClientFactory();
            Git = new TestGit();
            Environment = new TestEnvironment();
            SystemPrompts = new TestSystemPrompts();

            Settings = new TestSettings {Environment = Environment, GitConfiguration = Git.GlobalConfiguration};
        }

        public TestSettings Settings { get; set; }
        public TestStandardStreams Streams { get; set; }
        public TestTerminal Terminal { get; set; }
        public TestSessionManager SessionManager { get; set; }
        public ITrace Trace { get; set; }
        public TestFileSystem FileSystem { get; set; }
        public TestCredentialStore CredentialStore { get; set; }
        public TestHttpClientFactory HttpClientFactory { get; set; }
        public TestGit Git { get; set; }
        public TestEnvironment Environment { get; set; }
        public TestSystemPrompts SystemPrompts { get; set; }

        #region ICommandContext

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

        ISystemPrompts ICommandContext.SystemPrompts => SystemPrompts;

        #endregion

        #region IDisposable

        void IDisposable.Dispose() { }

        #endregion
    }
}
