// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Git.CredentialManager
{
    public static class Constants
    {
        public const string PersonalAccessTokenUserName = "PersonalAccessToken";
        public const string MicrosoftAuthHelperName = "Microsoft.Authentication.Helper";

        public static class EnvironmentVariables
        {
            public const string GcmTrace           = "GCM_TRACE";
            public const string GcmTraceSecrets    = "GCM_TRACE_SECRETS";
            public const string GcmTraceMsAuth     = "GCM_TRACE_MSAUTH";
            public const string GcmDebug           = "GCM_DEBUG";
            public const string GitTerminalPrompts = "GIT_TERMINAL_PROMPT";
        }

        public static class Http
        {
            public const string WwwAuthenticateBasicScheme     = "Basic";
            public const string WwwAuthenticateBearerScheme    = "Bearer";
            public const string WwwAuthenticateNegotiateScheme = "Negotiate";
            public const string WwwAuthenticateNtlmScheme      = "NTLM";

            public const string MimeTypeJson = "application/json";
        }

        private static string _gcmVersion;

        /// <summary>
        /// The current version of Git Credential Manager.
        /// </summary>
        public static string GcmVersion
        {
            get
            {
                if (_gcmVersion is null)
                {
                    _gcmVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
                }

                return _gcmVersion;
            }
        }

        /// <summary>
        /// Get standard program header title for Git Credential Manager, including the current version and OS information.
        /// </summary>
        /// <returns>Standard program header.</returns>
        public static string GetProgramHeader()
        {
            PlatformInformation info = PlatformUtils.GetPlatformInformation();

            return $"Git Credential Manager version {GcmVersion} ({info.OperatingSystemType}, {info.ClrVersion})";
        }

        /// <summary>
        /// Get the HTTP user-agent for Git Credential Manager.
        /// </summary>
        /// <returns>User-agent string for HTTP requests.</returns>
        public static string GetHttpUserAgent()
        {
            PlatformInformation info = PlatformUtils.GetPlatformInformation();
            string osType     = info.OperatingSystemType;
            string cpuArch    = info.CpuArchitecture;
            string clrVersion = info.ClrVersion;

            return string.Format($"Git-Credential-Manager/{GcmVersion} ({osType}; {cpuArch}) CLR/{clrVersion}");
        }
    }
}
