using System;
using System.Net;

namespace AceLand.WebRequest.Exceptions
{
    public sealed class NotFoundException : HttpErrorException
    {
        public NotFoundException(string body)
            : base(HttpStatusCode.NotFound, body)
        { }

        public NotFoundException(string body, Exception inner)
            : base(HttpStatusCode.NotFound, body, inner)
        { }
    }
}