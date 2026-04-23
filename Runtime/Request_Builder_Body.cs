using System;
using System.Collections.Generic;
using System.IO;
using AceLand.Library.Extensions;
using AceLand.Serialization.Json;
using AceLand.Serialization.Models;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.Handle;
using UnityEngine;
using ZLinq;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        public interface IUrlBodyBuilder
        {
            IContentTypeBuilder WithUrl(string url);
        }

        public interface IContentTypeBuilder
        {
            IRequestJsonBodyBuilder WithJsonContent();
            IRequestFormBodyBuilder WithFormContent();
            IRequestMultipartBodyBuilder WithMultipartContent();
        }

        public interface IRequestBodyBuilder
        {
            IRequestHandle Build();
        }
        
        public interface IRequestJsonBodyBuilder : IRequestBodyBuilder
        {
            IRequestJsonBodyBuilder WithContent(string json);
            IRequestJsonBodyBuilder WithContent(string key, string value);
            IRequestJsonBodyBuilder WithSection(string section);
            IRequestJsonBodyBuilder WithoutSection();
            IRequestJsonBodyBuilder WithHeader(string key, string value);
            IRequestJsonBodyBuilder WithParam(string key, string value);
            IRequestJsonBodyBuilder WithLongRequest(); 
            IRequestJsonBodyBuilder WithTimeout(int ms); 
        }

        public interface IRequestFormBodyBuilder : IRequestBodyBuilder
        {
            IRequestFormBodyBuilder WithContent(string key, string value);
            IRequestFormBodyBuilder WithSection(string section);
            IRequestFormBodyBuilder WithoutSection();
            IRequestFormBodyBuilder WithHeader(string key, string value);
            IRequestFormBodyBuilder WithParam(string key, string value);
            IRequestFormBodyBuilder WithLongRequest(); 
            IRequestFormBodyBuilder WithTimeout(int ms); 
        }

        public interface IRequestMultipartBodyBuilder : IRequestBodyBuilder
        {
            IRequestMultipartBodyBuilder WithContent(string key, string value);
            IRequestMultipartBodyBuilder WithStreamData(string key, string filePath, string fileName);
            IRequestMultipartBodyBuilder WithStreamData(string key, byte[] data, string fileName);
            IRequestMultipartBodyBuilder WithStreamData(string key, Stream content, string fileName);
            IRequestMultipartBodyBuilder WithSection(string section);
            IRequestMultipartBodyBuilder WithoutSection();
            IRequestMultipartBodyBuilder WithHeader(string key, string value);
            IRequestMultipartBodyBuilder WithParam(string key, string value);
            IRequestMultipartBodyBuilder WithLongRequest(); 
            IRequestMultipartBodyBuilder WithTimeout(int ms); 
        }

        private class RequestBodyHandleBuilder : IUrlBodyBuilder, IContentTypeBuilder,
            IRequestJsonBodyBuilder, IRequestFormBodyBuilder, IRequestMultipartBodyBuilder
        {
            internal RequestBodyHandleBuilder(RequestMethod requestMethod)
            {
                _requestMethod = requestMethod;
            }

            private readonly RequestMethod _requestMethod;
            private string _url;
            private bool _withoutSection;
            private string _section = string.Empty;
            private float _timeout = -1;
            private (string key, string token) _token;
            private DataType _dataType;
            private readonly List<FormData> _headers = new();
            private readonly List<FormData> _parameters = new();
            private string _jsonBody = string.Empty;
            private readonly List<FormData> _bodyData = new();
            private readonly List<StreamData> _streamData = new();

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
                
                if (_timeout <= 0)
                    _timeout = Settings.RequestTimeout;
                
                if (!_token.key.IsNullOrEmptyOrWhiteSpace() && !_token.token.IsNullOrEmptyOrWhiteSpace())
                    _headers.Add(new FormData(_token.key, _token.token));

                var headers = GetHeader(section == null ? null : section.SectionName);
                foreach (var header in headers)
                {
                    if (_headers.AsValueEnumerable().Any(h => h.Key == header.Key)) continue;
                    _headers.Add(header);
                }

                var fingerprint = _withoutSection ? null : section.RootCaFingerprint;
                
                return new RequestHandle(CreateBody(fingerprint));
            }

            IRequestMultipartBodyBuilder IRequestMultipartBodyBuilder.WithSection(string section)
            {
                throw new NotImplementedException();
            }

            IRequestMultipartBodyBuilder IRequestMultipartBodyBuilder.WithoutSection()
            {
                throw new NotImplementedException();
            }

            IRequestMultipartBodyBuilder IRequestMultipartBodyBuilder.WithHeader(string key, string value)
            {
                throw new NotImplementedException();
            }

            IRequestMultipartBodyBuilder IRequestMultipartBodyBuilder.WithParam(string key, string value)
            {
                throw new NotImplementedException();
            }

            IRequestMultipartBodyBuilder IRequestMultipartBodyBuilder.WithLongRequest()
            {
                _timeout = Settings.LongRequestTimeout;
                return this;
            }

            IRequestMultipartBodyBuilder IRequestMultipartBodyBuilder.WithTimeout(int ms)
            {
                if (ms < 0)
                {
                    Debug.LogWarning($"Timeout Ignored: {ms}ms is not accepted");
                    return this;
                }
                
                _timeout = ms;
                return this;
            }

            IRequestFormBodyBuilder IRequestFormBodyBuilder.WithSection(string section)
            {
                _withoutSection = false;
                if (!Settings.ContainSection(section))
                    throw new Exception($"Section {section} is not contained in request builder");
                
                _section = section;
                return this;
            }

            IRequestFormBodyBuilder IRequestFormBodyBuilder.WithoutSection()
            {
                _withoutSection = true;
                _section = string.Empty;
                return this;
            }

            IRequestFormBodyBuilder IRequestFormBodyBuilder.WithHeader(string key, string value)
            {
                AddHeader(key, value);
                return this;
            }

            IRequestFormBodyBuilder IRequestFormBodyBuilder.WithParam(string key, string value)
            {
                AddParameter(key, value);
                return this;
            }

            IRequestFormBodyBuilder IRequestFormBodyBuilder.WithLongRequest()
            {
                _timeout = Settings.LongRequestTimeout;
                return this;
            }

            IRequestFormBodyBuilder IRequestFormBodyBuilder.WithTimeout(int ms)
            {
                if (ms < 0)
                {
                    Debug.LogWarning($"Timeout Ignored: {ms}ms is not accepted");
                    return this;
                }
                
                _timeout = ms;
                return this;
            }

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithSection(string section)
            {
                _withoutSection = false;
                if (!Settings.ContainSection(section))
                    throw new Exception($"Section {section} is not contained in request builder");
                
                _section = section;
                return this;
            }

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithoutSection()
            {
                _withoutSection = true;
                _section = string.Empty;
                return this;
            }

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithHeader(string key, string value)
            {
                AddHeader(key, value);
                return this;
            }

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithParam(string key, string value)
            {
                AddParameter(key, value);
                return this;
            }

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithLongRequest()
            {
                _timeout = Settings.LongRequestTimeout;
                return this;
            }

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithTimeout(int ms)
            {
                if (ms < 0)
                {
                    Debug.LogWarning($"Timeout Ignored: {ms}ms is not accepted");
                    return this;
                }
                
                _timeout = ms;
                return this;
            }

            public IContentTypeBuilder WithUrl(string url)
            {
                _url = url;
                return this;
            }

            public IRequestJsonBodyBuilder WithJsonContent()
            {
                _dataType = DataType.Json;
                return this;
            }

            public IRequestFormBodyBuilder WithFormContent()
            {
                _dataType = DataType.Form;
                return this;
            }

            public IRequestMultipartBodyBuilder WithMultipartContent()
            {
                _dataType = DataType.Multipart;
                return this;
            }

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithContent(string json)
            {
                if (json.IsNullOrEmptyOrWhiteSpace()) return this;
                if (Settings.CheckJsonBeforeSend && !json.IsValidJson())
                    throw new Exception("json format is not correct");
                
                _jsonBody = json;
                return this;
            }

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithContent(string key, string value)
            {
                if (key.IsNullOrEmptyOrWhiteSpace() || value.IsNullOrEmptyOrWhiteSpace())
                    return this;

                if (_jsonBody.IsNullOrEmptyOrWhiteSpace())
                {
                    _jsonBody = $"{{\"{key}\":\"{value}\"}}";
                    return this;
                }

                var jsonData = JsonData.Builder().WithText(_jsonBody).Build();
                var items = jsonData.Container.ToObject<Dictionary<string, object>>();
                items[key] = value;
                _jsonBody = items.ToJson().Text;
                return this;
            }

            IRequestFormBodyBuilder IRequestFormBodyBuilder.WithContent(string key, string value)
            {
                _bodyData.Add(new FormData(key, value));
                return this;
            }

            IRequestMultipartBodyBuilder IRequestMultipartBodyBuilder.WithContent(string key, string value)
            {
                _headers.Add(new FormData(key, value));
                return this;
            }

            public IRequestMultipartBodyBuilder WithStreamData(string key, string filePath, string fileName)
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("File not found", filePath);
                
                var content = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                _streamData.Add(new StreamData(key, content, fileName));
                return this;
            }

            public IRequestMultipartBodyBuilder WithStreamData(string key, byte[] data, string fileName)
            {
                if (data == null || data.Length == 0)
                    throw new InvalidDataException("Data cannot be empty");
                
                var content = new MemoryStream(data);
                _streamData.Add(new StreamData(key, content, fileName));
                return this;
            }

            public IRequestMultipartBodyBuilder WithStreamData(string key, Stream content, string fileName)
            {
                if (content == null)
                    throw new InvalidDataException("Content cannot be empty");
                
                _streamData.Add(new StreamData(key, content, fileName));
                return this;
            }

            private IRequestBody CreateBody(string fingerprint)
            {
                switch (_dataType)
                {
                    case DataType.Json:
                        var jsonBody = new JsonBody();
                        jsonBody.RequestMethod = _requestMethod;
                        jsonBody.Url = _url;
                        jsonBody.Timeout = _timeout;
                        jsonBody.Headers.AddRange(_headers);
                        jsonBody.Parameters.AddRange(_parameters);
                        jsonBody.Body = _jsonBody;
                        jsonBody.Fingerprint = fingerprint;
                        return jsonBody;
                    case DataType.Form:
                        var formBody = new FormBody();
                        formBody.RequestMethod = _requestMethod;
                        formBody.Url = _url;
                        formBody.Timeout = _timeout;
                        formBody.Headers.AddRange(_headers);
                        formBody.Parameters.AddRange(_parameters);
                        formBody.Body.AddRange(_bodyData);
                        formBody.Fingerprint = fingerprint;
                        return formBody;
                    case DataType.Multipart:
                        var multipartBody = new MultipartBody();
                        multipartBody.RequestMethod = _requestMethod;
                        multipartBody.Url = _url;
                        multipartBody.Timeout = _timeout;
                        multipartBody.Headers.AddRange(_headers);
                        multipartBody.Parameters.AddRange(_parameters);
                        multipartBody.Body.AddRange(_bodyData);
                        multipartBody.StreamData.AddRange(_streamData);
                        multipartBody.Fingerprint = fingerprint;
                        return null;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private void AddHeader(string key, string value)
            {
                if (_headers.AsValueEnumerable()
                    .Any(header => header.Key == key))
                    return;

                _bodyData.Add(new FormData(key, value));
            }

            private void AddParameter(string key, string value)
            {
                if (_parameters.AsValueEnumerable()
                    .Any(param => param.Key == key))
                    return;

                _bodyData.Add(new FormData(key, value));
            }
        }
    }
}
