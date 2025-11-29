using System;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.Handle;
using UnityEngine;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        public interface IRequestBuilder
        {
            IRequestHandle Build();
            IRequestBuilder WithHeader(string key, string value);
            IRequestBuilder WithParam(string key, string value);
            IRequestBuilder WithLongRequest(); 
            IRequestBuilder WithTimeout(int ms); 
        }

        public interface IUrlBuilder
        {
            IRequestBuilder WithUrl(Uri url);
        }

        private class RequestHandleBuilder : IUrlBuilder, IRequestBuilder
        {
            internal RequestHandleBuilder(RequestMethod requestMethod)
            {
                _body.RequestMethod = requestMethod;
                _body.Header.AddRange(GetHeader());
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
                AddHeader(key, value);
                return this;
            }

            private void AddHeader(string key, string value)
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) return;
                
                var index = -1;
                for (var i = 0; i < _body.Header.Count; i++)
                {
                    var header = _body.Header[i];
                    if (header.Key != key) continue;
                    index = i;
                    break;
                }
                if (index != -1) _body.Header.RemoveAt(index);
                _body.Header.Add(new FormData(key, value));
            }

            public IRequestBuilder WithParam(string key, string value)
            {
                AddParameter(key, value);
                return this;
            }

            private void AddParameter(string key, string value)
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    Debug.LogWarning($"Parameter Ignored: key or value is null or empty. Key: {key??"null"}, Value: {value??"null"}");
                    return;
                }
                
                var index = -1;
                for (var i = 0; i < _body.Parameters.Count; i++)
                {
                    var header = _body.Parameters[i];
                    if (header.Key != key) continue;
                    index = i;
                    break;
                }
                if (index != -1) _body.Parameters.RemoveAt(index);
                _body.Parameters.Add(new FormData(key, value));
            }
        }
    }
}