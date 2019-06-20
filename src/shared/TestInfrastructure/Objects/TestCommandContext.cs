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
        public string StdIn { get; set; } = string.Empty;
        public StringBuilder StdOut { get; set; } = new StringBuilder();
        public StringBuilder StdError { get; set; } = new StringBuilder();
        public TestTerminal Terminal { get; set; } = new TestTerminal();
        public ITrace Trace { get; set; } = new NullTrace();
        public TestFileSystem FileSystem { get; set; } = new TestFileSystem();
        public TestCredentialStore CredentialStore { get; set; } = new TestCredentialStore();
        public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
        public string NewLine { get; set; } = "\n";

        #region ICommandContext

        TextReader ICommandContext.StdIn => new StringReader(StdIn);

        TextWriter ICommandContext.StdOut => new StringWriter(StdOut){NewLine = NewLine};

        TextWriter ICommandContext.StdError => new StringWriter(StdError){NewLine = NewLine};

        ITerminal ICommandContext.Terminal => Terminal;

        ITrace ICommandContext.Trace => Trace;

        IFileSystem ICommandContext.FileSystem => FileSystem;

        ICredentialStore ICommandContext.CredentialStore => CredentialStore;

        IReadOnlyDictionary<string, string> ICommandContext.EnvironmentVariables
            => new ReadOnlyDictionary<string, string>(EnvironmentVariables);

        #endregion
    }
}
