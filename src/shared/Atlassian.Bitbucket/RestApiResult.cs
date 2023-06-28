using System.Net;

namespace Atlassian.Bitbucket
{
    public class RestApiResult<T>
    {
        public RestApiResult(HttpStatusCode statusCode)
            : this(statusCode, default(T)) { }

        public RestApiResult(HttpStatusCode statusCode, T response)
        {
            StatusCode = statusCode;
            Response = response;
        }

        public HttpStatusCode StatusCode { get; }

        public T Response { get; }

        public bool Succeeded => 199 < (int)StatusCode && (int)StatusCode < 300;
    }
}
