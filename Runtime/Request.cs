using System;
using System.Collections.Generic;
using AceLand.Library.BuildLeveling;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.ProjectSetting;
using Newtonsoft.Json.Linq;
using UnityEngine;

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
        
        public static IUrlBuilder Delete() =>
            new RequestHandleBuilder(RequestMethod.Delete);

        public static IUrlBodyBuilder Post() =>
            new RequestBodyHandleBuilder(RequestMethod.Post);

        public static IUrlBodyBuilder Put() =>
            new RequestBodyHandleBuilder(RequestMethod.Put);

        public static IUrlBodyBuilder Patch() =>
            new RequestBodyHandleBuilder(RequestMethod.Patch);
        
        internal static void PrintRequestLog(IRequestBody body)
        {
            if (!Settings.LoggingLevel.IsAcceptedLevel()) return;
            
            var msg = body.RequestMethod is RequestMethod.Get or RequestMethod.Delete
                ? $"Send Request: {body.RequestMethod} || {body.Url}\n" +
                  $">>> Timeout: {body.Timeout} ms\n" +
                  $">>> Header:\n" +
                  $"{body.HeaderText()}\n" +
                  $">>> Body:\n" +
                  $"{body.BodyText()}"
                : $"Send Web Request: {body.RequestMethod} || {body.Url}\n" +
                  $">>> Timeout: {body.Timeout} ms\n" +
                  $">>> Content Format: {body.DataType}\n" +
                  $">>> Header:\n" +
                  $"{body.HeaderText()}\n" +
                  $">>> Body:\n" +
                  $"{body.BodyText()}";

            Debug.Log(msg);
        }
        
        internal static void PrintSuccessLog(IRequestBody body, JToken response)
        {
            if (!Settings.LoggingLevel.IsAcceptedLevel()) return;
            
            var msg = $"Request Success: {body.RequestMethod} || {body.Url}";
            if (Settings.ResultLoggingLevel.IsAcceptedLevel())
                msg += $"\n{response}";
            Debug.Log(msg);
        }
        
        internal static void PrintFailLog(IRequestBody body, JToken response)
        {
            if (!Settings.LoggingLevel.IsAcceptedLevel()) return;
            
            var msg = $"Request Fail: {body.RequestMethod} || {body.Url}";
            if (Settings.ResultLoggingLevel.IsAcceptedLevel())
                msg += $"\n{response}";
            Debug.Log(msg);
        }

        private static List<FormData> AutoFillHeader()
        {
            var header = new List<FormData>();
            
            if (Settings.AddTimeInHeader)
            {
                var time = $"{(DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000}";
                header.Add(new FormData(Settings.TimeKey, time));
            }

            return header;
        }

        private static bool CheckUrl(Uri url)
        {
            if (!Settings.ForceHttpsScheme) return true;
            return url.Scheme == Uri.UriSchemeHttps;

        }
    }
}