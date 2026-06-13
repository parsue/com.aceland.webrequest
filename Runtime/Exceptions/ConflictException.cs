using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class ConflictException : HttpErrorException
    {
        public ConflictException(string message)
            : base(HttpStatusCode.NotFound, message)
        { }

        public ConflictException(string message, Exception inner)
            : base(HttpStatusCode.NotFound, message, inner)
        { }
    }
}