using System;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class HttpServerErrorException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }

        public HttpServerErrorException(System.Net.HttpStatusCode statusCode, string body)
            : base($"HTTP {(int)statusCode} {statusCode}")
        {
            StatusCode = statusCode;
            ResponseBody = body;
        }
    }
}