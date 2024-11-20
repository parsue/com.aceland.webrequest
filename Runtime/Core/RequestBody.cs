using System;
using System.Collections.Generic;
using AceLand.Library.Disposable;

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
            Header.Clear();
        }
        
        public RequestMethod RequestMethod { get; internal set; }
        public DataType DataType => DataType.Json;
        public Uri Url { get; internal set; }
        public float Timeout { get; internal set; } = -1;
        
        public readonly List<FormData> Header = new();

        public string HeaderText()
        {
            var text = string.Empty;
            foreach (var data in Header)
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