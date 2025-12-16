using System;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class HttpErrorException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }

        public HttpErrorException(System.Net.HttpStatusCode statusCode, string body)
            : base($"HTTP {(int)statusCode} {statusCode}")
        {
            StatusCode = statusCode;
            ResponseBody = body;
        }
    }
}