using System;
using System.Collections.Generic;
using AceLand.Disposable;
using ZLinq;

namespace AceLand.WebRequest.Core
{
    internal abstract class RequestBody : DisposableObject, IRequestBody
    {
        ~RequestBody()
        {
            Dispose(false);
        }

        protected override void DisposeManagedResources()
        {
            Headers.Clear();
        }
        
        public RequestMethod RequestMethod { get; internal set; }
        public DataType DataType => DataType.Json;

        public Uri Uri
        {
            get
            {
                if (Parameters.Count == 0) return url.ToUri();
                var uri = Parameters.AsValueEnumerable()
                    .Aggregate(
                        $"{url}?",
                        (current, param) => current + $"{param.Key}={param.Value}&")
                    .TrimEnd('&')
                    .ToUri();
                return uri;
            }
            
            internal set
            {
                url = value.ToString();
            }
        }

        public float Timeout { get; internal set; } = -1;

        public readonly List<FormData> Headers = new();
        public readonly List<FormData> Parameters = new();

        private string url;

        public string HeaderText()
        {
            var text = string.Empty;
            foreach (var data in Headers)
                text += $">>>>>> {data.Key} : {data.Value}\n";
            text = text.TrimEnd('\n');
            return text;
        }

        public virtual string BodyText()
        {
            return string.Empty;
        }
    }
}