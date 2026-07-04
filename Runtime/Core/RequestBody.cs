using System;
using System.Collections.Generic;
using AceLand.Disposable;
using ZLinq;

namespace AceLand.WebRequest.Core
{
    internal abstract class RequestBody : DisposableObject, IRequestBody
    {
        protected RequestBody(DataType dataType) => DataType = dataType;
        
        ~RequestBody()
        {
            Dispose(false);
        }

        protected override void DisposeManagedResources()
        {
            Headers.Clear();
        }
        
        public RequestMethod RequestMethod { get; internal set; }
        public DataType DataType { get; }

        public string Url
        {
            get
            {
                if (Parameters.Count == 0) return url;
                var u = Parameters.AsValueEnumerable()
                    .Aggregate(
                        $"{url}?",
                        (current, param) => current + $"{param.Key}={param.Value}&")
                    .TrimEnd('&');
                return u;
            }
            
            internal set
            {
                url = value;
            }
        }
        public int MaxConcurrentRequests { get; internal set; } = -1;
        public float Timeout { get; internal set; } = -1;
        public string Fingerprint { get; internal set; } = null;

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
