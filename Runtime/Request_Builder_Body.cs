using System;
using System.Collections.Generic;
using System.IO;
using AceLand.Library.Extensions;
using AceLand.Library.Json;
using AceLand.Library.Models;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.Handle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ZLinq;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        public interface IUrlBodyBuilder
        {
            IContentTypeBuilder WithUrl(Uri url);
        }

        public interface IContentTypeBuilder
        {
            IRequestJsonBodyBuilder WithJsonBody();
            IRequestFormBodyBuilder WithFormBody();
            IRequestMultipartBodyBuilder WithMultipartBody();
        }
        
        public interface IRequestJsonBodyBuilder : IRequestBuilder
        {
            IRequestJsonBodyBuilder WithContent(string json);
            IRequestJsonBodyBuilder WithContent(string key, string value);
        }

        public interface IRequestFormBodyBuilder : IRequestBuilder
        {
            IRequestFormBodyBuilder WithContent(string key, string value);
        }

        public interface IRequestMultipartBodyBuilder : IRequestBuilder
        {
            IRequestMultipartBodyBuilder WithContent(string key, string value);
            IRequestMultipartBodyBuilder WithStreamData(string key, string filePath, string fileName);
            IRequestMultipartBodyBuilder WithStreamData(string key, byte[] data, string fileName);
            IRequestMultipartBodyBuilder WithStreamData(string key, Stream content, string fileName);
        }

        private class RequestBodyHandleBuilder : IUrlBodyBuilder, IContentTypeBuilder,
            IRequestJsonBodyBuilder, IRequestFormBodyBuilder, IRequestMultipartBodyBuilder
        {
            internal RequestBodyHandleBuilder(RequestMethod requestMethod)
            {
                _requestMethod = requestMethod;
                _headers.AddRange(GetHeader());
            }

            private readonly RequestMethod _requestMethod;
            private Uri _url;
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
                if (!CheckUrl(_url))
                    throw new Exception($"Url is not https scheme : {_url}");

                if (_timeout <= 0)
                    _timeout = Settings.RequestTimeout;
                
                if (!_token.key.IsNullOrEmptyOrWhiteSpace() && !_token.token.IsNullOrEmptyOrWhiteSpace())
                    _headers.Add(new FormData(_token.key, _token.token));

                return new RequestHandle(CreateBody());
            }

            public IRequestBuilder WithLongRequest()
            {
                _timeout = Settings.LongRequestTimeout;
                return this;
            }

            public IRequestBuilder WithTimeout(int ms)
            {
                _timeout = ms;
                return this;
            }

            public IContentTypeBuilder WithUrl(Uri url)
            {
                _url = url;
                return this;
            }

            public IRequestJsonBodyBuilder WithJsonBody()
            {
                _dataType = DataType.Json;
                return this;
            }

            public IRequestFormBodyBuilder WithFormBody()
            {
                _dataType = DataType.Form;
                return this;
            }

            public IRequestMultipartBodyBuilder WithMultipartBody()
            {
                _dataType = DataType.Multipart;
                return this;
            }

            public IRequestBuilder WithHeader(string key, string value)
            {
                AddHeader(key, value);
                return this;
            }

            private void AddHeader(string key, string value)
            {
                if (_headers.AsValueEnumerable()
                    .Any(header => header.Key == key))
                    return;

                _bodyData.Add(new FormData(key, value));
            }

            public IRequestBuilder WithParam(string key, string value)
            {
                AddParameter(key, value);
                return this;
            }

            private void AddParameter(string key, string value)
            {
                if (_parameters.AsValueEnumerable()
                    .Any(param => param.Key == key))
                    return;

                _bodyData.Add(new FormData(key, value));
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

            private IRequestBody CreateBody()
            {
                switch (_dataType)
                {
                    case DataType.Json:
                        var jsonBody = new JsonBody();
                        jsonBody.RequestMethod = _requestMethod;
                        jsonBody.Url = _url;
                        jsonBody.Timeout = _timeout;
                        jsonBody.Header.AddRange(_headers);
                        jsonBody.Parameters.AddRange(_parameters);
                        jsonBody.Body = _jsonBody;
                        return jsonBody;
                    case DataType.Form:
                        var formBody = new FormBody();
                        formBody.RequestMethod = _requestMethod;
                        formBody.Url = _url;
                        formBody.Timeout = _timeout;
                        formBody.Header.AddRange(_headers);
                        formBody.Parameters.AddRange(_parameters);
                        formBody.Body.AddRange(_bodyData);
                        return formBody;
                    case DataType.Multipart:
                        var multipartBody = new MultipartBody();
                        multipartBody.RequestMethod = _requestMethod;
                        multipartBody.Url = _url;
                        multipartBody.Timeout = _timeout;
                        multipartBody.Header.AddRange(_headers);
                        multipartBody.Parameters.AddRange(_parameters);
                        multipartBody.Body.AddRange(_bodyData);
                        multipartBody.StreamData.AddRange(_streamData);
                        return null;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}