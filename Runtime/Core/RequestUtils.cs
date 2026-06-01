using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using AceLand.Library.Extensions;
using UnityEngine;

namespace AceLand.WebRequest.Core
{
    internal static class RequestUtils
    {
        private static HttpClient _client;

        public static HttpClient GetOrCreateHttpClient()
        {
            if (_client != null) return _client;
            
            var handler = new HttpClientHandler();

            var fingerprint = Request.DefaultSection.RootCaFingerprint;
            
            if (!fingerprint.IsNullOrEmptyOrWhiteSpace())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == SslPolicyErrors.None) return true;

                    foreach (var element in chain.ChainElements)
                    {
                        if (element.Certificate.GetCertHashString() == fingerprint)
                            return true;
                    }

                    Debug.LogError($"Certificate rejected. SSL Error: {sslPolicyErrors}");
                    return false;
                };
            }

            handler.UseCookies = true; 
            handler.CookieContainer = Request.CookieContainer;
            
            var client = new HttpClient(handler);

            if (!string.IsNullOrEmpty(Request.RawCookie))
                client.DefaultRequestHeaders.Add("Cookie", Request.RawCookie);

            return client;
        }

        public static HttpRequestMessage CreateRequestMessage(IRequestBody body)
        {
            var requestMsg = new HttpRequestMessage(body.RequestMethod.ToHttpMethod(), body.Url);
            return body.GetType() switch
            {
                { } t when t == typeof(JsonBody) => CreateWebRequest(requestMsg, (JsonBody)body),
                { } t when t == typeof(FormBody) => CreateWebRequest(requestMsg, (FormBody)body),
                { } t when t == typeof(MultipartBody) => CreateWebRequest(requestMsg, (MultipartBody)body),
                _ => throw new Exception("Unknown type of data")
            };
        }
        
        private static HttpRequestMessage CreateWebRequest(HttpRequestMessage msg, JsonBody body)
        {
            msg.Content = new StringContent(body.Body, Encoding.UTF8, "application/json");

            foreach (var data in body.Headers)
            {
                if (msg.Headers.Contains(data.Key))
                    msg.Headers.Remove(data.Key);
                msg.Headers.Add(data.Key, data.Value);
            }

            return msg;
        }
        
        private static HttpRequestMessage CreateWebRequest(HttpRequestMessage msg, FormBody body)
        {
            var dicBody = new Dictionary<string, string>();
            foreach (var data in body.Body)
                dicBody[data.Key] = data.Value;
            
            msg.Content = new FormUrlEncodedContent(dicBody);
            
            foreach (var data in body.Headers)
            {
                if (msg.Headers.Contains(data.Key))
                    msg.Headers.Remove(data.Key);
                msg.Headers.Add(data.Key, data.Value);
            }
            
            return msg;
        }
        
        private static HttpRequestMessage CreateWebRequest(HttpRequestMessage msg, MultipartBody body)
        {
            var multipartContent = new MultipartFormDataContent();
            
            foreach (var data in body.Body)
                multipartContent.Add(new StringContent(data.Value), data.Key);

            foreach (var data in body.StreamData)
                multipartContent.Add(new StreamContent(data.Content), data.Key, data.FileName);
            
            msg.Content = multipartContent;
            
            foreach (var data in body.Headers)
            {
                if (msg.Headers.Contains(data.Key))
                    msg.Headers.Remove(data.Key);
                msg.Headers.Add(data.Key, data.Value);
            }
            
            return msg;
        }
    }
}
