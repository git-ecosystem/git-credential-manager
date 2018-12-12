// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Text;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public interface IBasicAuthentication
    {
        GitCredential GetCredentials(Uri uri);
    }

    public class TtyPromptBasicAuthentication : IBasicAuthentication
    {
        private readonly ICommandContext _context;

        public TtyPromptBasicAuthentication(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public GitCredential GetCredentials(Uri uri)
        {
            EnsureArgument.AbsoluteUri(uri, nameof(uri));

            // Are terminal prompt disabled?
            if (_context.TryGetEnvironmentVariable(
                         Constants.EnvironmentVariables.GitTerminalPrompts, out string envarPrompts)
                     && envarPrompts == "0")
            {
                _context.Trace.WriteLine($"{Constants.EnvironmentVariables.GitTerminalPrompts} is 0; terminal prompts have been disabled.");

                throw new InvalidOperationException("Cannot show basic credential prompt because terminal prompts have been disabled.");
            }

            _context.StdError.WriteLine("Enter credentials for '{0}':", uri.AbsoluteUri);

            string userName;
            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                // Don't need to prompt for the username if it has been specified in the URL
                userName = uri.UserInfo;
                _context.StdError.WriteLine("Username: {0}", userName);
            }
            else
            {
                // Prompt for username
                userName = _context.Prompt("Username", outStream: _context.StdError);
            }

            // Prompt for password
            string password = _context.Prompt("Password", echo: false, outStream: _context.StdError);

            return new GitCredential(userName, password);
        }
    }
}
