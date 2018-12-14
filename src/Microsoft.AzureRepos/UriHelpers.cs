// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Linq;
using Microsoft.Git.CredentialManager;

namespace Microsoft.AzureRepos
{
    internal static class UriHelpers
    {
        /// <summary>
        /// Combine two parts of a URI path element with the '/' character.
        /// </summary>
        /// <param name="basePath">Left/base URI path element.</param>
        /// <param name="path">Right/appending URI path element.</param>
        /// <returns>Concatenated URI path.</returns>
        public static string CombinePath(string basePath, string path)
        {
            if (basePath.Length > 0 && path.Length > 0)
            {
                char lastBasePath = basePath.Last();
                char firstPath = path.First();

                if (lastBasePath == '/' && firstPath == '/')
                {
                    return basePath + path.Substring(1);
                }
                if (lastBasePath != '/' && firstPath != '/')
                {
                    return basePath + '/' + path;
                }
            }

            return basePath + path;
        }

        /// <summary>
        /// Check if the hostname is a valid Azure DevOps hostname (dev.azure.com or *.visualstudio.com).
        /// </summary>
        /// <param name="host">Hostname to check</param>
        /// <returns>True if the hostname is Azure DevOps, false otherwise.</returns>
        public static bool IsAzureDevOpsHost(string host)
        {
            return host != null &&
                   (StringComparer.OrdinalIgnoreCase.Equals(host, AzureDevOpsConstants.AzureDevOpsHost) ||
                   host.EndsWith(AzureDevOpsConstants.VstsHostSuffix, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Create a URI for the Azure DevOps organization from the give Git input query arguments.
        /// </summary>
        /// <param name="input">Git query arguments.</param>
        /// <returns>Azure DevOps organization URI</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="InputArguments.Protocol"/> is null or white space.
        /// <para/>
        /// Thrown if <see cref="InputArguments.Host"/> is null or white space.
        /// <para/>
        /// Thrown if <see cref="InputArguments.Host"/> is not an Azure DevOps hostname.
        /// <para/>
        /// Thrown if both of <see cref="InputArguments.UserName"/> or <see cref="InputArguments.Path"/>
        /// are null or white space when <see cref="InputArguments.Host"/> is an Azure-style URL
        /// ('dev.azure.com' rather than '*.visualstudio.com').
        /// </exception>
        public static Uri CreateOrganizationUri(InputArguments input)
        {
            EnsureArgument.NotNull(input, nameof(input));

            if (string.IsNullOrWhiteSpace(input.Protocol))
            {
                throw new InvalidOperationException("Input arguments must include protocol");
            }

            if (string.IsNullOrWhiteSpace(input.Host))
            {
                throw new InvalidOperationException("Input arguments must include host");
            }

            if (!IsAzureDevOpsHost(input.Host))
            {
                throw new InvalidOperationException("Host is not Azure DevOps");
            }

            var ub = new UriBuilder
            {
                Scheme = input.Protocol,
                Host = input.Host,
            };

            // Extract the organization name for Azure ('dev.azure.com') style URLs.
            // The older *.visualstudio.com URLs contained the organization name in the host already.
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Host, AzureDevOpsConstants.AzureDevOpsHost))
            {
                // dev.azure.com/{org}
                string[] pathParts = input.Path?.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                if (pathParts?.Length > 0)
                {
                    ub.Path = pathParts[0];
                }
                // {org}@dev.azure.com
                else if (!string.IsNullOrWhiteSpace(input.UserName))
                {
                    ub.Path = input.UserName;
                }
                else
                {
                    throw new InvalidOperationException(
                        "Cannot determine the organization name for this 'dev.azure.com' remote URL. " +
                        "Ensure the `credential.useHttpPath` configuration value is set, or set the organization " +
                        "name as the user in the remote URL '{org}@dev.azure.com'."
                    );
                }
            }

            return ub.Uri;
        }
    }
}
