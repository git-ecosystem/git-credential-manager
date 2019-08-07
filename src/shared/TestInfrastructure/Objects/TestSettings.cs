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

        public string ProviderOverride { get; set; }

        public string LegacyAuthorityOverride { get; set; }

        public bool IsWindowsIntegratedAuthenticationEnabled { get; set; } = true;

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

        string ISettings.ProviderOverride => ProviderOverride;

        string ISettings.LegacyAuthorityOverride => LegacyAuthorityOverride;

        bool ISettings.IsWindowsIntegratedAuthenticationEnabled => IsWindowsIntegratedAuthenticationEnabled;

        #endregion
    }
}
