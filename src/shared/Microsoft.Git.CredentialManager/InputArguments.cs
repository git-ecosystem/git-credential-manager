// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents the input for a Git credential query such as get, erase, or store.
    /// </summary>
    /// <remarks>
    /// This class surfaces the input that is streamed over standard in from Git which provides
    /// the credential helper the remote repository information, including the protocol, host,
    /// and remote repository path.
    /// </remarks>
    public class InputArguments
    {
        private readonly IReadOnlyDictionary<string, string> _dict;

        public InputArguments(IDictionary<string, string> dict)
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }

            // Wrap the dictionary internally as readonly
            _dict = new ReadOnlyDictionary<string, string>(dict);
        }

        #region Common Arguments

        public string Protocol => GetArgumentOrDefault("protocol");
        public string Host     => GetArgumentOrDefault("host");
        public string Path     => GetArgumentOrDefault("path");
        public string UserName => GetArgumentOrDefault("username");
        public string Password => GetArgumentOrDefault("password");

        #endregion

        #region Public Methods

        public string this[string key]
        {
            get => GetArgumentOrDefault(key);
        }

        public string GetArgumentOrDefault(string key)
        {
            return TryGetArgument(key, out string value) ? value : null;
        }

        public bool TryGetArgument(string key, out string value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public bool TryGetHostAndPort(out string host, out int? port)
        {
            host = null;
            port = null;

            if (Host is null)
            {
                return false;
            }

            // Split port number and hostname from host input argument
            string[] hostParts = Host.Split(':');
            if (hostParts.Length > 0)
            {
                host = hostParts[0];
            }

            if (hostParts.Length > 1)
            {
                if (!int.TryParse(hostParts[1], out int portInt))
                {
                    return false;
                }

                port = portInt;
            }

            return true;
        }

        public Uri GetRemoteUri(bool includeUser = false)
        {
            if (Protocol is null || Host is null)
            {
                return null;
            }

            string[] hostParts = Host.Split(':');
            if (hostParts.Length > 0)
            {
                var ub = new UriBuilder(Protocol, hostParts[0])
                {
                    Path = Path
                };

                if (hostParts.Length > 1 && int.TryParse(hostParts[1], out int port))
                {
                    ub.Port = port;
                }

                if (includeUser && !string.IsNullOrEmpty(UserName))
                {
                    ub.UserName = Uri.EscapeDataString(UserName);
                }

                return ub.Uri;
            }

            return null;
        }

        #endregion
    }
}
