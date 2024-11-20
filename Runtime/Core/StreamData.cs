using System.IO;

namespace AceLand.WebRequest.Core
{
    internal sealed class StreamData
    {
        public StreamData(string key, Stream content, string fileName)
        {
            Key = key;
            Content = content;
            FileName = fileName;
        }

        public string Key { get; }
        public Stream Content { get; }
        public string FileName { get; }
    }
}