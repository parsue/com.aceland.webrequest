using System;

namespace AceLand.WebRequest.Core
{
    internal interface IRequestBody
    {
        RequestMethod RequestMethod { get; }
        DataType DataType { get; }
        string Url { get; }
        float Timeout { get; }
        void Dispose();
        string HeaderText();
        string BodyText();
    }
}