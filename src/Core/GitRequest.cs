using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GitCredentialManager
{
    /// <summary>
    /// Represents the request for a single Git credential helper invocation (get / store / erase).
    /// </summary>
    /// <remarks>
    /// Surfaces the input streamed over standard input from Git, including the
    /// protocol, host, and remote repository path, plus any negotiated protocol
    /// capabilities.
    /// </remarks>
    public class GitRequest
    {
        private readonly IReadOnlyDictionary<string, IList<string>> _dict;
        private GitCapabilities? _capabilities;
        private IReadOnlyDictionary<string, string> _state;

        public GitRequest(IDictionary<string, string> dict)
        {
            EnsureArgument.NotNull(dict, nameof(dict));

            // Transform input from 1:1 to 1:n and store as readonly
            _dict = new ReadOnlyDictionary<string, IList<string>>(
                dict.ToDictionary(x => x.Key, x => (IList<string>)new[] { x.Value })
            );
        }

        public GitRequest(IDictionary<string, IList<string>> dict)
        {
            EnsureArgument.NotNull(dict, nameof(dict));

            // Wrap the dictionary internally as readonly
            _dict = new ReadOnlyDictionary<string, IList<string>>(dict);
        }

        /// <summary>
        /// The set of Git credential protocol capabilities that Git itself advertised
        /// it supports on this invocation. Unrecognized capability names are silently
        /// discarded per the protocol specification.
        /// </summary>
        public GitCapabilities Capabilities => _capabilities ??= ParseCapabilities();

        public string Protocol => GetArgumentOrDefault("protocol");
        public string Host     => GetArgumentOrDefault("host");
        public string Path     => GetArgumentOrDefault("path");
        public string UserName => GetArgumentOrDefault("username");
        public string Password => GetArgumentOrDefault("password");
        public IList<string> WwwAuth => GetMultiArgumentOrDefault("wwwauth");

        /// <summary>
        /// Opaque per-helper state Git is replaying from a previous invocation,
        /// gated by the <c>state</c> capability.
        /// </summary>
        /// <remarks>
        /// Only entries with our recognized prefix (<see cref="Constants.CredentialProtocol.GcmStatePrefix"/>)
        /// are kept. The prefix is stripped from dictionary keys. Malformed entries
        /// (missing <c>=</c>, invalid key/value characters) are silently discarded.
        /// </remarks>
        public IReadOnlyDictionary<string, string> State => _state ??= ParseState();

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

        private GitCapabilities ParseCapabilities()
        {
            var caps = GitCapabilities.None;

            IList<string> values = GetMultiArgumentOrDefault("capability");
            foreach (string name in values)
            {
                caps |= GitCapabilitiesUtils.ParseName(name);
            }

            return caps;
        }

        private IReadOnlyDictionary<string, string> ParseState()
        {
            const string prefix = Constants.CredentialProtocol.GcmStatePrefix;
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            IList<string> values = GetMultiArgumentOrDefault(Constants.CredentialProtocol.StateKey);
            foreach (string entry in values)
            {
                int sep = entry.IndexOf('=');
                if (sep <= 0)
                {
                    // Malformed (no '=' or empty key): per the protocol
                    // "unrecognized attributes are silently discarded".
                    continue;
                }

                string rawKey = entry.Substring(0, sep);
                string value = entry.Substring(sep + 1);

                // Only consume our own namespace; let other helpers' state pass
                // through us untouched (we never see it once Git stores it
                // per-helper, but the protocol mandates this discipline).
                if (!rawKey.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string key = rawKey.Substring(prefix.Length);

                // Defensive: validate the post-prefix key and the value.
                // Git should never hand us malformed entries, but skip any
                // that wouldn't round-trip through our own emitter.
                if (!GitStateValidation.IsValidKey(key) || !GitStateValidation.IsValidValue(value))
                {
                    continue;
                }

                result[key] = value;
            }

            return new ReadOnlyDictionary<string, string>(result);
        }
    }
}
