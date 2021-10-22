using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GitCredentialManager
{
    public static class HttpContentExtensions
    {
        public static async Task<IDictionary<string, string>> ReadAsFormContentAsync(this HttpContent content)
        {
            string str = await content.ReadAsStringAsync();
            return UriExtensions.ParseQueryString(str);
        }
    }
}
