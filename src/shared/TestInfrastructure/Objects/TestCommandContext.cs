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
        public TestSettings Settings { get; set; } = new TestSettings();
        public TestStandardStreams Streams { get; set; } = new TestStandardStreams();
        public TestTerminal Terminal { get; set; } = new TestTerminal();
        public bool IsDesktopSession { get; set; } = true;
        public ITrace Trace { get; set; } = new NullTrace();
        public TestFileSystem FileSystem { get; set; } = new TestFileSystem();
        public TestCredentialStore CredentialStore { get; set; } = new TestCredentialStore();
        public TestHttpClientFactory HttpClientFactory { get; set; } = new TestHttpClientFactory();
        public TestGit Git { get; set; } = new TestGit();
        public TestEnvironment Environment { get; set; } = new TestEnvironment();
        public TestSystemPrompts SystemPrompts { get; set; } = new TestSystemPrompts();

        #region ICommandContext

        IStandardStreams ICommandContext.Streams => Streams;

        ISettings ICommandContext.Settings => Settings;

        ITerminal ICommandContext.Terminal => Terminal;

        bool ICommandContext.IsDesktopSession => IsDesktopSession;

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
