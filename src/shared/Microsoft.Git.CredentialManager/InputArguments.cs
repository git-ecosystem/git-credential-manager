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
            return _dict.TryGetValue(key, out string value) ? value : null;
        }

        public Uri GetRemoteUri()
        {
            if (Protocol is null || Host is null)
            {
                return null;
            }

            var ub = new UriBuilder(Protocol, Host)
            {
                Path = Path
            };

            return ub.Uri;
        }

        #endregion
    }
}
