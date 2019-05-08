// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Text;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public interface IBasicAuthentication
    {
        GitCredential GetCredentials(string resource, string userName);
    }

    public static class BasicAuthenticationExtensions
    {
        public static GitCredential GetCredentials(this IBasicAuthentication basicAuth, string resource)
        {
            return basicAuth.GetCredentials(resource, null);
        }
    }

    public class TtyPromptBasicAuthentication : IBasicAuthentication
    {
        private readonly ICommandContext _context;

        public TtyPromptBasicAuthentication(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public GitCredential GetCredentials(string resource, string userName)
        {
            EnsureArgument.NotNullOrWhiteSpace(resource, nameof(resource));

            // Are terminal prompt disabled?
            if (_context.TryGetEnvironmentVariable(
                         Constants.EnvironmentVariables.GitTerminalPrompts, out string envarPrompts)
                     && envarPrompts == "0")
            {
                _context.Trace.WriteLine($"{Constants.EnvironmentVariables.GitTerminalPrompts} is 0; terminal prompts have been disabled.");

                throw new InvalidOperationException("Cannot show basic credential prompt because terminal prompts have been disabled.");
            }

            _context.Terminal.WriteLine("Enter credentials for '{0}':", resource);

            if (!string.IsNullOrWhiteSpace(userName))
            {
                // Don't need to prompt for the username if it has been specified already
                _context.Terminal.WriteLine("Username: {0}", userName);
            }
            else
            {
                // Prompt for username
                userName = _context.Terminal.Prompt("Username");
            }

            // Prompt for password
            string password = _context.Terminal.PromptSecret("Password");

            return new GitCredential(userName, password);
        }
    }
}
