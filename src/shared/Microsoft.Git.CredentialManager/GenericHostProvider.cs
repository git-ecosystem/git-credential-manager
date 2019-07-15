// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication;

namespace Microsoft.Git.CredentialManager
{
    public class GenericHostProvider : HostProvider
    {
        private readonly IBasicAuthentication _basicAuth;
        private readonly IWindowsIntegratedAuthentication _winAuth;

        public GenericHostProvider(ICommandContext context)
            : this(context, new TtyPromptBasicAuthentication(context), new WindowsIntegratedAuthentication(context)) { }

        public GenericHostProvider(ICommandContext context,
                                   IBasicAuthentication basicAuth,
                                   IWindowsIntegratedAuthentication winAuth)
            : base(context)
        {
            EnsureArgument.NotNull(basicAuth, nameof(basicAuth));
            EnsureArgument.NotNull(winAuth, nameof(winAuth));

            _basicAuth = basicAuth;
            _winAuth = winAuth;
        }

        #region HostProvider

        public override string Name => "Generic";

        public override bool IsSupported(InputArguments input)
        {
            return input != null && (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                                     StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https"));
        }

        public override string GetCredentialKey(InputArguments input)
        {
            return $"git:{GetUriFromInput(input).AbsoluteUri}";
        }

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            Uri uri = GetUriFromInput(input);

            // Determine the if the host supports Windows Integration Authentication (WIA)
            if (PlatformUtils.IsWindows())
            {
                Context.Trace.WriteLine($"Checking host '{uri.AbsoluteUri}' for Windows Integrated Authentication...");
                bool isWiaSupported = await _winAuth.GetIsSupportedAsync(uri);

                if (!isWiaSupported)
                {
                    Context.Trace.WriteLine("Host does not support WIA.");
                }
                else
                {
                    Context.Trace.WriteLine($"Host supports WIA - generating empty credential...");

                    // WIA is signaled to Git using an empty username/password
                    return new GitCredential(string.Empty, string.Empty);
                }
            }
            else
            {
                string osType = PlatformUtils.GetPlatformInformation().OperatingSystemType;
                Context.Trace.WriteLine($"Skipping check for Windows Integrated Authentication on {osType}.");
            }

            Context.Trace.WriteLine("Prompting for basic credentials...");
            return _basicAuth.GetCredentials(uri.AbsoluteUri, uri.UserInfo);
        }

        protected override void Dispose(bool disposing)
        {
            _winAuth.Dispose();
            base.Dispose(disposing);
        }

        #endregion

        #region Helpers

        private static Uri GetUriFromInput(InputArguments input)
        {
            return new UriBuilder
            {
                Scheme   = input.Protocol,
                UserName = input.UserName,
                Host     = input.Host,
                Path     = input.Path
            }.Uri;
        }

        #endregion
    }
}
