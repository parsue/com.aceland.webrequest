using System;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.Handle;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        public interface IRequestBuilder
        {
            IRequestHandle Build();
            IRequestBuilder WithLongRequest(); 
            IRequestBuilder WithTimeout(int ms); 
        }

        public interface IUrlBuilder
        {
            IRequestBuilder WithUrl(Uri url);
        }
        
        public interface IHeaderBuilder : IRequestBuilder
        {
            IRequestBuilder WithHeader(string key, string value);
        }

        private class RequestHandleBuilder : IUrlBuilder, IHeaderBuilder
        {
            internal RequestHandleBuilder(RequestMethod requestMethod)
            {
                _body.RequestMethod = requestMethod;
                _body.Header.AddRange(AutoFillHeader());
            }

            private readonly JsonBody _body = new();

            public IRequestHandle Build()
            {
                if (!CheckUrl(_body.Url))
                    throw new Exception($"Url is not https scheme : {_body.Url}");

                if (_body.Timeout <= 0)
                    _body.Timeout = Settings.RequestTimeout;

                return new RequestHandle(_body);
            }

            public IRequestBuilder WithLongRequest()
            {
                _body.Timeout = Settings.LongRequestTimeout;
                return this;
            }

            public IRequestBuilder WithTimeout(int ms)
            {
                _body.Timeout = ms;
                return this;
            }
            
            public IRequestBuilder WithUrl(Uri url)
            {
                _body.Url = url;
                return this;
            }

            public IRequestBuilder WithHeader(string key, string value)
            {
                _body.Header.Add(new FormData(key, value));
                return this;
            }
        }
    }
}