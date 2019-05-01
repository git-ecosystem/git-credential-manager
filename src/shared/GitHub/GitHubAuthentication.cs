// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;

namespace GitHub
{
    public interface IGitHubAuthentication
    {
        Task<ICredential> GetCredentialsAsync(Uri targetUri);

        Task<string> GetAuthenticationCodeAsync(Uri targetUri, bool isSms);
    }

    public class TtyGitHubPromptAuthentication : IGitHubAuthentication
    {
        private readonly ICommandContext _context;

        public TtyGitHubPromptAuthentication(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public Task<ICredential> GetCredentialsAsync(Uri targetUri)
        {
            EnsureTerminalPromptsEnabled();

            _context.Terminal.WriteLine("Enter credentials for '{0}'...", targetUri);

            string userName = _context.Terminal.Prompt("Username");
            string password = _context.Terminal.PromptSecret("Password");

            return Task.FromResult<ICredential>(new GitCredential(userName, password));
        }

        public Task<string> GetAuthenticationCodeAsync(Uri targetUri, bool isSms)
        {
            EnsureTerminalPromptsEnabled();

            _context.Terminal.WriteLine("Two-factor authentication is enabled and an authentication code is required.");

            if (isSms)
            {
                _context.Terminal.WriteLine("An SMS containing the authentication code has been sent to your registered device.");
            }
            else
            {
                _context.Terminal.WriteLine("Use your registered authentication app to generate an authentication code.");
            }

            string authCode = _context.Terminal.Prompt("Authentication code");

            return Task.FromResult(authCode);
        }

        private void EnsureTerminalPromptsEnabled()
        {
            if (_context.TryGetEnvironmentVariable(Constants.EnvironmentVariables.GitTerminalPrompts, out string envarPrompts)
                && envarPrompts == "0")
            {
                _context.Trace.WriteLine($"{Constants.EnvironmentVariables.GitTerminalPrompts} is 0; terminal prompts have been disabled.");

                throw new InvalidOperationException("Cannot show GitHub credential prompt because terminal prompts have been disabled.");
            }
        }
    }
}
