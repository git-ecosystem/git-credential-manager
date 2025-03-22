using System;
using System.Reflection;

namespace GitCredentialManager
{
    public static class Constants
    {
        public const string DefaultWindowTitle = "Git Credential Manager";
        public const string PersonalAccessTokenUserName = "PersonalAccessToken";
        public const string DefaultCredentialNamespace = "git";
        public const int DefaultAutoDetectProviderTimeoutMs = 2000; // 2 seconds
        public const string DefaultUiHelper = "git-credential-manager-ui";

        public const string ProviderIdAuto  = "auto";
        public const string AuthorityIdAuto = "auto";

        public const string GcmDataDirectoryName = ".gcm";

        public static readonly Guid DevBoxPartnerId = new("e3171dd9-9a5f-e5be-b36c-cc7c4f3f3bcf");

        /// <summary>
        /// Home tenant ID for Microsoft Accounts (MSA).
        /// </summary>
        public static readonly Guid MsaHomeTenantId = new("9188040d-6c67-4c5b-b112-36a304b66dad");

        /// <summary>
        /// Special tenant ID for transferring between Microsoft Account (MSA) native tokens
        /// and AAD tokens. Only required for MSA-Passthrough applications.
        /// </summary>
        public static readonly Guid MsaTransferTenantId = new("f8cdef31-a31e-4b4a-93e4-5f571e91255a");

        public static class CredentialStoreNames
        {
            public const string WindowsCredentialManager = "wincredman";
            public const string Dpapi = "dpapi";
            public const string MacOSKeychain = "keychain";
            public const string Gpg = "gpg";
            public const string SecretService = "secretservice";
            public const string Plaintext = "plaintext";
            public const string Cache = "cache";
        }

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
            public const string GitTrace2Event        = "GIT_TRACE2_EVENT";
            public const string GitTrace2Normal       = "GIT_TRACE2";
            public const string GitTrace2Performance  = "GIT_TRACE2_PERF";

            /*
             * Unlike other environment variables, these proxy variables are normally lowercase only.
             * However, libcurl also implemented checks for the uppercase variants! The lowercase
             * variants should take precedence over the uppercase ones since the former are quasi-standard.
             *
             * One exception to this is that libcurl does not even look for the uppercase variant of
             * http_proxy (for some security reasons).
             */
            public const string CurlNoProxy           = "no_proxy";
            public const string CurlNoProxyUpper      = "NO_PROXY";
            public const string CurlHttpsProxy        = "https_proxy";
            public const string CurlHttpsProxyUpper   = "HTTPS_PROXY";
            public const string CurlHttpProxy         = "http_proxy";
            // Note there is no uppercase variant of the http_proxy since libcurl doesn't use it
            public const string CurlAllProxy          = "all_proxy";
            public const string CurlAllProxyUpper     = "ALL_PROXY";

            public const string GcmHttpProxy          = "GCM_HTTP_PROXY";
            public const string GitSslNoVerify        = "GIT_SSL_NO_VERIFY";
            public const string GitSslCaInfo          = "GIT_SSL_CAINFO";
            public const string GcmInteractive        = "GCM_INTERACTIVE";
            public const string GcmParentWindow       = "GCM_MODAL_PARENTHWND";
            public const string MsAuthFlow            = "GCM_MSAUTH_FLOW";
            public const string MsAuthUseBroker       = "GCM_MSAUTH_USEBROKER";
            public const string MsAuthUseDefaultAccount = "GCM_MSAUTH_USEDEFAULTACCOUNT";
            public const string GcmCredNamespace      = "GCM_NAMESPACE";
            public const string GcmCredentialStore    = "GCM_CREDENTIAL_STORE";
            public const string GcmCredCacheOptions   = "GCM_CREDENTIAL_CACHE_OPTIONS";
            public const string GcmPlaintextStorePath = "GCM_PLAINTEXT_STORE_PATH";
            public const string GcmDpapiStorePath     = "GCM_DPAPI_STORE_PATH";
            public const string GitExecutablePath     = "GIT_EXEC_PATH";
            public const string GpgExecutablePath     = "GCM_GPG_PATH";
            public const string GcmAutoDetectTimeout  = "GCM_AUTODETECT_TIMEOUT";
            public const string GcmGuiPromptsEnabled  = "GCM_GUI_PROMPT";
            public const string GcmUiHelper           = "GCM_UI_HELPER";
            public const string OAuthAuthenticationModes = "GCM_OAUTH_AUTHMODES";
            public const string OAuthClientId            = "GCM_OAUTH_CLIENTID";
            public const string OAuthClientSecret        = "GCM_OAUTH_CLIENTSECRET";
            public const string OAuthRedirectUri         = "GCM_OAUTH_REDIRECTURI";
            public const string OAuthScopes              = "GCM_OAUTH_SCOPES";
            public const string OAuthAuthzEndpoint       = "GCM_OAUTH_AUTHORIZE_ENDPOINT";
            public const string OAuthTokenEndpoint       = "GCM_OAUTH_TOKEN_ENDPOINT";
            public const string OAuthDeviceEndpoint      = "GCM_OAUTH_DEVICE_ENDPOINT";
            public const string OAuthClientAuthHeader    = "GCM_OAUTH_USE_CLIENT_AUTH_HEADER";
            public const string OAuthDefaultUserName     = "GCM_OAUTH_DEFAULT_USERNAME";
            public const string GcmDevUseLegacyUiHelpers = "GCM_DEV_USELEGACYUIHELPERS";
            public const string GcmGuiSoftwareRendering  = "GCM_GUI_SOFTWARE_RENDERING";
            public const string GcmAllowUnsafeRemotes    = "GCM_ALLOW_UNSAFE_REMOTES";
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
                public const string Trace       = "trace";
                public const string TraceSecrets = "traceSecrets";
                public const string TraceMsAuth = "traceMsAuth";
                public const string Debug       = "debug";
                public const string Provider    = "provider";
                public const string Authority   = "authority";
                public const string AllowWia    = "allowWindowsAuth";
                public const string HttpProxy   = "httpProxy";
                public const string HttpsProxy  = "httpsProxy";
                public const string UseHttpPath = "useHttpPath";
                public const string Interactive = "interactive";
                public const string MsAuthFlow  = "msauthFlow";
                public const string MsAuthUseBroker = "msauthUseBroker";
                public const string CredNamespace = "namespace";
                public const string CredentialStore = "credentialStore";
                public const string CredCacheOptions = "cacheOptions";
                public const string PlaintextStorePath = "plaintextStorePath";
                public const string DpapiStorePath = "dpapiStorePath";
                public const string UserName = "username";
                public const string AutoDetectTimeout = "autoDetectTimeout";
                public const string GuiPromptsEnabled = "guiPrompt";
                public const string UiHelper = "uiHelper";
                public const string DevUseLegacyUiHelpers = "devUseLegacyUiHelpers";
                public const string MsAuthUseDefaultAccount = "msauthUseDefaultAccount";
                public const string GuiSoftwareRendering = "guiSoftwareRendering";
                public const string GpgPassStorePath = "gpgPassStorePath";
                public const string AllowUnsafeRemotes = "allowUnsafeRemotes";

