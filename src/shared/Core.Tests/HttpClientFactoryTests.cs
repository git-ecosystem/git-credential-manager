using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Moq;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class HttpClientFactoryTests
    {
        [Fact]
        public void HttpClientFactory_GetClient_SetsDefaultHeaders()
        {
            var factory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), Mock.Of<ISettings>(), new TestStandardStreams());

            HttpClient client = factory.CreateClient();

            Assert.NotNull(client);
            Assert.Equal(Constants.GetHttpUserAgent(Mock.Of<ITrace2>()), client.DefaultRequestHeaders.UserAgent.ToString());
            Assert.True(client.DefaultRequestHeaders.CacheControl.NoCache);
        }

        [Fact]
        public void HttpClientFactory_GetClient_MultipleCalls_ReturnsNewInstance()
        {
            var factory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), Mock.Of<ISettings>(), new TestStandardStreams());

            HttpClient client1 = factory.CreateClient();
            HttpClient client2 = factory.CreateClient();

            Assert.NotSame(client1, client2);
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_NoProxy_ReturnsFalseOutNull()
        {
            const string repoPath = "/tmp/repos/foo";
            const string repoRemote  = "https://remote.example.com/foo.git";

            var repoRemoteUri = new Uri(repoRemote);
            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath
            };
            var httpFactory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, Mock.Of<IStandardStreams>());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.False(result);
            Assert.Null(proxy);
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_ProxyNoCredentials_ReturnsTrueOutProxyWithUrlDefaultCredentials()
        {
            const string proxyUrl = "https://proxy.example.com/git";
            const string repoPath = "/tmp/repos/foo";
            const string repoRemote = "https://remote.example.com/foo.git";

            var repoRemoteUri = new Uri(repoRemote);
            var proxyConfig = new ProxyConfiguration(new Uri(proxyUrl));

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath,
                ProxyConfiguration = proxyConfig
            };
            var httpFactory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, Mock.Of<IStandardStreams>());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.True(result);
            Assert.NotNull(proxy);
            Uri configuredProxyUrl = proxy.GetProxy(repoRemoteUri);
            Assert.Equal(proxyUrl, configuredProxyUrl?.ToString());

            AssertDefaultCredentials(proxy.Credentials);
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_ProxyWithBypass_ReturnsTrueOutProxyWithBypassedHosts()
        {
            const string proxyUrl = "https://proxy.example.com/git";
            const string repoPath = "/tmp/repos/foo";
            const string repoRemote = "https://remote.example.com/foo.git";

            var noProxyRaw = "contoso.com,fabrikam.com";
            var repoRemoteUri = new Uri(repoRemote);
            var proxyConfig = new ProxyConfiguration(
                new Uri(proxyUrl),
                userName: null,
                password: null,
                noProxyRaw: noProxyRaw);

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath,
                ProxyConfiguration = proxyConfig
            };
            var httpFactory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, Mock.Of<IStandardStreams>());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.True(result);
            Assert.NotNull(proxy);
            Uri configuredProxyUrl = proxy.GetProxy(repoRemoteUri);
            Assert.Equal(proxyUrl, configuredProxyUrl?.ToString());

            Assert.True(proxy.IsBypassed(new Uri("https://contoso.com")));
            Assert.True(proxy.IsBypassed(new Uri("http://fabrikam.com")));
            Assert.True(proxy.IsBypassed(new Uri("https://subdomain.fabrikam.com")));
            Assert.False(proxy.IsBypassed(repoRemoteUri));
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_ProxyWithWildcardBypass_ReturnsFalse()
        {
            const string proxyUrl = "https://proxy.example.com/git";
            const string repoPath = "/tmp/repos/foo";
            const string repoRemote = "https://remote.example.com/foo.git";

            var noProxyRaw = "*";
            var repoRemoteUri = new Uri(repoRemote);
            var proxyConfig = new ProxyConfiguration(
                new Uri(proxyUrl),
                userName: null,
                password: null,
                noProxyRaw: noProxyRaw);

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath,
                ProxyConfiguration = proxyConfig
            };
            var httpFactory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, Mock.Of<IStandardStreams>());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.False(result);
            Assert.Null(proxy);
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_ProxyWithCredentials_ReturnsTrueOutProxyWithUrlConfiguredCredentials()
        {
            const string proxyUser   = "john.doe";
            const string proxyPass   = "letmein";
            const string proxyUrl    = "https://proxy.example.com/git";
            const string repoPath    = "/tmp/repos/foo";
            const string repoRemote  = "https://remote.example.com/foo.git";

            var repoRemoteUri = new Uri(repoRemote);
            var proxyConfig = new ProxyConfiguration(
                new Uri(proxyUrl),
                proxyUser,
                proxyPass);

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath,
                ProxyConfiguration = proxyConfig
            };
            var httpFactory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, Mock.Of<IStandardStreams>());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.True(result);
            Assert.NotNull(proxy);
            Uri configuredProxyUrl = proxy.GetProxy(repoRemoteUri);
            Assert.Equal(proxyUrl, configuredProxyUrl?.ToString());

            Assert.NotNull(proxy.Credentials);
            Assert.IsType<NetworkCredential>(proxy.Credentials);
            var configuredCredentials = (NetworkCredential) proxy.Credentials;
            Assert.Equal(proxyUser, configuredCredentials.UserName);
            Assert.Equal(proxyPass, configuredCredentials.Password);
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_ProxyWithNonEmptyUserAndEmptyPass_ReturnsTrueOutProxyWithUrlConfiguredCredentials()
        {
            const string proxyUrl    = "https://proxy.example.com/git";
            const string proxyUser   = "john.doe";
            const string repoPath    = "/tmp/repos/foo";
            const string repoRemote  = "https://remote.example.com/foo.git";

            var repoRemoteUri = new Uri(repoRemote);
            var proxyConfig = new ProxyConfiguration(
                new Uri(proxyUrl),
                proxyUser,
                password: null);

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath,
                ProxyConfiguration = proxyConfig
            };
            var httpFactory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, Mock.Of<IStandardStreams>());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.True(result);
            Assert.NotNull(proxy);
            Uri configuredProxyUrl = proxy.GetProxy(repoRemoteUri);
            Assert.Equal(proxyUrl, configuredProxyUrl?.ToString());

            Assert.NotNull(proxy.Credentials);
            Assert.IsType<NetworkCredential>(proxy.Credentials);
            var configuredCredentials = (NetworkCredential) proxy.Credentials;
            Assert.Equal(proxyUser, configuredCredentials.UserName);
            Assert.True(string.IsNullOrWhiteSpace(configuredCredentials.Password));
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_ProxyWithEmptyUserAndNonEmptyPass_ReturnsTrueOutProxyWithUrlConfiguredCredentials()
        {
            const string proxyUrl    = "https://proxy.example.com/git";
            const string proxyPass   = "letmein";
            const string repoPath    = "/tmp/repos/foo";
            const string repoRemote  = "https://remote.example.com/foo.git";

            var repoRemoteUri = new Uri(repoRemote);
            var proxyConfig = new ProxyConfiguration(
                new Uri(proxyUrl),
                userName: null,
                password: proxyPass);

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath,
                ProxyConfiguration = proxyConfig
            };
            var httpFactory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, Mock.Of<IStandardStreams>());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.True(result);
            Assert.NotNull(proxy);
            Uri configuredProxyUrl = proxy.GetProxy(repoRemoteUri);
            Assert.Equal(proxyUrl, configuredProxyUrl?.ToString());

            Assert.NotNull(proxy.Credentials);
            Assert.IsType<NetworkCredential>(proxy.Credentials);
            var configuredCredentials = (NetworkCredential) proxy.Credentials;
            Assert.True(string.IsNullOrWhiteSpace(configuredCredentials.UserName));
            Assert.Equal(proxyPass, configuredCredentials.Password);
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_ProxyEmptyUserAndEmptyPass_ReturnsTrueOutProxyWithUrlDefaultCredentials()
        {
            const string proxyUrl    = "https://proxy.example.com/git";
            const string repoPath = "/tmp/repos/foo";
            const string repoRemote = "https://remote.example.com/foo.git";
            var repoRemoteUri = new Uri(repoRemote);

            var proxyConfig = new ProxyConfiguration(
                new Uri(proxyUrl),
                userName: string.Empty,
                password: string.Empty);

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath,
                ProxyConfiguration = proxyConfig
            };
            var httpFactory = new HttpClientFactory(Mock.Of<IFileSystem>(), Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, Mock.Of<IStandardStreams>());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.True(result);
            Assert.NotNull(proxy);
            Uri configuredProxyUrl = proxy.GetProxy(repoRemoteUri);
            Assert.Equal(proxyUrl, configuredProxyUrl?.ToString());

            AssertDefaultCredentials(proxy.Credentials);
        }

        [Theory]
        [InlineData(null, TlsBackend.OpenSsl, false, false)]
        [InlineData("ca-bundle.crt", TlsBackend.Other, false, true)]
        [InlineData("ca-bundle.crt", TlsBackend.Schannel, false, false)]
        [InlineData("ca-bundle.crt", TlsBackend.Schannel, true, true)]
        public void HttpClientFactory_GetClient_ChecksCertBundleOnlyIfEnabled(string customCertBundle,
            TlsBackend tlsBackend, bool useCustomCertBundleWithSchannel, bool expectBundleChecked)
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);

            var settings = new TestSettings()
            {
                CustomCertificateBundlePath = customCertBundle,
                TlsBackend = tlsBackend,
                UseCustomCertificateBundleWithSchannel = useCustomCertBundleWithSchannel
            };

            var factory = new HttpClientFactory(fileSystemMock.Object, Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, new TestStandardStreams());

            HttpClient client = factory.CreateClient();

            fileSystemMock.Verify(fs => fs.FileExists(It.IsAny<string>()), expectBundleChecked ? Times.Once : Times.Never);
        }

        [Theory]
        [InlineData(null, false, null)]
        [InlineData("~/.git-cookie", true, "# Netscape HTTP Cookie File\n" +
                          "# https://curl.haxx.se/rfc/cookie_spec.html\n" +
                          "# This is a generated file! Do not edit.\n" +
                          "\n" +
                          ".example.com\tTRUE\t/\tTRUE\t0\tcookie1\tvalue1\n" +
                          ".example.com\tTRUE\t/\tTRUE\t0\tcookie2\tvalue2\n" +
                          "#HttpOnly_.example.com\tTRUE\t/\tTRUE\t0\tcookie3\tvalue3\n")]
        public void HttpClientFactory_GetClient_SetCookieOnlyIfEnabled(string cookieFilePath, bool expectCookieChecked, string cookieFileContent)
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            if (!string.IsNullOrWhiteSpace(cookieFileContent))
            {
                fileSystemMock.Setup(fs => fs.ReadAllText(cookieFilePath)).Returns(cookieFileContent);
            }

            var settings = new TestSettings()
            {
                CustomCookieFilePath = cookieFilePath
            };

            var factory = new HttpClientFactory(fileSystemMock.Object, Mock.Of<ITrace>(), Mock.Of<ITrace2>(), settings, new TestStandardStreams());

            HttpClient client = factory.CreateClient();

            fileSystemMock.Verify(fs => fs.FileExists(It.IsAny<string>()), expectCookieChecked ? Times.AtLeastOnce : Times.Never);
        }

        private static void AssertDefaultCredentials(ICredentials credentials)
        {
            var netCred = (NetworkCredential) credentials;

            Assert.Equal(string.Empty, netCred.Domain);
            Assert.Equal(string.Empty, netCred.UserName);
            Assert.Equal(string.Empty, netCred.Password);
        }
    }
}
