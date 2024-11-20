using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace AceLand.WebRequest.Core
{
    internal static class RequestUtils
    {
        public static (HttpClient client, HttpContent content) CreateWebRequest(JsonBody body)
        {
            var client = new HttpClient();
            var content = new StringContent(body.Body, Encoding.UTF8, "application/json");
            
            client.DefaultRequestHeaders.Clear();
            foreach (var data in body.Header)
                client.DefaultRequestHeaders.Add(data.Key, data.Value);
            
            client.Timeout = TimeSpan.FromMilliseconds(body.Timeout);

            return (client, content);
        }
        
        public static (HttpClient client, HttpContent content) CreateWebRequest(FormBody body)
        {
            var client = new HttpClient();
            var dicBody = new Dictionary<string, string>();
            foreach (var data in body.Body)
                dicBody[data.Key] = data.Value;
            
            var content = new FormUrlEncodedContent(dicBody);
            
            client.DefaultRequestHeaders.Clear();
            foreach (var data in body.Header)
                client.DefaultRequestHeaders.Add(data.Key, data.Value);
            
            client.Timeout = TimeSpan.FromMilliseconds(body.Timeout);

            return (client, content);
        }
        
        public static (HttpClient client, HttpContent content) CreateWebRequest(MultipartBody body)
        {
            var client = new HttpClient();
            var content = new MultipartFormDataContent();
            
            foreach (var data in body.Body)
                content.Add(new StringContent(data.Value), data.Key);

            foreach (var data in body.StreamData)
                content.Add(new StreamContent(data.Content), data.Key, data.FileName);
            
            client.DefaultRequestHeaders.Clear();
            foreach (var data in body.Header)
                client.DefaultRequestHeaders.Add(data.Key, data.Value);
            
            client.Timeout = TimeSpan.FromMilliseconds(body.Timeout);

            return (client, content);
        }
    }
}