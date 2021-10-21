using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace GitCredentialManager
{
    public static class NameValueCollectionExtensions
    {
        public static IDictionary<string, string> ToDictionary(this NameValueCollection collection, IEqualityComparer<string> comparer = null)
        {
            var dict = new Dictionary<string, string>(comparer ?? StringComparer.Ordinal);

            foreach (string key in collection.AllKeys)
            {
                dict[key] = collection[key];
            }

            return dict;
        }
    }
}
