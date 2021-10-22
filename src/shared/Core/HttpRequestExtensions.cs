using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace GitCredentialManager
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Add a basic authentication header to the request, with the given username and password.
        /// </summary>
        /// <remarks>
        /// The header value is formed by computing the base64 string from the UTF-8 string "{<paramref name="userName"/>}:{<paramref name="password"/>}".
        /// </remarks>
        public static void AddBasicAuthenticationHeader(this HttpRequestMessage request, string userName, string password)
        {
            string basicAuthValue = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", userName, password);
            byte[] authBytes = Encoding.UTF8.GetBytes(basicAuthValue);
            string base64String = Convert.ToBase64String(authBytes);
            request.Headers.Authorization = new AuthenticationHeaderValue(Constants.Http.WwwAuthenticateBasicScheme, base64String);
        }

        /// <summary>
        /// Add a bearer authentication header to the request, with the given bearer token.
        /// </summary>
        public static void AddBearerAuthenticationHeader(this HttpRequestMessage request, string bearerToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(Constants.Http.WwwAuthenticateBearerScheme, bearerToken);
        }
    }
}
