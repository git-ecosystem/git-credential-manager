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

        #region ISettings

        bool ISettings.IsDebuggingEnabled => IsDebuggingEnabled;

        bool ISettings.IsTerminalPromptsEnabled => IsTerminalPromptsEnabled;

        bool ISettings.GetTracingEnabled(out string value)
        {
            value = Trace;
            return Trace != null;
        }

        bool ISettings.IsSecretTracingEnabled => IsSecretTracingEnabled;

        #endregion
    }
}
