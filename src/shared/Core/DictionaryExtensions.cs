using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace GitCredentialManager
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Get the value of a dictionary entry as 'booleany' (either 'truthy' or 'falsey').
        /// </summary>
        /// <param name="dict">Dictionary.</param>
        /// <param name="key">Dictionary entry key.</param>
        /// <param name="defaultValue">Default value if the key is not present, or was neither 'truthy' or 'falsey'.</param>
        /// <returns>Dictionary entry value.</returns>
        /// <remarks>
        /// 'Truthy' and 'fasley' is defined by the implementation of <see cref="StringExtensions.IsTruthy"/> and <see cref="StringExtensions.IsFalsey"/>.
        /// </remarks>
        public static bool GetBooleanyOrDefault(this IReadOnlyDictionary<string, string> dict, string key, bool defaultValue)
        {
            if (dict.TryGetValue(key, out string value))
            {
                return value.ToBooleanyOrDefault(defaultValue);
            }

            return defaultValue;
        }

        public static string ToQueryString(this IDictionary<string, string> dict)
        {
            var sb = new StringBuilder();
            int i = 0;

            foreach (var kvp in dict)
            {
                string key  = HttpUtility.UrlEncode(kvp.Key);
                string value = HttpUtility.UrlEncode(kvp.Value);

                if (i > 0)
                {
                    sb.Append('&');
                }

                sb.AppendFormat("{0}={1}", key, value);

                i++;
            }

            return sb.ToString();
        }

        public static void Append<TKey, TValue>(this IDictionary<TKey, ICollection<TValue>> dict, TKey key, TValue value)
        {
            if (!dict.TryGetValue(key, out var values))
            {
                values = new List<TValue>();
                dict[key] = values;
            }

            values.Add(value);
        }

        public static IEnumerable<TValue> GetValues<TKey, TValue>(this IDictionary<TKey, IEnumerable<TValue>> dict, TKey key)
        {
            return dict.TryGetValue(key, out var values) ? values : Enumerable.Empty<TValue>();
        }
        
        public static IEnumerable<TValue> GetValues<TKey, TValue>(this IDictionary<TKey, ICollection<TValue>> dict, TKey key)
        {
            return dict.TryGetValue(key, out var values) ? values : Enumerable.Empty<TValue>();
        }

        public static IDictionary<TKey, IEnumerable<TValue>> ToDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> grouping)
        {
            return grouping.ToDictionary(x => x.Key, x => (IEnumerable<TValue>) x);
        }
    }
}
