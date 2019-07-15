// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestSettings : ISettings
    {
        public bool IsDebuggingEnabled { get; set; }

        public bool IsTerminalPromptsEnabled { get; set; } = true;

        public string Trace { get; set; }

        public bool IsSecretTracingEnabled { get; set; }

        public bool IsMsalTracingEnabled { get; set; }

        #region ISettings

        public string RepositoryPath { get; set; }

        public Uri RemoteUri { get; set; }

        bool ISettings.IsDebuggingEnabled => IsDebuggingEnabled;

        bool ISettings.IsTerminalPromptsEnabled => IsTerminalPromptsEnabled;

        bool ISettings.GetTracingEnabled(out string value)
        {
            value = Trace;
            return Trace != null;
        }

        bool ISettings.IsSecretTracingEnabled => IsSecretTracingEnabled;

        bool ISettings.IsMsalTracingEnabled => IsMsalTracingEnabled;

        #endregion
    }
}
