// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestCommandContext : ICommandContext
    {
        public TestSettings Settings { get; set; } = new TestSettings();
        public TestStandardStreams Streams { get; set; } = new TestStandardStreams();
        public TestTerminal Terminal { get; set; } = new TestTerminal();
        public ITrace Trace { get; set; } = new NullTrace();
        public TestFileSystem FileSystem { get; set; } = new TestFileSystem();
        public TestCredentialStore CredentialStore { get; set; } = new TestCredentialStore();
        public TestHttpClientFactory HttpClientFactory { get; set; } = new TestHttpClientFactory();
        public TestGit Git { get; set; } = new TestGit();
        public TestEnvironment Environment { get; set; } = new TestEnvironment();

        #region ICommandContext

        IStandardStreams ICommandContext.Streams => Streams;

        ISettings ICommandContext.Settings => Settings;

        ITerminal ICommandContext.Terminal => Terminal;

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
