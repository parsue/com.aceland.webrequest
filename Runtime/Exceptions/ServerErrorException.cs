using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public class ServerErrorException : Exception
    {
        public ServerErrorException(HttpStatusCode statusCode, string body)
            : base($"HTTP {(int)statusCode} {statusCode}")
        {
            StatusCode = statusCode;
            ResponseBody = body;
        }
        
        public ServerErrorException(HttpStatusCode statusCode, string body, Exception inner)
            : base($"HTTP {(int)statusCode} {statusCode}", inner)
        {
            StatusCode = statusCode;
            ResponseBody = body;
        }
        
        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }
    }
}