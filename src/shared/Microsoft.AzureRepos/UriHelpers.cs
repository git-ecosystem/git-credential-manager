using System;
using System.Linq;
using GitCredentialManager;

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
        /// Check if the hostname is the legacy Azure DevOps hostname (*.visualstudio.com).
        /// </summary>
        /// <param name="input">Git query arguments.</param>
        /// <returns>True if the hostname is the legacy Azure DevOps host, false otherwise.</returns>
        public static bool IsVisualStudioComHost(InputArguments input)
        {
            EnsureArgument.NotNull(input, nameof(input));

            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                throw new InvalidOperationException("Host name and/or port is invalid.");
            }

            return IsVisualStudioComHost(hostName);
        }

        /// <summary>
        /// Check if the hostname is the legacy Azure DevOps hostname (*.visualstudio.com).
        /// </summary>
        /// <param name="host">Hostname to check.</param>
        /// <returns>True if the hostname is the legacy Azure DevOps host, false otherwise.</returns>
        public static bool IsVisualStudioComHost(string host)
        {
            return host != null &&
                   host.EndsWith(AzureDevOpsConstants.VstsHostSuffix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if the hostname is the new Azure DevOps hostname (dev.azure.com).
        /// </summary>
        /// <param name="input">Git query arguments.</param>
        /// <returns>True if the hostname is the new Azure DevOps host, false otherwise.</returns>
        public static bool IsDevAzureComHost(InputArguments input)
        {
            EnsureArgument.NotNull(input, nameof(input));

            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                throw new InvalidOperationException("Host name and/or port is invalid.");
            }

            return IsDevAzureComHost(hostName);
        }

        /// <summary>
        /// Check if the hostname is the new Azure DevOps hostname (dev.azure.com).
        /// </summary>
        /// <param name="host">Hostname to check.</param>
        /// <returns>True if the hostname is the new Azure DevOps host, false otherwise.</returns>
        public static bool IsDevAzureComHost(string host)
        {
            return host != null &&
                   StringComparer.OrdinalIgnoreCase.Equals(host, AzureDevOpsConstants.AzureDevOpsHost);
        }

        /// <summary>
        /// Check if the hostname is a valid Azure DevOps hostname (dev.azure.com or *.visualstudio.com).
        /// </summary>
        /// <param name="host">Hostname to check</param>
        /// <returns>True if the hostname is Azure DevOps, false otherwise.</returns>
        public static bool IsAzureDevOpsHost(string host)
        {
            return IsVisualStudioComHost(host) || IsDevAzureComHost(host);
        }

        public static string GetOrganizationName(Uri remoteUri)
        {
            CreateOrganizationUri(remoteUri, out string orgName);
            return orgName;
        }

        /// <summary>
        /// Create a URI for the Azure DevOps organization from the Git remote URI.
        /// </summary>
        /// <param name="remoteUri">Git remote URI arguments.</param>
        /// <param name="orgName">Azure DevOps organization name.</param>
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
        public static Uri CreateOrganizationUri(Uri remoteUri, out string orgName)
        {
            EnsureArgument.AbsoluteUri(remoteUri, nameof(remoteUri));

            orgName = null;

            if (!IsAzureDevOpsHost(remoteUri.Host))
            {
                throw new InvalidOperationException("Host is not Azure DevOps.");
            }

            var ub = new UriBuilder
            {
                Scheme = remoteUri.Scheme,
                Host = remoteUri.Host,
            };

            if (!remoteUri.IsDefaultPort)
            {
                ub.Port = remoteUri.Port;
            }

            // Extract the organization name for Azure ('dev.azure.com') style URLs.
            // The older *.visualstudio.com URLs contained the organization name in the host already.
            if (IsDevAzureComHost(remoteUri.Host))
            {
                string firstPathComponent = GetFirstPathComponent(remoteUri.AbsolutePath);
                string remoteUriUserName = remoteUri.GetUserName();

                // Prefer getting the org name from the path: dev.azure.com/{org}
                if (!string.IsNullOrWhiteSpace(firstPathComponent))
                {
                    orgName = firstPathComponent;
                }
                // Failing that try using the username: {org}@dev.azure.com
                else if (!string.IsNullOrWhiteSpace(remoteUriUserName))
                {
                    orgName = remoteUriUserName;
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
            else if (IsVisualStudioComHost(remoteUri.Host))
            {
                // {org}.visualstudio.com
                int orgNameLength = remoteUri.Host.Length - AzureDevOpsConstants.VstsHostSuffix.Length;
                orgName = remoteUri.Host.Substring(0, orgNameLength);
            }

            return ub.Uri;
        }

        public static string GetFirstPathComponent(string path)
        {
            string[] parts = path?.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (parts?.Length > 0)
            {
                return parts[0];
            }

            return null;
        }
    }
}
