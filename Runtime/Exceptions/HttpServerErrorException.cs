using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class HttpServerErrorException : Exception
    {
        public HttpServerErrorException(HttpStatusCode statusCode, string body)
            : base($"HTTP {(int)statusCode} {statusCode}")
        {
            StatusCode = statusCode;
            ResponseBody = body;
        }
        
        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }
    }
}