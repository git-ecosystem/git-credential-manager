using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Git.CredentialManager
{
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

        #endregion
    }
}
