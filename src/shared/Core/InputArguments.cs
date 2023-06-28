using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GitCredentialManager
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
        private readonly IReadOnlyDictionary<string, IList<string>> _dict;

        public InputArguments(IDictionary<string, string> dict)
        {
            EnsureArgument.NotNull(dict, nameof(dict));

            // Transform input from 1:1 to 1:n and store as readonly
            _dict = new ReadOnlyDictionary<string, IList<string>>(
                dict.ToDictionary(x => x.Key, x => (IList<string>)new[] { x.Value })
            );
        }

        public InputArguments(IDictionary<string, IList<string>> dict)
        {
            EnsureArgument.NotNull(dict, nameof(dict));

            // Wrap the dictionary internally as readonly
            _dict = new ReadOnlyDictionary<string, IList<string>>(dict);
        }

        #region Common Arguments

        public string Protocol => GetArgumentOrDefault("protocol");
        public string Host     => GetArgumentOrDefault("host");
        public string Path     => GetArgumentOrDefault("path");
        public string UserName => GetArgumentOrDefault("username");
        public string Password => GetArgumentOrDefault("password");
        public IList<string> WwwAuth => GetMultiArgumentOrDefault("wwwauth");

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

        public IList<string> GetMultiArgumentOrDefault(string key)
        {
            return TryGetMultiArgument(key, out IList<string> values) ? values : Array.Empty<string>();
        }

        public bool TryGetArgument(string key, out string value)
        {
            if (_dict.TryGetValue(key, out IList<string> values))
            {
                value = values.FirstOrDefault();
                return value != null;
            }

            value = null;
            return false;
        }

        public bool TryGetMultiArgument(string key, out IList<string> value)
        {
            if (_dict.TryGetValue(key, out IList<string> values))
            {
                value = values;
                return true;
            }

            value = null;
            return false;
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
                var ub = new UriBuilder(Protocol, hostParts[0]);

                if (hostParts.Length > 1 && int.TryParse(hostParts[1], out int port))
                {
                    ub.Port = port;
                }

                if (includeUser && !string.IsNullOrEmpty(UserName))
                {
                    ub.UserName = Uri.EscapeDataString(UserName);
                }

                if (Path != null)
                {
                    string[] pathParts = Path.Split('?', '#');
                    // We know the first piece is the path
                    ub.Path = pathParts[0];

                    switch (pathParts.Length)
                    {
                        // If we have 3 items, that means path, query, and fragment
                        case 3:
                            ub.Query = pathParts[1];
                            ub.Fragment = pathParts[2];
                            break;
                        // If we have 2 items, we must distinguish between query and fragment
                        case 2 when Path.Contains('?'):
                            ub.Query = pathParts[1];
                            break;
                        case 2 when Path.Contains('#'):
                            ub.Fragment = pathParts[1];
                            break;
                    }
                }
                return ub.Uri;
            }

            return null;
        }

        #endregion
    }
}
