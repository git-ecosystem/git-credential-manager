using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication;

namespace Microsoft.Git.CredentialManager
{
    public class GenericHostProvider : HostProvider
    {
        private readonly IBasicAuthentication _basicAuth;
        private readonly INtlmAuthentication _ntlmAuth;

        public GenericHostProvider(ICommandContext context)
            : this(context, new TtyPromptBasicAuthentication(context), new NtlmAuthentication(context)) { }

        public GenericHostProvider(ICommandContext context,
                                   IBasicAuthentication basicAuth,
                                   INtlmAuthentication ntlmAuth)
            : base(context)
        {
            EnsureArgument.NotNull(basicAuth, nameof(basicAuth));
            EnsureArgument.NotNull(ntlmAuth, nameof(ntlmAuth));

            _basicAuth = basicAuth;
            _ntlmAuth = ntlmAuth;
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
            return GetUriFromInput(input).AbsoluteUri;
        }

        public override async Task<GitCredential> CreateCredentialAsync(InputArguments input)
        {
            Uri uri = GetUriFromInput(input);

            // Determine the if the host supports NTLM
            Context.Trace.WriteLine($"Checking host '{uri.AbsoluteUri}' for NTLM support...");
            bool supportsNtlm = await _ntlmAuth.IsNtlmSupportedAsync(uri);

            GitCredential credential;
            if (supportsNtlm)
            {
                Context.Trace.WriteLine("Host supports NTLM - generating empty credential...");

                // NTLM is signaled to Git using an empty username/password
                credential = new GitCredential(string.Empty, string.Empty);
            }
            else
            {
                Context.Trace.WriteLine("Prompting for basic credentials...");

                credential = _basicAuth.GetCredentials(uri.AbsoluteUri, uri.UserInfo);
            }

            Context.Trace.WriteLine("Credentials created.");
            return credential;
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