                public const string OAuthAuthenticationModes = "oauthAuthModes";
                public const string OAuthClientId            = "oauthClientId";
                public const string OAuthClientSecret        = "oauthClientSecret";
                public const string OAuthRedirectUri         = "oauthRedirectUri";
                public const string OAuthScopes              = "oauthScopes";
                public const string OAuthAuthzEndpoint       = "oauthAuthorizeEndpoint";
                public const string OAuthTokenEndpoint       = "oauthTokenEndpoint";
                public const string OAuthDeviceEndpoint      = "oauthDeviceEndpoint";
                public const string OAuthClientAuthHeader    = "oauthUseClientAuthHeader";
                public const string OAuthDefaultUserName     = "oauthDefaultUserName";
            }

            public static class Http
            {
                public const string SectionName = "http";
                public const string Proxy = "proxy";
                public const string SchannelUseSslCaInfo = "schannelUseSSLCAInfo";
                public const string SslBackend = "sslBackend";
                public const string SslVerify = "sslVerify";
                public const string SslCaInfo = "sslCAInfo";
                public const string SslAutoClientCert = "sslAutoClientCert";
                public const string CookieFile = "cookieFile";
            }

            public static class Remote
            {
                public const string SectionName = "remote";
                public const string FetchUrl = "url";
                public const string PushUrl = "pushUrl";
            }

            public static class Trace2
            {
                public const string SectionName       = "trace2";
                public const string EventTarget       = "eventtarget";
                public const string NormalTarget      = "normaltarget";
                public const string PerformanceTarget = "perftarget";
            }
        }

        public static class WindowsRegistry
        {
            public const string HKAppBasePath = @"SOFTWARE\GitCredentialManager";
            public const string HKConfigurationPath = HKAppBasePath + @"\Configuration";

            public const string HKWindows365Path = @"SOFTWARE\Microsoft\Windows365";
            public const string IsW365EnvironmentKeyName = "IsW365Environment";
            public const string W365PartnerIdKeyName = "PartnerId";
        }

        public static class HelpUrls
        {
            public const string GcmProjectUrl          = "https://aka.ms/gcm";
            public const string GcmNewIssue            = "https://aka.ms/gcm/bug";
            public const string GcmAuthorityDeprecated = "https://aka.ms/gcm/authority";
            public const string GcmHttpProxyGuide      = "https://aka.ms/gcm/httpproxy";
            public const string GcmTlsVerification     = "https://aka.ms/gcm/tlsverify";
            public const string GcmCredentialStores    = "https://aka.ms/gcm/credstores";
            public const string GcmWamComSecurity      = "https://aka.ms/gcm/wamadmin";
            public const string GcmAutoDetect          = "https://aka.ms/gcm/autodetect";
            public const string GcmDefaultAccount      = "https://aka.ms/gcm/defaultaccount";
            public const string GcmMultipleUsers       = "https://aka.ms/gcm/multipleusers";
            public const string GcmUnsafeRemotes       = "https://aka.ms/gcm/unsaferemotes";
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
        public static string GetHttpUserAgent(ITrace2 trace2)
        {
            PlatformInformation info = PlatformUtils.GetPlatformInformation(trace2);
            string osType     = info.OperatingSystemType;
            string cpuArch    = info.CpuArchitecture;
            string clrVersion = info.ClrVersion;

            return string.Format($"Git-Credential-Manager/{GcmVersion} ({osType}; {cpuArch}) CLR/{clrVersion}");
        }
    }
}
