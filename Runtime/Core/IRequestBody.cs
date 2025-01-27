using System;

namespace AceLand.WebRequest.Core
{
    public interface IRequestBody
    {
        RequestMethod RequestMethod { get; }
        DataType DataType { get; }
        Uri Url { get; }
        float Timeout { get; }
        void Dispose();
        string HeaderText();
        string BodyText();
    }
}