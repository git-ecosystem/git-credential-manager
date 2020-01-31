// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

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

    public class BasicAuthentication : AuthenticationBase, IBasicAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "basic",
        };

        public BasicAuthentication(ICommandContext context)
            : base (context) { }

        public GitCredential GetCredentials(string resource, string userName)
        {
            EnsureArgument.NotNullOrWhiteSpace(resource, nameof(resource));

            ThrowIfUserInteractionDisabled();
            ThrowIfTerminalPromptsDisabled();

            Context.Terminal.WriteLine("Enter basic credentials for '{0}':", resource);

            if (!string.IsNullOrWhiteSpace(userName))
            {
                // Don't need to prompt for the username if it has been specified already
                Context.Terminal.WriteLine("Username: {0}", userName);
            }
            else
            {
                // Prompt for username
                userName = Context.Terminal.Prompt("Username");
            }

            // Prompt for password
            string password = Context.Terminal.PromptSecret("Password");

            return new GitCredential(userName, password);
        }
    }
}
