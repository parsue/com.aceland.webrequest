using System;

namespace AceLand.WebRequest
{
    public static class RequestExtension
    {
        public static Uri ToUri(this string url) => new(url);
    }
}