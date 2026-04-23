using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class UnauthorizedException : HttpErrorException
    {
        public UnauthorizedException(string body)
            : base(HttpStatusCode.Unauthorized, body)
        { }

        public UnauthorizedException(string body, Exception inner)
            : base(HttpStatusCode.Unauthorized, body, inner)
        { }
    }
}