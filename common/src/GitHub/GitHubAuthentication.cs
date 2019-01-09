// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Git.CredentialManager;

namespace GitHub
{
    public interface IGitHubAuthentication
    {
        bool TryGetCredentials(Uri targetUri, out string userName, out string password);

        bool TryGetAuthenticationCode(Uri targetUri, bool isSms, out string authenticationCode);
    }

    public class TtyGitHubPromptAuthentication : IGitHubAuthentication
    {
        private readonly ICommandContext _context;

        public TtyGitHubPromptAuthentication(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public bool TryGetCredentials(Uri targetUri, out string userName, out string password)
        {
            EnsureTerminalPromptsEnabled();

            _context.StdError.WriteLine("Enter credentials for '{0}'...", targetUri);

            userName = _context.Prompt("Username");
            password = _context.PromptSecret("Password");

            return !string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password);
        }

        public bool TryGetAuthenticationCode(Uri targetUri, bool isSms, out string authenticationCode)
        {
            EnsureTerminalPromptsEnabled();

            _context.StdError.WriteLine("Two-factor authentication is enabled and an authentication code is required.");

            if (isSms)
            {
                _context.StdError.WriteLine("An SMS containing the authentication code has been sent to your registered device.");
            }
            else
            {
                _context.StdError.WriteLine("Use your registered authentication app to generate an authentication code.");
            }

            authenticationCode = _context.Prompt("Authentication code");
            return !string.IsNullOrWhiteSpace(authenticationCode);
        }

        private void EnsureTerminalPromptsEnabled()
        {
            if (_context.TryGetEnvironmentVariable(
                    Constants.EnvironmentVariables.GitTerminalPrompts, out string envarPrompts)
                && envarPrompts == "0")
            {
                _context.Trace.WriteLine($"{Constants.EnvironmentVariables.GitTerminalPrompts} is 0; terminal prompts have been disabled.");

                throw new InvalidOperationException("Cannot show GitHub credential prompt because terminal prompts have been disabled.");
            }
        }
    }
}
