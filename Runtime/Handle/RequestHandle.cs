using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AceLand.Disposable;
using AceLand.ProjectSetting;
using AceLand.Serialization.Json;
using AceLand.TaskUtils;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.Exceptions;
using AceLand.WebRequest.ProjectSetting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AceLand.WebRequest.Handle
{
    internal class RequestHandle : DisposableObject, IRequestHandle
    {
        public RequestHandle(IRequestBody body)
        {
            var (client, content) = body.GetType() switch
            {
                { } t when t == typeof(JsonBody) => RequestUtils.CreateWebRequest((JsonBody)body),
                { } t when t == typeof(FormBody) => RequestUtils.CreateWebRequest((FormBody)body),
                { } t when t == typeof(MultipartBody) => RequestUtils.CreateWebRequest((MultipartBody)body),
                _ => throw new Exception("Unknown type of data")
            };
            
            Client = client;
            Content = content;
            Body = body;
            TokenSource = new CancellationTokenSource();
            RenewLinkedTokenSource();
        }

        ~RequestHandle() => Dispose(false);

        protected override void DisposeManagedResources()
        {
            LinkedTokenSource?.Dispose();
            TokenSource?.Dispose();
            Client?.Dispose();
            Content?.Dispose();
            Body.Dispose();
        }

        private static AceLandWebRequestSettings Settings => Request.Settings;
        
        public HttpResponseMessage Response { get; private set; }
        public JToken Result { get; private set; }
        private HttpClient Client { get; }
        private HttpContent Content { get; }
        private IRequestBody Body { get; }
        private CancellationTokenSource TokenSource { get; }
        private CancellationTokenSource LinkedTokenSource { get; set; }
        private CancellationToken LinkedToken => LinkedTokenSource.Token;

        private WebException retryException;

        public void Cancel()
        {
            LinkedTokenSource?.Cancel();
            TokenSource?.Cancel();
        }

        public Task<T> Send<T>()
        {
            return Task.Run(async () =>
                {
                    var result = await Send();
                    var data = result.ToObject<T>();
                    return data;
                },
                LinkedToken
            );
        }

        public Task<JToken> Send()
        {
            Request.PrintRequestLog(Body);

            return Task.Run(async () =>
                {
                    for (var attempt = 1; attempt <= Settings.RequestRetry; attempt++)
                    {
                        HttpStatusCode httpStatusCode;
                        
                        try
                        {
                            Response = Body.RequestMethod switch
                            {
                                RequestMethod.Post => await Client.PostAsync(Body.Url, Content, LinkedToken),
                                RequestMethod.Get => await Client.GetAsync(Body.Url, LinkedToken),
                                RequestMethod.Put => await Client.PutAsync(Body.Url, Content, LinkedToken),
                                RequestMethod.Delete => await Client.DeleteAsync(Body.Url, LinkedToken),
                                RequestMethod.Patch => await Client.PatchAsync(Body.Url, Content, LinkedToken),
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            var response = await Response.Content.ReadAsStringAsync();
                            var jsonResponse = response.IsValidJson()
                                ? response
                                : $"{{\"message\":\"{response}\"}}";
                            
                            if (Response.IsSuccessStatusCode)
                            {
                                Result = JToken.Parse(jsonResponse);
                                Request.PrintSuccessLog(Body, Result);

                                return Result;
                            }

                            httpStatusCode = Response.StatusCode;

                            if (httpStatusCode is >= HttpStatusCode.InternalServerError or
                                HttpStatusCode.TooManyRequests)
                            {
                                // Retry for server errors (5xx) and rate limiting (429)
                                throw new HttpServerErrorException(httpStatusCode, response);
                            }

                            // Throw an exception for other HTTP errors (4xx)
                            throw new HttpErrorException(httpStatusCode, response);
                        }
                        catch (WebException ex)
                        {
                            retryException = ex;
                            await HandleRetry(attempt);
                        }
                        catch (HttpRequestException ex)
                        {
                            if (ex.InnerException is WebException we)
                            {
                                retryException = we;
                                await HandleRetry(attempt);
                            }
                            else
                            {
                                if (Settings.LoggingLevel.IsAcceptedLevel())
                                    Debug.LogWarning($"Request failed: {ex.Message}\n" +
                                                     $"Exception: {ex}");
                                throw;
                            }
                        }
                        catch (TaskCanceledException ex)
                        {
                            // Check if the cancellation was user-initiated
                            if (LinkedToken.IsCancellationRequested)
                            {
                                if (Settings.LoggingLevel.IsAcceptedLevel())
                                    Debug.LogWarning("Request failed: canceled by user\n" +
                                                     $"Exception: {ex}");
                                throw new OperationCanceledException(
                                    "The request was canceled by the user.",
                                    ex,
                                    LinkedToken
                                );
                            }
                            
                            retryException = new WebException("Canceled by Connection Error", ex);
                            await HandleRetry(attempt);
                        }
                        catch (JsonReaderException ex)
                        {
                            if (Settings.LoggingLevel.IsAcceptedLevel())
                                Debug.LogError($"Json Parse Fail: {ex.Message}\n" +
                                               $"Exception: {ex}");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            if (ex.InnerException != null)
                                throw ex.InnerException;
                            
                            // For non-retryable errors, rethrow immediately
                            if (Settings.LoggingLevel.IsAcceptedLevel())
                                Debug.LogError($"Request failed: {ex.Message}\n" +
                                               $"Exception: {ex}");
                            throw new Exception($"Request failed: {ex.Message}", ex);
                        }
                    }

                    // Handle connection-related errors
                    if (Settings.LoggingLevel.IsAcceptedLevel())
                        Debug.LogError($"Max retries reached. Request failed due to: {retryException.Message}\n" +
                                       $"Exception: {retryException}");
                    throw retryException;
                },
                LinkedToken
            );
        }

        private async Task HandleRetry(int attempt)
        {
            var retryInterval = Settings.GetRetryInterval(attempt);
            
            if (Settings.LoggingLevel.IsAcceptedLevel())
                Debug.LogWarning($"Connection error on attempt {attempt}: " +
                                 $"{retryException.Message}. Retry after {retryInterval} ms...\n" +
                                 $"Exception:\n{retryException}");
            
            await Task.Delay(retryInterval, LinkedToken);
        }

        private void RenewLinkedTokenSource()
        {
            LinkedTokenSource?.Dispose();
            Promise.LinkedOrApplicationAliveToken(TokenSource, out var source);
            LinkedTokenSource = source;
        }
    }
}