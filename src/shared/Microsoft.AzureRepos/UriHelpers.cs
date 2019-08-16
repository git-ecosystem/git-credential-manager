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
        /// Extract the Azure DevOps organization name from the given remote URI.
        /// </summary>
        /// <param name="remoteUri">Remote URL.</param>
        /// <returns>Azure DevOps organization URI</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="InputArguments.Host"/> is not an Azure DevOps hostname.
        /// <para/>
        /// Thrown if both of <see cref="Uri.UserInfo"/> or <see cref="Uri.AbsolutePath"/>
        /// are null or white space when <see cref="Uri.Host"/> is an Azure-style URL
        /// ('dev.azure.com' rather than '*.visualstudio.com').
        /// </exception>
        public static string GetOrganizationName(Uri remoteUri)
        {
            CreateOrganizationUri(remoteUri, out string orgName);
            return orgName;
        }

        /// <summary>
        /// Create a URI for the Azure DevOps organization from the given remote URI.
        /// </summary>
        /// <param name="remoteUri">Remote URL.</param>
        /// <returns>Azure DevOps organization URI</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="InputArguments.Host"/> is not an Azure DevOps hostname.
        /// <para/>
        /// Thrown if both of <see cref="Uri.UserInfo"/> or <see cref="Uri.AbsolutePath"/>
        /// are null or white space when <see cref="Uri.Host"/> is an Azure-style URL
        /// ('dev.azure.com' rather than '*.visualstudio.com').
        /// </exception>
        public static Uri CreateOrganizationUri(Uri remoteUri) => CreateOrganizationUri(remoteUri, out _);

        /// <summary>
        /// Create a URI for the Azure DevOps organization from the given remote URI, also returning the
        /// organization name.
        /// </summary>
        /// <param name="remoteUri">Remote URL.</param>
        /// <param name="orgName">Azure DevOps organization name.</param>
        /// <returns>Azure DevOps organization URI</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="InputArguments.Host"/> is not an Azure DevOps hostname.
        /// <para/>
        /// Thrown if both of <see cref="Uri.UserInfo"/> or <see cref="Uri.AbsolutePath"/>
        /// are null or white space when <see cref="Uri.Host"/> is an Azure-style URL
        /// ('dev.azure.com' rather than '*.visualstudio.com').
        /// </exception>
        public static Uri CreateOrganizationUri(Uri remoteUri, out string orgName)
        {
            EnsureArgument.NotNull(remoteUri, nameof(remoteUri));

            if (!IsAzureDevOpsHost(remoteUri.Host))
            {
                throw new InvalidOperationException("Host is not Azure DevOps");
            }

            var ub = new UriBuilder
            {
                Scheme = remoteUri.Scheme,
                Host = remoteUri.Host,
            };

            // Extract the organization name for Azure ('dev.azure.com') style URLs.
            // The older *.visualstudio.com URLs contained the organization name in the host already.
            if (StringComparer.OrdinalIgnoreCase.Equals(remoteUri.Host, AzureDevOpsConstants.AzureDevOpsHost))
            {
                string[] pathParts = remoteUri.AbsolutePath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                string userName = remoteUri.UserInfo.Split(':')[0];

                // dev.azure.com/{org}
                if (pathParts.Length > 0)
                {
                    orgName = pathParts[0];
                }
                // {org}@dev.azure.com
                else if (!string.IsNullOrWhiteSpace(userName))
                {
                    orgName = userName;
                }
                else
                {
                    throw new InvalidOperationException(
                        "Cannot determine the organization name for this 'dev.azure.com' remote URL. " +
                        "Ensure the `credential.useHttpPath` configuration value is set, or set the organization " +
                        "name as the user in the remote URL '{org}@dev.azure.com'."
                    );
                }

                ub.Path = orgName;
            }
            // visualstudio.com URLs have the organization name is the sub-domain
            else if (remoteUri.Host.EndsWith(AzureDevOpsConstants.VstsHostSuffix, StringComparison.OrdinalIgnoreCase))
            {
                orgName = remoteUri.Host.Substring(0, remoteUri.Host.Length - AzureDevOpsConstants.VstsHostSuffix.Length);
            }
            else
            {
                throw new InvalidOperationException("Unknown Azure DevOps URL");
            }

            return ub.Uri;
        }
    }
}
