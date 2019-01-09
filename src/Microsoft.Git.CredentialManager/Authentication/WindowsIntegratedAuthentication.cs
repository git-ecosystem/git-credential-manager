// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public interface IWindowsIntegratedAuthentication
    {
        Task<bool> GetIsSupportedAsync(Uri uri);
    }

    public class WindowsIntegratedAuthentication : IWindowsIntegratedAuthentication
    {
        private readonly ICommandContext _context;
        private readonly IHttpClientFactory _httpFactory;

        public WindowsIntegratedAuthentication(ICommandContext context)
            : this(context, new HttpClientFactory()) { }

        public WindowsIntegratedAuthentication(ICommandContext context, IHttpClientFactory httpFactory)
        {
            _context = context;
            _httpFactory = httpFactory;
        }

        public async Task<bool> GetIsSupportedAsync(Uri uri)
        {
            EnsureArgument.AbsoluteUri(uri, nameof(uri));

            bool supported = false;

            _context.Trace.WriteLine($"HTTP: HEAD {uri}");
            using (HttpClient client = _httpFactory.GetClient())
            using (HttpResponseMessage response = await client.SendAsync(HttpMethod.Head, uri))
            {
                _context.Trace.WriteLine("HTTP: Response code ignored.");

                _context.Trace.WriteLine("Inspecting WWW-Authenticate headers...");
                foreach (AuthenticationHeaderValue wwwHeader in response.Headers.WwwAuthenticate)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(wwwHeader.Scheme, Constants.Http.WwwAuthenticateNegotiateScheme))
                    {
                        _context.Trace.WriteLine("Found WWW-Authenticate header for Negotiate");
                        supported = true;
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(wwwHeader.Scheme, Constants.Http.WwwAuthenticateNtlmScheme))
                    {
                        _context.Trace.WriteLine("Found WWW-Authenticate header for NTLM");
                        supported = true;
                    }
                }
            }

            return supported;
        }
    }
}
