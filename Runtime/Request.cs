using System;
using System.Collections.Generic;
using AceLand.Library.BuildLeveling;
using AceLand.Library.Extensions;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.ProjectSetting;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ZLinq;

namespace AceLand.WebRequest
{
    public static partial class Request
    {
        internal static AceLandWebRequestSettings Settings
        {
            get
            {
                _settings ??= Resources.Load<AceLandWebRequestSettings>(nameof(AceLandWebRequestSettings));
                return _settings;
            }
        }
        
        private static AceLandWebRequestSettings _settings;

        public static IUrlBuilder Get() =>
            new RequestHandleBuilder(RequestMethod.Get);

        public static IUrlBodyBuilder Post() =>
            new RequestBodyHandleBuilder(RequestMethod.Post);

        public static IUrlBodyBuilder Put() =>
            new RequestBodyHandleBuilder(RequestMethod.Put);

        public static IUrlBodyBuilder Patch() =>
            new RequestBodyHandleBuilder(RequestMethod.Patch);
        
        public static IUrlBodyBuilder Delete() =>
            new RequestBodyHandleBuilder(RequestMethod.Delete);
        
        internal static void PrintRequestLog(IRequestBody body)
        {
            if (!Settings.LoggingLevel.IsAcceptedLevel()) return;

            var msg = body.RequestMethod is RequestMethod.Get or RequestMethod.Delete
                ? $"Send Request: {body.RequestMethod} || {ShortenUrl(body.Uri)}\n" +
                  $">>> Timeout: {body.Timeout} ms"
                : $"Send Web Request: {body.RequestMethod} || {body.Uri}\n" +
                  $">>> Timeout: {body.Timeout} ms\n" +
                  $">>> Content Format: {body.DataType}";

            Debug.Log(msg);
        }
        
        internal static void PrintSuccessLog(IRequestBody body, JToken response)
        {
            if (!Settings.LoggingLevel.IsAcceptedLevel()) return;
            
            var msg = $"Request Success: {body.RequestMethod} || {ShortenUrl(body.Uri)}";
            if (Settings.ResultLoggingLevel.IsAcceptedLevel())
                msg += $"\n{response}";
            Debug.Log(msg);
        }
        
        internal static void PrintFailLog(IRequestBody body, JToken response)
        {
            if (!Settings.LoggingLevel.IsAcceptedLevel()) return;
            
            var msg = $"Request Fail: {body.RequestMethod} || {ShortenUrl(body.Uri)}";
            if (Settings.ResultLoggingLevel.IsAcceptedLevel())
                msg += $"\n{response}";
            Debug.Log(msg);
        }

        private static List<FormData> GetHeader(string sectionName = null)
        {
            var headers = new List<FormData>();
            
            if (Settings.AddTimeInHeader)
            {
                var time = $"{(DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000}";
                headers.Add(new FormData(Settings.TimeKey, time));
            }
            
            var headerList = sectionName.IsNullOrEmptyOrWhiteSpace()
                ? Settings.DefaultHeaders()
                : Settings.SectionHeaders(sectionName);
            
            foreach (var header in headerList.AsValueEnumerable().Where(h => !h.IsEmpty))
                headers.Add(new FormData(header.Key, header.Value));

            return headers;
        }

        private static bool CheckUrl(string url)
        {
            if (!Settings.ForceHttpsScheme) return true;
            var uri = url.ToUri();
            return uri.Scheme == Uri.UriSchemeHttps;

        }

        private static string ShortenUrl(Uri uri)
        {
            var urls = uri.ToString().Split('?');
            return urls.Length switch
            {
                > 1 => urls[0] + "?...",
                1 => urls[0],
                _ => string.Empty
            };
        }
    }
}