using System;
using System.Collections.Generic;
using System.IO;
using AceLand.Library.Extensions;
using AceLand.Library.Json;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.Handle;

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
            IRequestJsonBodyBuilder WithJsonContent();
            IRequestFormBodyBuilder WithFormContent();
            IRequestMultipartBodyBuilder WithMultipartContent();
        }
        
        public interface IRequestJsonBodyBuilder : IRequestBuilder
        {
            IRequestJsonBodyBuilder WithHeader(string key, string value);
            IRequestJsonBodyBuilder WithContent(string json);
        }

        public interface IRequestFormBodyBuilder : IRequestBuilder
        {
            IRequestFormBodyBuilder WithHeader(string key, string value);
            IRequestFormBodyBuilder WithContent(string key, string value);
        }

        public interface IRequestMultipartBodyBuilder : IRequestBuilder
        {
            IRequestMultipartBodyBuilder WithHeader(string key, string value);
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
                _headers.AddRange(AutoFillHeader());
            }

            private readonly RequestMethod _requestMethod;
            private Uri _url;
            private float _timeout = -1;
            private (string key, string token) _token;
            private DataType _dataType;
            private readonly List<FormData> _headers = new();
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

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithHeader(string key, string value)
            {
                _headers.Add(new FormData(key, value));
                return this;
            }

            IRequestFormBodyBuilder IRequestFormBodyBuilder.WithHeader(string key, string value)
            {
                _bodyData.Add(new FormData(key, value));
                return this;
            }

            IRequestMultipartBodyBuilder IRequestMultipartBodyBuilder.WithHeader(string key, string value)
            {
                _bodyData.Add(new FormData(key, value));
                return this;
            }

            IRequestJsonBodyBuilder IRequestJsonBodyBuilder.WithContent(string json)
            {
                if (Settings.CheckJsonBeforeSend && !json.IsNullOrEmptyOrWhiteSpace() && !json.IsValidJson())
                    throw new Exception("json format is not correct");
                
                _jsonBody = json;
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
                        jsonBody.Body = _jsonBody;
                        return jsonBody;
                    case DataType.Form:
                        var formBody = new FormBody();
                        formBody.RequestMethod = _requestMethod;
                        formBody.Url = _url;
                        formBody.Timeout = _timeout;
                        formBody.Header.AddRange(_headers);
                        formBody.Body.AddRange(_bodyData);
                        return formBody;
                    case DataType.Multipart:
                        var multipartBody = new MultipartBody();
                        multipartBody.RequestMethod = _requestMethod;
                        multipartBody.Url = _url;
                        multipartBody.Timeout = _timeout;
                        multipartBody.Header.AddRange(_headers);
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