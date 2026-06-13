using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class BadRequestException : HttpErrorException
    {
        public BadRequestException(string message)
            : base(HttpStatusCode.BadRequest, message)
        { }

        public BadRequestException(string message, Exception inner)
            : base(HttpStatusCode.BadRequest, message, inner)
        { }
    }
}