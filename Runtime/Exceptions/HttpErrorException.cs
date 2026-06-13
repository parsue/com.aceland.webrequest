using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public class HttpErrorException : Exception
    {
        public HttpErrorException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
        
        public HttpErrorException(HttpStatusCode statusCode, string message, Exception inner)
            : base(message, inner)
        {
            StatusCode = statusCode;
        }
        
        public HttpStatusCode StatusCode { get; }
    }
}