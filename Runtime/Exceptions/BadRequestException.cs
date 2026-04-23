using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class BadRequestException : HttpErrorException
    {
        public BadRequestException(string body)
            : base(HttpStatusCode.BadRequest, body)
        { }

        public BadRequestException(string body, Exception inner)
            : base(HttpStatusCode.BadRequest, body, inner)
        { }
    }
}