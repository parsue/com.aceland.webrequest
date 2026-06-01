using System;
using System.Net.Http;

namespace AceLand.WebRequest
{
    public enum RequestMethod
    {
        Post, Get, Put, Delete, Patch,
    }

    public static class RequestMethodExtensions
    {
        public static HttpMethod ToHttpMethod(this RequestMethod method) => method switch
        {
            RequestMethod.Post => HttpMethod.Post,
            RequestMethod.Get => HttpMethod.Get,
            RequestMethod.Put => HttpMethod.Put,
            RequestMethod.Delete => HttpMethod.Delete,
            RequestMethod.Patch => HttpMethod.Patch,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
    }
}