using System;
using System.Linq;
using System.Net;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void Settings_IsDebuggingEnabled_EnvarUnset_ReturnsFalse()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsDebuggingEnabled);
        }

        [Fact]
        public void Settings_IsDebuggingEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmDebug] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsDebuggingEnabled);
        }

        [Fact]
        public void Settings_IsDebuggingEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmDebug] = "0"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsDebuggingEnabled);
        }

        [Fact]
        public void Settings_IsTerminalPromptsEnabled_EnvarUnset_ReturnsTrue()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsTerminalPromptsEnabled);
        }

        [Fact]
        public void Settings_IsTerminalPromptsEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GitTerminalPrompts] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsTerminalPromptsEnabled);
        }

        [Fact]
        public void Settings_IsTerminalPromptsEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GitTerminalPrompts] = "0"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsTerminalPromptsEnabled);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_EnvarUnset_ReturnsTrue()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmInteractive] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmInteractive] = "0"},
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigAuto_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {"auto"};

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigAlways_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {"always"};

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigNever_ReturnsFalse()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {"never"};

            var settings = new Settings(envars, git);

            Assert.False(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigTruthy_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {"1"};

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigFalsey_ReturnsFalse()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {"0"};

            var settings = new Settings(envars, git);

            Assert.False(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigNonBooleanyValue_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {Guid.NewGuid().ToString()};

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsTracingEnabled_EnvarUnset_ReturnsFalse()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.False(result);
        }

        [Fact]
        public void Settings_IsTracingEnabled_EnvarTruthy_ReturnsTrueOutValue()
        {
            const string expectedValue = "1";
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTrace] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_IsTracingEnabled_EnvarFalsey_ReturnsFalseOutValue()
        {
            const string expectedValue = "0";
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTrace] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.False(result);
            Assert.Equal(expectedValue, actualValue);
        }


        [Fact]
        public void Settings_IsTracingEnabled_EnvarPathy_ReturnsTrueOutValue()
        {
            const string expectedValue = "/tmp/gcm.log";
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTrace] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_IsSecretTracingEnabled_EnvarUnset_ReturnsFalse()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsSecretTracingEnabled);
        }

        [Fact]
        public void Settings_IsSecretTracingEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTraceSecrets] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsSecretTracingEnabled);
        }

        [Fact]
        public void Settings_IsSecretTracingEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTraceSecrets] = "0"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsSecretTracingEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_EnvarUnset_ReturnsTrue()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAllowWia] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAllowWia] = "0"},
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_EnvarNonBooleanyValue_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAllowWia] = Guid.NewGuid().ToString("N")},
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_ConfigUnset_ReturnsTrue()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_ConfigTruthy_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.AllowWia;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {"1"};

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_ConfigFalsey_ReturnsFalse()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.AllowWia;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {"0"};

            var settings = new Settings(envars, git);

            Assert.False(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_ConfigNonBooleanyValue_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.AllowWia;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {Guid.NewGuid().ToString()};

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Theory]
        [InlineData("", new string[0])]
        [InlineData("    ", new string[0])]
        [InlineData(",", new string[0])]
        [InlineData("example.com", new[] { @"(\.|\:\/\/)example\.com$" })]
        [InlineData("example.com:8080", new[] { @"(\.|\:\/\/)example\.com:8080$" })]
        [InlineData("example.com,", new[] { @"(\.|\:\/\/)example\.com$" })]
        [InlineData(",example.com", new[] { @"(\.|\:\/\/)example\.com$" })]
        [InlineData(",example.com,", new[] { @"(\.|\:\/\/)example\.com$" })]
        [InlineData(".example.com", new[] { @"(\.|\:\/\/)example\.com$" })]
        [InlineData("..example.com", new[] { @"(\.|\:\/\/)example\.com$" })]
        [InlineData("*.example.com", new[] { @"(\.|\:\/\/)example\.com$" })]
        [InlineData("my.example.com", new[] { @"(\.|\:\/\/)my\.example\.com$" })]
        [InlineData("example.com,contoso.com,fabrikam.com", new[]
        {
            @"(\.|\:\/\/)example\.com$",
            @"(\.|\:\/\/)contoso\.com$",
            @"(\.|\:\/\/)fabrikam\.com$"
        })]
        public void Settings_ProxyConfiguration_ConvertToBypassRegexArray(string input, string[] expected)
        {
            string[] actual = ProxyConfiguration.ConvertToBypassRegexArray(input).ToArray();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("example.com", "http://example.com", true)]
        [InlineData("example.com", "https://example.com", true)]
        [InlineData("example.com", "https://www.example.com", true)]
        [InlineData("example.com", "http://www.example.com:80", true)]
        [InlineData("example.com", "https://www.example.com:443", true)]
        [InlineData("example.com", "https://www.example.com:8080", false)]
        [InlineData("example.com", "http://notanexample.com", false)]
        [InlineData("example.com", "https://notanexample.com", false)]
        [InlineData("example.com", "https://www.notanexample.com", false)]
        [InlineData("example.com", "https://example.com.otherltd", false)]
        [InlineData("example.com:8080", "http://example.com", false)]
        [InlineData("my.example.com", "http://example.com", false)]
        public void Settings_ProxyConfiguration_ConvertToBypassRegexArray_WebProxyBypass(string noProxy, string address, bool expected)
        {
            var bypassList = ProxyConfiguration.ConvertToBypassRegexArray(noProxy).ToArray();
            var webProxy = new WebProxy("https://localhost:8080/proxy")
            {
                BypassList = bypassList
            };

            bool actual = webProxy.IsBypassed(new Uri(address));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Settings_ProxyConfiguration_Unset_ReturnsNull()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration config = settings.GetProxyConfiguration();

            Assert.Null(config);
        }

        [Fact]
        public void Settings_ProxyConfiguration_GcmHttpConfig_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.HttpProxy;
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var expectedNoProxy = "contoso.com,fabrikam.com";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.CurlNoProxy] = expectedNoProxy}
            };
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {settingValue.ToString()};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(expectedNoProxy, actualConfig.NoProxyRaw);
            Assert.True(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_GcmHttpsConfig_ReturnsValue()
        {
            const string remoteUrl = "https://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.HttpsProxy;
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var expectedNoProxy = "contoso.com,fabrikam.com";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.CurlNoProxy] = expectedNoProxy}
            };
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {settingValue.ToString()};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(expectedNoProxy, actualConfig.NoProxyRaw);
            Assert.True(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_GitHttpConfig_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Http.SectionName;
            const string property = Constants.GitConfiguration.Http.Proxy;
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var expectedNoProxy = "contoso.com,fabrikam.com";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.CurlNoProxy] = expectedNoProxy}
            };
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {settingValue.ToString()};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(expectedNoProxy, actualConfig.NoProxyRaw);
            Assert.False(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_GitHttpConfig_EmptyScopedUriUnscoped_ReturnsNull()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Http.SectionName;
            const string property = Constants.GitConfiguration.Http.Proxy;
            var remoteUri = new Uri(remoteUrl);

            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {settingValue.ToString()};
            git.Configuration.Global[$"{section}.{remoteUrl}.{property}"] = new[] {string.Empty};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.Null(actualConfig);
        }

        [Fact]
        public void Settings_ProxyConfiguration_CurlHttpEnvar_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var expectedNoProxy = "contoso.com,fabrikam.com";

            var envars = new TestEnvironment
            {
                Variables =
                {
                    [Constants.EnvironmentVariables.CurlHttpProxy] = settingValue.ToString(),
                    [Constants.EnvironmentVariables.CurlNoProxy] = expectedNoProxy
                }
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(expectedNoProxy, actualConfig.NoProxyRaw);
            Assert.False(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_CurlHttpsEnvar_ReturnsValue()
        {
            const string remoteUrl = "https://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var expectedNoProxy = "contoso.com,fabrikam.com";

            var envars = new TestEnvironment
            {
                Variables =
                {
                    [Constants.EnvironmentVariables.CurlHttpsProxy] = settingValue.ToString(),
                    [Constants.EnvironmentVariables.CurlNoProxy] = expectedNoProxy
                }
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(expectedNoProxy, actualConfig.NoProxyRaw);
            Assert.False(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_TryGetProxy_CurlAllEnvar_ReturnsValue()
        {
            const string remoteUrl = "https://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var expectedNoProxy = "contoso.com,fabrikam.com";

            var envars = new TestEnvironment
            {
                Variables =
                {
                    [Constants.EnvironmentVariables.CurlAllProxy] = settingValue.ToString(),
                    [Constants.EnvironmentVariables.CurlNoProxy] = expectedNoProxy
                }
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(expectedNoProxy, actualConfig.NoProxyRaw);
            Assert.False(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_LegacyGcmEnvar_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var expectedNoProxy = "contoso.com,fabrikam.com";

            var envars = new TestEnvironment
            {
                Variables =
                {
                    [Constants.EnvironmentVariables.GcmHttpProxy] = settingValue.ToString(),
                    [Constants.EnvironmentVariables.CurlNoProxy] = expectedNoProxy
                }
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(expectedNoProxy, actualConfig.NoProxyRaw);
            Assert.True(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_Precedence_ReturnsValue()
        {
            // 1. GCM proxy Git configuration (deprecated)
            //      credential.httpsProxy
            //      credential.httpProxy
            // 2. Standard Git configuration
            //      http.proxy
            // 3. cURL environment variables
            //      http_proxy
            //      HTTPS_PROXY
            //      http_proxy (note that uppercase HTTP_PROXY is not supported by libcurl)
            //      all_proxy
            //      ALL_PROXY
            // 4. GCM proxy environment variable (deprecated)
            //      GCM_HTTP_PROXY

            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            var value1 = new Uri("http://proxy1.example.com");
            var value2 = new Uri("http://proxy2.example.com");
            var value3 = new Uri("http://proxy3.example.com");
            var value4 = new Uri("http://proxy4.example.com");

            var envars = new TestEnvironment();
            var git = new TestGit();

            void RunTest(Uri expectedValue)
            {
                var settings = new Settings(envars, git)
                {
                    RemoteUri = remoteUri
                };
                ProxyConfiguration actualConfig = settings.GetProxyConfiguration();
                Assert.Equal(expectedValue, actualConfig.Address);
            }

             // Test case 1: cURL environment variables > GCM_HTTP_PROXY
            envars.Variables[Constants.EnvironmentVariables.GcmHttpProxy] = value1.ToString();
            envars.Variables[Constants.EnvironmentVariables.CurlHttpProxy] = value2.ToString();
            RunTest(value2);

            // Test case 2: http.proxy > cURL environment variables
            string httpProxyKey = $"{Constants.GitConfiguration.Http.SectionName}.{Constants.GitConfiguration.Http.Proxy}";
            git.Configuration.Global[httpProxyKey] = new[] {value3.ToString()};
            RunTest(value3);

            // Test case 3: credential.httpProxy > http.proxy
            string credentialProxyKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.HttpProxy}";
            git.Configuration.Global[credentialProxyKey] = new[] {value4.ToString()};
            RunTest(value4);
        }

        [Fact]
        public void Settings_ProxyConfiguration_CurlEnvarPrecedence_PrefersLowercase()
        {
            // Expected precedence:
            //  https_proxy
            //  HTTPS_PROXY
            //  http_proxy (note that uppercase HTTP_PROXY is not supported by libcurl)
            //  all_proxy
            //  ALL_PROXY

            const string remoteUrl = "https://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            var value1 = new Uri("http://proxy1.example.com");
            var value2 = new Uri("http://proxy2.example.com");

            // The differentiation between upper- and lowercase environment variables is not possible
            // on some platforms (Windows) so force the comparer to case-sensitive in the test so we
            // can run this on all platforms.
            var envars = new TestEnvironment(envarComparer: StringComparer.Ordinal);
            var git = new TestGit();

            void RunTest(Uri expectedValue)
            {
                var settings = new Settings(envars, git)
                {
                    RemoteUri = remoteUri
                };
                ProxyConfiguration actualConfig = settings.GetProxyConfiguration();
                Assert.Equal(expectedValue, actualConfig.Address);
            }

            // Test case 1: https_proxy > HTTPS_PROXY
            envars.Variables[Constants.EnvironmentVariables.CurlHttpsProxy] = value1.ToString();
            envars.Variables[Constants.EnvironmentVariables.CurlHttpsProxyUpper] = value2.ToString();
            RunTest(value1);

            // Test case 2a: https_proxy > http_proxy
            envars.Variables.Clear();
            envars.Variables[Constants.EnvironmentVariables.CurlHttpsProxy] = value1.ToString();
            envars.Variables[Constants.EnvironmentVariables.CurlHttpProxy] = value2.ToString();
            RunTest(value1);

            // Test case 2b: HTTPS_PROXY > http_proxy
            envars.Variables.Clear();
            envars.Variables[Constants.EnvironmentVariables.CurlHttpsProxyUpper] = value1.ToString();
            envars.Variables[Constants.EnvironmentVariables.CurlHttpProxy] = value2.ToString();
            RunTest(value1);

            // Test case 3: all_proxy > ALL_PROXY
            envars.Variables.Clear();
            envars.Variables[Constants.EnvironmentVariables.CurlAllProxy] = value1.ToString();
            envars.Variables[Constants.EnvironmentVariables.CurlAllProxyUpper] = value2.ToString();
            RunTest(value1);
        }

        [Fact]
        public void Settings_ProviderOverride_Unset_ReturnsNull()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string value = settings.ProviderOverride;

            Assert.Null(value);
        }

        [Fact]
        public void Settings_ProviderOverride_EnvarSet_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmProvider] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string actualValue = settings.ProviderOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_ProviderOverride_ConfigSet_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Provider;
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {expectedValue};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string actualValue = settings.ProviderOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_ProviderOverride_EnvarAndConfigSet_ReturnsEnvarValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Provider;
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";
            const string otherValue = "provider2";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmProvider] = expectedValue}
            };
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {otherValue};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string actualValue = settings.ProviderOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_LegacyAuthorityOverride_Unset_ReturnsNull()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string value = settings.LegacyAuthorityOverride;

            Assert.Null(value);
        }

        [Fact]
        public void Settings_LegacyAuthorityOverride_EnvarSet_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAuthority] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string actualValue = settings.LegacyAuthorityOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_LegacyAuthorityOverride_ConfigSet_ReturnsTrueOutValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Authority;
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {expectedValue};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var actualValue = settings.LegacyAuthorityOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_LegacyAuthorityOverride_EnvarAndConfigSet_ReturnsEnvarValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Authority;
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";
            const string otherValue = "provider2";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAuthority] = expectedValue}
            };
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {otherValue};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var actualValue = settings.LegacyAuthorityOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_EnvarSet_ReturnsTrueOutValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_EnvarUnset_ReturnsFalse()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.False(result);
            Assert.Null(actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_GlobalConfig_ReturnsTrueAndValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Global[$"{section}.{property}"] = new[] {expectedValue};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting( envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_RepoConfig_ReturnsTrueAndValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Local[$"{section}.{property}"] = new[] {expectedValue};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_ScopedConfig()
        {
            const string remoteUrl = "http://example.com/foo/bar/bazz.git";
            const string scope1 = "example.com";
            const string scope2 = "example.com/foo/bar";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";
            const string otherValue = "Goodbye, World!";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.Configuration.Local[$"{section}.{scope1}.{property}"] = new []{otherValue};
            git.Configuration.Local[$"{section}.{scope2}.{property}"] = new []{expectedValue};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_EnvarAndConfig_EnvarTakesPrecedence()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";
            const string otherValue = "Goodbye, World!";

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = expectedValue}
            };
            var git = new TestGit();
            git.Configuration.Local[$"{section}.{property}"] = new[] {otherValue};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_GetSettingValues_EnvarAndMultipleConfig_ReturnsAllWithCorrectPrecedence()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string scope1 = "http://example.com";
            const string scope2 = "example.com";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string value1 = "First value";
            const string value2 = "Second value";
            const string value3 = "Third value";
            const string value4 = "Last value";

            string[] expectedValues = {value1, value2, value3, value4};

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = value1}
            };
            var git = new TestGit();
            git.Configuration.Local[$"{section}.{scope1}.{property}"] = new[]{value2};
            git.Configuration.Local[$"{section}.{scope2}.{property}"] = new[]{value3};
            git.Configuration.Local[$"{section}.{property}"]          = new[]{value4};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string[] actualValues = settings.GetSettingValues(envarName, section, property, false).ToArray();

            Assert.Equal(expectedValues, actualValues);
        }

        [Fact]
        public void Settings_GetSettingValues_ReturnsAllMatchingValues()
        {
            const string remoteUrl = "http://example.com/foo/bar/bazz.git";
            const string broadScope = "example.com";
            const string tightScope = "example.com/foo/bar";
            const string otherScope1 = "test.com";
            const string otherScope2 = "sub.test.com";
            const string envarName = "GCM_TESTVAR";
            const string envarValue = "envar-value";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string tightScopeValue = "value-scope1";
            const string broadScopeValue = "value-scope2";
            const string noScopeValue = "value-no-scope";
            const string otherValue1 = "other-scope1";
            const string otherValue2 = "other-scope2";

            string[] expectedValues = {envarValue, tightScopeValue, broadScopeValue, noScopeValue};

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = envarValue}
            };

            var git = new TestGit();
            git.Configuration.Local[$"{section}.{property}"] = new[] {noScopeValue};
            git.Configuration.Local[$"{section}.{broadScope}.{property}"] = new[] {broadScopeValue};
            git.Configuration.Local[$"{section}.{tightScope}.{property}"] = new[] {tightScopeValue};
            git.Configuration.Local[$"{section}.{otherScope1}.{property}"] = new[] {otherValue1};
            git.Configuration.Local[$"{section}.{otherScope2}.{property}"] = new[] {otherValue2};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            string[] actualValues = settings.GetSettingValues(envarName, section, property, false).ToArray();

            Assert.NotNull(actualValues);
            Assert.Equal(expectedValues, actualValues);
        }

        [Fact]
        public void Settings_GetSettingValues_IgnoresSectionAndPropertyCase_ScopeIsCaseSensitive()
        {
            const string remoteUrl = "http://example.com/foo/bar/bazz.git";
            const string scopeLo = "example.com";
            const string scopeHi = "EXAMPLE.COM";
            const string envarName = "GCM_TESTVAR";
            const string envarValue = "envar-value";
            const string sectionLo = "gcmtest";
            const string sectionHi = "GCMTEST";
            const string sectionMix = "GcMtEsT";
            const string propertyLo = "bar";
            const string propertyHi = "BAR";
            const string propertyMix = "bAr";
            var remoteUri = new Uri(remoteUrl);

            const string noScopeValue = "the-value";
            const string lowScopeValue = "value-scope-lo";
            const string highScopeValue = "value-scope-hi";

            string[] expectedValues = {envarValue, lowScopeValue, noScopeValue};

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = envarValue}
            };

            var git = new TestGit();
            git.Configuration.Local[$"{sectionLo}.{propertyHi}"] = new[] {noScopeValue};
            git.Configuration.Local[$"{sectionHi}.{scopeLo}.{propertyHi}"] = new[] {lowScopeValue};
            git.Configuration.Local[$"{sectionLo}.{scopeHi}.{propertyLo}"] = new[] {highScopeValue};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            string[] actualValues = settings.GetSettingValues(envarName, sectionMix, propertyMix, false).ToArray();

            Assert.NotNull(actualValues);
            Assert.Equal(expectedValues, actualValues);
        }

        [Theory]
        [InlineData(false, "~")]
        [InlineData(true, TestGitConfiguration.CanonicalPathPrefix)]
        public void Settings_GetSettingValues_IsPath_ReturnsAllParsedValues(bool isPath, string expectedPrefix)
        {
            const string remoteUrl = "http://example.com/foo/bar/bazz.git";
            const string broadScope = "example.com";
            const string tightScope = "example.com/foo/bar";
            const string envarName = "GCM_TESTVAR";
            const string envarValue = "envar-value";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string tightScopeValue = "path-tight";
            const string broadScopeValue = "path-broad";
            const string noScopeValue = "path-no-scope";

            string[] expectedValues = {
                envarValue,
                $"{expectedPrefix}/{tightScopeValue}",
                broadScopeValue,
                $"{expectedPrefix}/{noScopeValue}"
            };

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = envarValue}
            };

            var git = new TestGit();
            git.Configuration.Local[$"{section}.{property}"] = new[] {$"~/{noScopeValue}"};
            git.Configuration.Local[$"{section}.{broadScope}.{property}"] = new[] {broadScopeValue};
            git.Configuration.Local[$"{section}.{tightScope}.{property}"] = new[] {$"~/{tightScopeValue}"};

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            string[] actualValues = settings.GetSettingValues(envarName, section, property, isPath).ToArray();

            Assert.NotNull(actualValues);
            Assert.Equal(expectedValues, actualValues);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, "ca-config.crt", "ca-config.crt")]
        [InlineData("ca-envar.crt", "ca-config.crt", "ca-envar.crt")]
        public void Settings_CustomCertificateBundlePath_ReturnsExpectedValue(string sslCaInfoEnvar, string sslCaInfoConfig, string expectedValue)
        {
            const string envarName = Constants.EnvironmentVariables.GitSslCaInfo;
            const string section = Constants.GitConfiguration.Http.SectionName;
            const string sslCaInfo = Constants.GitConfiguration.Http.SslCaInfo;

            var envars = new TestEnvironment();
            if (sslCaInfoEnvar != null)
            {
                envars.Variables[envarName] = sslCaInfoEnvar;
            }

            var git = new TestGit();
            if (sslCaInfoConfig != null)
            {
                git.Configuration.Local[$"{section}.{sslCaInfo}"] = new[] {sslCaInfoConfig};
            }

            var settings = new Settings(envars, git);

            string actualValue = settings.CustomCertificateBundlePath;

            if (expectedValue is null)
            {
                Assert.Null(actualValue);
            }
            else
            {
                Assert.NotNull(actualValue);
                Assert.Equal(expectedValue, actualValue);
            }
        }

        [Theory]
        [InlineData(null, TlsBackend.OpenSsl)]
        [InlineData("schannel", TlsBackend.Schannel)]
        [InlineData("gnutls", TlsBackend.Other)]
        public void Settings_TlsBackend_ReturnsExpectedValue(string sslBackendConfig, TlsBackend expectedValue)
        {
            const string section = Constants.GitConfiguration.Http.SectionName;
            const string sslBackend = Constants.GitConfiguration.Http.SslBackend;

            var envars = new TestEnvironment();

            var git = new TestGit();
            if (sslBackendConfig != null)
            {
                git.Configuration.Local[$"{section}.{sslBackend}"] = new[] {sslBackendConfig};
            }

            var settings = new Settings(envars, git);

            TlsBackend actualValue = settings.TlsBackend;

            Assert.Equal(expectedValue, actualValue);
        }
    }
}
