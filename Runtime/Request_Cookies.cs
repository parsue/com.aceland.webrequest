using System.Net;
using Unity.Scripting.LifecycleManagement;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
#if UNITY_6000_5_OR_NEWER
        [AutoStaticsCleanup]
#endif
        public static readonly CookieContainer CookieContainer = new();
        
#if UNITY_6000_5_OR_NEWER
        [AutoStaticsCleanup]
#endif
        internal static string RawCookie { get; private set; } = "";
        
        public static string GetCookie() => RawCookie;
        
        internal static void SetRawCookie(string cookie) => RawCookie = cookie;
    }
}