using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public class ServerErrorException : Exception
    {
        public ServerErrorException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
        
        public ServerErrorException(HttpStatusCode statusCode, string message, Exception inner)
            : base(message)
        {
            StatusCode = statusCode;
        }
        
        public HttpStatusCode StatusCode { get; }
    }
}