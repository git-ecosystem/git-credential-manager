// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication;

namespace Microsoft.Git.CredentialManager
{
    public class GenericHostProvider : HostProvider
    {
        private readonly IBasicAuthentication _basicAuth;
        private readonly IWindowsIntegratedAuthentication _winAuth;

        public GenericHostProvider(ICommandContext context)
            : this(context, new BasicAuthentication(context), new WindowsIntegratedAuthentication(context)) { }

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

        public override string Id => "generic";

        public override string Name => "Generic";

        public override IEnumerable<string> SupportedAuthorityIds =>
            EnumerableExtensions.ConcatMany(
                BasicAuthentication.AuthorityIds,
                WindowsIntegratedAuthentication.AuthorityIds
            );

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
            ThrowIfDisposed();

            Uri uri = GetUriFromInput(input);

            // Determine the if the host supports Windows Integration Authentication (WIA)
            if (IsWindowsAuthAllowed)
            {
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
                        Context.Trace.WriteLine("Host supports WIA - generating empty credential...");

                        // WIA is signaled to Git using an empty username/password
                        return new GitCredential(string.Empty, string.Empty);
                    }
                }
                else
                {
                    string osType = PlatformUtils.GetPlatformInformation().OperatingSystemType;
                    Context.Trace.WriteLine($"Skipping check for Windows Integrated Authentication on {osType}.");
                }
            }
            else
            {
                Context.Trace.WriteLine("Windows Integrated Authentication detection has been disabled.");
            }

            Context.Trace.WriteLine("Prompting for basic credentials...");
            return _basicAuth.GetCredentials(uri.AbsoluteUri, uri.UserInfo);
        }

        /// <summary>
        /// Check if the user permits checking for Windows Integrated Authentication.
        /// </summary>
        /// <remarks>
        /// Checks the explicit 'GCM_ALLOW_WINDOWSAUTH' setting and also the legacy 'GCM_AUTHORITY' setting iif equal to "basic".
        /// </remarks>
        private bool IsWindowsAuthAllowed
        {
            get
            {
                if (Context.Settings.IsWindowsIntegratedAuthenticationEnabled)
                {
                    /* COMPAT: In the old GCM one workaround for common authentication problems was to specify "basic" as the authority
                     *         which prevents any smart detection of provider or NTLM etc, allowing the user a chance to manually enter
                     *         a username/password or PAT.
                     *
                     *         We take this old setting into account to ensure a good migration experience.
                     */
                    return !BasicAuthentication.AuthorityIds.Contains(Context.Settings.LegacyAuthorityOverride, StringComparer.OrdinalIgnoreCase);
                }

                return false;
            }
        }

        protected override void ReleaseManagedResources()
        {
            _winAuth.Dispose();
            base.ReleaseManagedResources();
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
