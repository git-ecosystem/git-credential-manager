// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public interface INtlmAuthentication
    {
        Task<bool> IsNtlmSupportedAsync(Uri uri);
    }

    public class NtlmAuthentication : INtlmAuthentication
    {
        private readonly ICommandContext _context;
        private readonly IHttpClientFactory _httpFactory;

        public NtlmAuthentication(ICommandContext context)
            : this(context, new HttpClientFactory()) { }

        public NtlmAuthentication(ICommandContext context, IHttpClientFactory httpFactory)
        {
            _context = context;
            _httpFactory = httpFactory;
        }

        public async Task<bool> IsNtlmSupportedAsync(Uri uri)
        {
            EnsureArgument.AbsoluteUri(uri, nameof(uri));

            if (!PlatformUtils.IsWindows())
            {
                _context.Trace.WriteLine("NTLM is only supported on Windows");
                return false;
            }

            _context.Trace.WriteLine($"HTTP HEAD {uri}");
            using (HttpClient client = _httpFactory.GetClient())
            using (HttpResponseMessage response = await client.HeadAsync(uri))
            {
                _context.Trace.WriteLine("HTTP Response - Ignoring response code");

                _context.Trace.WriteLine("Inspecting WWW-Authenticate headers...");
                foreach (AuthenticationHeaderValue wwwHeader in response.Headers.WwwAuthenticate)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(wwwHeader.Scheme, Constants.WwwAuthenticateNtlmScheme))
                    {
                        _context.Trace.WriteLine("Found WWW-Authenticate header for NTLM");
                        return true;
                    }
                }
            }

            _context.Trace.WriteLine("No WWW-Authenticate header for NTLM was found");
            return false;
        }
    }
}
