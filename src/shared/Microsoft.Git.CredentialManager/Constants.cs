// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Reflection;

namespace Microsoft.Git.CredentialManager
{
    public static class Constants
    {
        public const string PersonalAccessTokenUserName = "PersonalAccessToken";
        public const string DefaultCredentialNamespace = "git";

        public const string ProviderIdAuto  = "auto";
        public const string AuthorityIdAuto = "auto";

        public const string GcmDataDirectoryName = ".gcm";

        public static class RegexPatterns
        {
            /// <summary>
            /// A regular expression that matches any value.
            /// </summary>
            public const string Any = @".*";

            /// <summary>
            /// A regular expression that matches no value.
            /// </summary>
            public const string None = @"$.+";

            /// <summary>
            /// A regular expression that matches empty strings.
            /// </summary>
            public const string Empty = @"^$";
        }

        public static class EnvironmentVariables
        {
            public const string GcmTrace              = "GCM_TRACE";
            public const string GcmTraceSecrets       = "GCM_TRACE_SECRETS";
            public const string GcmTraceMsAuth        = "GCM_TRACE_MSAUTH";
            public const string GcmDebug              = "GCM_DEBUG";
            public const string GcmProvider           = "GCM_PROVIDER";
            public const string GcmAuthority          = "GCM_AUTHORITY";
            public const string GitTerminalPrompts    = "GIT_TERMINAL_PROMPT";
            public const string GcmAllowWia           = "GCM_ALLOW_WINDOWSAUTH";
            public const string CurlAllProxy          = "ALL_PROXY";
            public const string CurlHttpProxy         = "HTTP_PROXY";
            public const string CurlHttpsProxy        = "HTTPS_PROXY";
            public const string GcmHttpProxy          = "GCM_HTTP_PROXY";
            public const string GitSslNoVerify        = "GIT_SSL_NO_VERIFY";
            public const string GcmInteractive        = "GCM_INTERACTIVE";
            public const string GcmParentWindow       = "GCM_MODAL_PARENTHWND";
            public const string MsAuthFlow            = "GCM_MSAUTH_FLOW";
            public const string GcmCredNamespace      = "GCM_NAMESPACE";
            public const string GcmCredentialStore    = "GCM_CREDENTIAL_STORE";
            public const string GcmCredCacheOptions   = "GCM_CREDENTIAL_CACHE_OPTIONS";
            public const string GcmPlaintextStorePath = "GCM_PLAINTEXT_STORE_PATH";
            public const string GitExecutablePath     = "GIT_EXEC_PATH";
        }

        public static class Http
        {
            public const string WwwAuthenticateBasicScheme     = "Basic";
            public const string WwwAuthenticateBearerScheme    = "Bearer";
            public const string WwwAuthenticateNegotiateScheme = "Negotiate";
            public const string WwwAuthenticateNtlmScheme      = "NTLM";

            public const string MimeTypeJson = "application/json";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string SectionName = "credential";
                public const string Helper      = "helper";
                public const string Provider    = "provider";
                public const string Authority   = "authority";
                public const string AllowWia    = "allowWindowsAuth";
                public const string HttpProxy   = "httpProxy";
                public const string HttpsProxy  = "httpsProxy";
                public const string UseHttpPath = "useHttpPath";
                public const string Interactive = "interactive";
                public const string MsAuthFlow  = "msauthFlow";
                public const string CredNamespace = "namespace";
                public const string CredentialStore = "credentialStore";
                public const string CredCacheOptions = "cacheOptions";
                public const string PlaintextStorePath = "plaintextStorePath";
            }

            public static class Http
            {
                public const string SectionName = "http";
                public const string Proxy = "proxy";
                public const string SslVerify = "sslVerify";
            }
        }

        public static class HelpUrls
        {
            public const string GcmProjectUrl          = "https://aka.ms/gcmcore";
            public const string GcmAuthorityDeprecated = "https://aka.ms/gcmcore-authority";
            public const string GcmHttpProxyGuide      = "https://aka.ms/gcmcore-httpproxy";
            public const string GcmTlsVerification     = "https://aka.ms/gcmcore-tlsverify";
            public const string GcmLinuxCredStores     = "https://aka.ms/gcmcore-linuxcredstores";
        }

        private static Version _gcmVersion;

        public static Version GcmVersion
        {
            get
            {
                if (_gcmVersion is null)
                {
                    var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                    var attr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
                    if (attr is null)
                    {
                        _gcmVersion = assembly.GetName().Version;
                    }
                    else if (Version.TryParse(attr.Version, out Version asmVersion))
                    {
                        _gcmVersion = asmVersion;
                    }
                    else
                    {
                        // Unknown version!
                        _gcmVersion = new Version(0, 0);
                    }
                }

                return _gcmVersion;
            }
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
