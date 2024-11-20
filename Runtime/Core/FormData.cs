namespace AceLand.WebRequest.Core
{
    internal sealed class FormData
    {
        internal FormData(string key, string value)
        {
            Key = key;
            Value = value;
        }
        
        public string Key { get; }
        public string Value { get; }
    }
}