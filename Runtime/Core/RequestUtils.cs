using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AceLand.Library.Extensions;
using UnityEngine;

namespace AceLand.WebRequest.Core
{
    internal static class RequestUtils
    {
        public static (HttpClient client, HttpContent content) CreateWebRequest(JsonBody body, string fingerprint)
        {
            var client = CreateHttpClient(fingerprint);
            var content = new StringContent(body.Body, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            foreach (var data in body.Headers)
                client.DefaultRequestHeaders.Add(data.Key, data.Value);
            
            client.Timeout = TimeSpan.FromMilliseconds(body.Timeout);

            return (client, content);
        }
        
        public static (HttpClient client, HttpContent content) CreateWebRequest(FormBody body, string fingerprint)
        {
            var client = CreateHttpClient(fingerprint);
            var dicBody = new Dictionary<string, string>();
            foreach (var data in body.Body)
                dicBody[data.Key] = data.Value;
            
            var content = new FormUrlEncodedContent(dicBody);
            
            client.DefaultRequestHeaders.Clear();
            foreach (var data in body.Headers)
                client.DefaultRequestHeaders.Add(data.Key, data.Value);
            
            client.Timeout = TimeSpan.FromMilliseconds(body.Timeout);

            return (client, content);
        }
        
        public static (HttpClient client, HttpContent content) CreateWebRequest(MultipartBody body, string fingerprint)
        {
            var client = CreateHttpClient(fingerprint);
            var content = new MultipartFormDataContent();
            
            foreach (var data in body.Body)
                content.Add(new StringContent(data.Value), data.Key);

            foreach (var data in body.StreamData)
                content.Add(new StreamContent(data.Content), data.Key, data.FileName);
            
            client.DefaultRequestHeaders.Clear();
            foreach (var data in body.Headers)
                client.DefaultRequestHeaders.Add(data.Key, data.Value);
            
            client.Timeout = TimeSpan.FromMilliseconds(body.Timeout);

            return (client, content);
        }
        
        private static HttpClient CreateHttpClient(string fingerprint)
        {
            if (fingerprint.IsNullOrEmptyOrWhiteSpace()) return new HttpClient();
            
            var handler = new HttpClientHandler();
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

            return new HttpClient(handler);
        }
    }
}
