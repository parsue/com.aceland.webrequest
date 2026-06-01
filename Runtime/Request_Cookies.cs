using System.Net;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        public static readonly CookieContainer CookieContainer = new();

        public static string GetCookie() => RawCookie;
        
        internal static string RawCookie { get; private set; } = "";
        internal static void SetRawCookie(string cookie) => RawCookie = cookie;
    }
}