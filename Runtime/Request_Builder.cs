using System;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.Handle;
using UnityEngine;
using ZLinq;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        public interface IRequestBuilder
        {
            IRequestHandle Build();
            IRequestBuilder WithSection(string section);
            IRequestBuilder WithoutSection();
            IRequestBuilder WithHeader(string key, string value);
            IRequestBuilder WithParam(string key, string value);
            IRequestBuilder WithLongRequest(); 
            IRequestBuilder WithTimeout(int ms); 
        }

        public interface IUrlBuilder
        {
            IRequestBuilder WithUrl(string url);
        }

        private class RequestHandleBuilder : IUrlBuilder, IRequestBuilder
        {
            internal RequestHandleBuilder(RequestMethod requestMethod)
            {
                _body.RequestMethod = requestMethod;
            }

            private bool _withoutSection;
            private string _section = string.Empty;
            private string _url = string.Empty;
            private readonly JsonBody _body = new();

            public IRequestHandle Build()
            {
                if (!_withoutSection)
                    _section = Settings.DefaultSection;
                
                Settings.TryGetSection(_section, out var section);
                if (_withoutSection) section = null;
                
                if (section && !_url.StartsWith("http"))
                    _url = section.ApiUrl + _url;

                if (!CheckUrl(_url))
                    throw new Exception($"Invalid Url : {_url}");
                
                _body.Url = _url;

                var headers = GetHeader(section == null ? null : section.SectionName);
                foreach (var header in headers)
                {
                    if (_body.Headers.AsValueEnumerable().Any(h => h.Key == header.Key)) continue;
                    _body.Headers.Add(header);
                }
                
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
                if (ms < 0)
                {
                    Debug.LogWarning($"Timeout Ignored: {ms}ms is not accepted");
                    return this;
                }
                
                _body.Timeout = ms;
                return this;
            }
            
            public IRequestBuilder WithUrl(string url)
            {
                if (string.IsNullOrEmpty(url.Trim()))
                    throw new ArgumentNullException(nameof(url));
                
                _url = url;
                return this;
            }

            public IRequestBuilder WithSection(string section)
            {
                _withoutSection = false;
                if (!Settings.ContainSection(section))
                    throw new Exception($"Section {section} is not contained in request builder");
                
                _section = section;
                return this;
            }

            public IRequestBuilder WithoutSection()
            {
                _withoutSection = true;
                _section = string.Empty;
                return this;
            }

            public IRequestBuilder WithHeader(string key, string value)
            {
                AddHeader(key, value);
                return this;
            }

            private void AddHeader(string key, string value)
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    Debug.LogWarning($"Header Ignored: key or value is null or empty. Key: {key??"null"}, Value: {value??"null"}");
                    return;
                }
                
                var index = -1;
                for (var i = 0; i < _body.Headers.Count; i++)
                {
                    var header = _body.Headers[i];
                    if (header.Key != key) continue;
                    index = i;
                    break;
                }
                if (index != -1) _body.Headers.RemoveAt(index);
                _body.Headers.Add(new FormData(key, value));
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