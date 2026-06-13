using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class UnauthorizedException : HttpErrorException
    {
        public UnauthorizedException(string message)
            : base(HttpStatusCode.Unauthorized, message)
        { }

        public UnauthorizedException(string message, Exception inner)
            : base(HttpStatusCode.Unauthorized, message, inner)
        { }
    }
}