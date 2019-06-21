// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;

namespace GitHub
{
    public interface IGitHubAuthentication
    {
        Task<ICredential> GetCredentialsAsync(Uri targetUri);

        Task<string> GetAuthenticationCodeAsync(Uri targetUri, bool isSms);
    }

    public class GitHubAuthentication : AuthenticationBase, IGitHubAuthentication
    {
        public GitHubAuthentication(ICommandContext context)
            : base(context) {}

        public async Task<ICredential> GetCredentialsAsync(Uri targetUri)
        {
            string userName, password;

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, "--prompt userpass", null);

                if (!resultDict.TryGetValue("username", out userName))
                {
                    throw new Exception("Missing username in response");
                }

                if (!resultDict.TryGetValue("password", out password))
                {
                    throw new Exception("Missing password in response");
                }
            }
            else
            {
                EnsureTerminalPromptsEnabled();

                Context.Terminal.WriteLine("Enter credentials for '{0}'...", targetUri);

                userName = Context.Terminal.Prompt("Username");
                password = Context.Terminal.PromptSecret("Password");
            }

            return new GitCredential(userName, password);
        }
        public async Task<string> GetAuthenticationCodeAsync(Uri targetUri, bool isSms)
        {
            if (TryFindHelperExecutablePath(out string helperPath))
            {
                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, "--prompt authcode", null);

                if (!resultDict.TryGetValue("authcode", out string authCode))
                {
                    throw new Exception("Missing authentication code in response");
                }

                return authCode;
            }
            else
            {
                EnsureTerminalPromptsEnabled();

                Context.Terminal.WriteLine("Two-factor authentication is enabled and an authentication code is required.");

                if (isSms)
                {
                    Context.Terminal.WriteLine("An SMS containing the authentication code has been sent to your registered device.");
                }
                else
                {
                    Context.Terminal.WriteLine("Use your registered authentication app to generate an authentication code.");
                }

                return Context.Terminal.Prompt("Authentication code");
            }
        }

        private bool TryFindHelperExecutablePath(out string path)
        {
            string helperName = GitHubConstants.AuthHelperName;

            if (PlatformUtils.IsWindows())
            {
                helperName += ".exe";
            }

            string executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(executableDirectory, helperName);
            if (!Context.FileSystem.FileExists(path))
            {
                Context.Trace.WriteLine($"Did not find helper '{helperName}' in '{executableDirectory}'");

                // We currently only have a helper on Windows. If we failed to find the helper we should warn the user.
                if (PlatformUtils.IsWindows())
                {
                    Context.StdError.WriteLine($"warning: missing '{helperName}' from installation.");
                }

                return false;
            }

            return true;
        }
    }
}
