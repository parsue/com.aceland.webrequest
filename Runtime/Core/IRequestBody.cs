namespace AceLand.WebRequest.Core
{
    internal interface IRequestBody
    {
        RequestMethod RequestMethod { get; }
        DataType DataType { get; }
        string Url { get; }
        int MaxConcurrentRequests { get; }
        float Timeout { get; }
        string Fingerprint { get; }
        void Dispose();
        string HeaderText();
        string BodyText();
    }
}
