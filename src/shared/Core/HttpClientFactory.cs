using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace GitCredentialManager
{
    /// <summary>
    /// Constructs <see cref="HttpClient"/>s that have been configured for use in Git Credential Manager.
    /// </summary>
    public interface IHttpClientFactory
    {
        /// <summary>
        /// Get a new instance of <see cref="HttpClient"/> with default request headers set.
        /// </summary>
        /// <remarks>
        /// Callers should reuse instances of <see cref="HttpClient"/> returned from this method as long
        /// as they are needed, rather than repeatably call this method to create new ones.
        /// <para/>
        /// Creating a new <see cref="HttpClient"/> consumes one free socket which may not be released
        /// by the Operating System until sometime after the client is disposed, leading to possible free
        /// socket exhaustion.
        /// <para/>
        /// The client instance is configured for use behind a proxy using Git's http.proxy configuration
        /// for the local repository (the current working directory), if found.
        /// </remarks>
        /// <returns>New client instance with default headers.</returns>
        HttpClient CreateClient();
    }

    public class HttpClientFactory : IHttpClientFactory
    {
        private readonly IFileSystem _fileSystem;
        private readonly ITrace _trace;
        private readonly ISettings _settings;
        private readonly IStandardStreams _streams;

        public HttpClientFactory(IFileSystem fileSystem, ITrace trace, ISettings settings, IStandardStreams streams)
        {
            EnsureArgument.NotNull(fileSystem, nameof(fileSystem));
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(settings, nameof(settings));
            EnsureArgument.NotNull(streams, nameof(streams));

            _fileSystem = fileSystem;
            _trace = trace;
            _settings = settings;
            _streams = streams;
        }

        public HttpClient CreateClient()
        {
            _trace.WriteLine("Creating new HTTP client instance...");

            HttpClientHandler handler;

            if (TryCreateProxy(out IWebProxy proxy))
            {
                _trace.WriteLine("HTTP client is using the configured proxy.");

                handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true
                };
            }
            else
            {
                handler = new HttpClientHandler();
            }

            // IsCertificateVerificationEnabled takes precedence over custom TLS cert verification
            if (!_settings.IsCertificateVerificationEnabled)
            {
                _trace.WriteLine("TLS certificate verification has been disabled.");
                _streams.Error.WriteLine("warning: ----------------- SECURITY WARNING ----------------");
                _streams.Error.WriteLine("warning: | TLS certificate verification has been disabled! |");
                _streams.Error.WriteLine("warning: ---------------------------------------------------");
                _streams.Error.WriteLine($"warning: HTTPS connections may not be secure. See {Constants.HelpUrls.GcmTlsVerification} for more information.");

#if NETFRAMEWORK
                ServicePointManager.ServerCertificateValidationCallback = (req, cert, chain, errors) => true;
#elif NETSTANDARD
                handler.ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true;
#endif
            }
            // If schannel is the TLS backend, custom certificate usage must be explicitly enabled
            else if (!string.IsNullOrWhiteSpace(_settings.CustomCertificateBundlePath) &&
                ((_settings.TlsBackend != TlsBackend.Schannel) || _settings.UseCustomCertificateBundleWithSchannel))
            {
                string certBundlePath = _settings.CustomCertificateBundlePath;
                _trace.WriteLine($"Custom certificate verification has been enabled with certificate bundle at {certBundlePath}");

                // Throw exception if cert bundle file not found
                if (!_fileSystem.FileExists(certBundlePath))
                {
                    throw new FileNotFoundException($"Custom certificate bundle not found at path: {certBundlePath}", certBundlePath);
                }

                Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> validationCallback = (cert, chain, errors) =>
                {
                    // Fail immediately if there are non-chain issues with the remote cert
                    if ((errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                    {
                        return false;
                    }

                    // Import the custom certs
                    X509Certificate2Collection certBundle = new X509Certificate2Collection();
                    certBundle.Import(certBundlePath);

                    try
                    {
                        // Add the certs to the chain
                        chain.ChainPolicy.ExtraStore.AddRange(certBundle);

                        // Rebuild the chain
                        if (chain.Build(cert))
                        {
                            return true;
                        }

                        // Manually handle case where only error is UntrustedRoot
                        if (chain.ChainStatus.All(status => status.Status == X509ChainStatusFlags.UntrustedRoot))
                        {
                            // Verify root is contained within the certBundle
                            X509Certificate2 rootCert = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                            var matchingCerts = certBundle.Find(X509FindType.FindByThumbprint, rootCert.Thumbprint, false);
                            if (matchingCerts.Count > 0)
                            {
                                // Check the content of the first matching cert found (do
                                // not try others if mismatched - mirrors OpenSSL:
                                // https://www.openssl.org/docs/man1.1.1/man3/SSL_CTX_set_default_verify_paths.html#WARNINGS)
                                return rootCert.RawData.SequenceEqual(matchingCerts[0].RawData);
                            }
                            else
                            {
                                // Untrusted root not found in custom cert bundle
                                return false;
                            }
                        }

                        // Fail for errors other than UntrustedRoot
                        return false;
                    }
                    finally
                    {
                        // Dispose imported cert bundle
                        for (int i = 0; i < certBundle.Count; i++)
                        {
                            certBundle[i].Dispose();
                        }
                    }
                };

                // Set the custom server certificate validation callback.
                // NOTE: this is executed after the default platform server certificate validation is performed
#if NETFRAMEWORK
                ServicePointManager.ServerCertificateValidationCallback = (_, cert, chain, errors) =>
                {
                    // Fail immediately if the cert or chain isn't present
                    if (cert is null || chain is null)
                    {
                        return false;
                    }

                    using (X509Certificate2 cert2 = new X509Certificate2(cert))
                    {
                        return validationCallback(cert2, chain, errors);
                    }
                };
#elif NETSTANDARD
                handler.ServerCertificateCustomValidationCallback = (_, cert, chain, errors) => validationCallback(cert, chain, errors);
#endif
            }

            var client = new HttpClient(handler);

            // Add default headers
            client.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.GetHttpUserAgent());
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };

            return client;
        }

        /// <summary>
        /// Try to create an <see cref="IWebProxy"/> object from the configuration specified by environment variables,
        /// or Git configuration for the specified repository, current user, and system.
        /// </summary>
        /// <param name="proxy">Configured <see cref="IWebProxy"/>, or null if no proxy configuration was found.</param>
        /// <returns>True if proxy configuration was found, false otherwise.</returns>
        public bool TryCreateProxy(out IWebProxy proxy)
        {
            // Try to extract the proxy URI from the environment or Git config
            ProxyConfiguration proxyConfig = _settings.GetProxyConfiguration();
            if (proxyConfig != null)
            {
                // Inform the user if they are using a deprecated proxy configuration
                if (proxyConfig.IsDeprecatedSource)
                {
                    _trace.WriteLine("Using a deprecated proxy configuration.");
                    _streams.Error.WriteLine($"warning: Using a deprecated proxy configuration. See {Constants.HelpUrls.GcmHttpProxyGuide} for more information.");
                }

                // If NO_PROXY is set to the value "*" then libcurl disables proxying. We should mirror this.
                if (StringComparer.OrdinalIgnoreCase.Equals("*", proxyConfig.NoProxyRaw))
                {
                    _trace.WriteLine("NO_PROXY value set to \"*\"; disabling proxy settings");
                    proxy = null;
                    return false;
                }

                // Dictionary of proxy info for tracing
                var dict = new Dictionary<string, string> {["address"] = proxyConfig.Address.ToString()};

                // Try to configure proxy credentials.
                // For an empty username AND password we should use the system default credentials
                // (for example for Windows Integrated Authentication-based proxies).
                if (!(string.IsNullOrEmpty(proxyConfig.UserName) && string.IsNullOrEmpty(proxyConfig.Password)))
                {
                    proxy = new WebProxy(proxyConfig.Address)
                    {
                        Credentials = new NetworkCredential(proxyConfig.UserName, proxyConfig.Password),
                    };

                    // Add user/pass info to the trace dictionary
                    dict["username"] = proxyConfig.UserName;
                    dict["password"] = proxyConfig.Password;
                }
                else
                {
                    proxy = new WebProxy(proxyConfig.Address)
                    {
                        UseDefaultCredentials = true,
                    };

                    // Trace the use of system default credentials
                    dict["useDefaultCredentials"] = "true";
                }

                // Set bypass address list.
                // The .NET WebProxy class requires that each host entry in the bypass list be a regular expression.
                // However libcurl (that we are aiming to be compatible/co-operative with) doesn't support regexs so
                // we must convert the libcurl-esc entries in to .NET-compatible regular expressions.
                // If we fail at any point we shouldn't crash but write a warning to trace output.
                if (!string.IsNullOrWhiteSpace(proxyConfig.NoProxyRaw))
                {
                    dict["noProxy"] = proxyConfig.NoProxyRaw;

                    try
                    {
                        string[] bypassRegexs = ProxyConfiguration.ConvertToBypassRegexArray(proxyConfig.NoProxyRaw).ToArray();
                        dict["bypass"] = string.Join(",", bypassRegexs);

                        ((WebProxy)proxy).BypassList = bypassRegexs;
                    }
                    catch (Exception ex)
                    {
                        _trace.WriteLine("Failed to convert proxy bypass hosts to regular expressions; ignoring bypass list");
                        _trace.WriteException(ex);
                        dict["bypass"] = "<< failed to convert >>";
                    }
                }

                // Tracer out proxy info dictionary
                _trace.WriteLine("Created a WebProxy instance:");
                _trace.WriteDictionarySecrets(dict, new[] {"password"});

                return true;
            }

            proxy = null;
            return false;
        }
    }
}
