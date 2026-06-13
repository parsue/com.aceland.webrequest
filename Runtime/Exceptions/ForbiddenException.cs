using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class ForbiddenException : HttpErrorException
    {
        public ForbiddenException(string message)
            : base(HttpStatusCode.NotFound, message)
        { }

        public ForbiddenException(string message, Exception inner)
            : base(HttpStatusCode.NotFound, message, inner)
        { }
    }
}