using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AceLand.Disposable;
using AceLand.EventDriven.Bus;
using AceLand.ProjectSetting;
using AceLand.Serialization.Json;
using AceLand.TaskUtils;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.Events;
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
            Client = RequestUtils.GetOrCreateHttpClient();
            RequestMessage = RequestUtils.CreateRequestMessage(body);
            Body = body;
            TokenSource = new CancellationTokenSource();
        }

        ~RequestHandle() => Dispose(false);

        protected override void DisposeManagedResources()
        {
            LinkedTokenSource?.Dispose();
            TokenSource?.Dispose();
            RequestMessage?.Dispose();
            Body.Dispose();
        }

        private static AceLandWebRequestSettings Settings => Request.Settings;
        
        public HttpResponseMessage Response { get; private set; }
        public JToken Result { get; private set; }
        private HttpClient Client { get; }
        private HttpRequestMessage RequestMessage { get; set; }
        private IRequestBody Body { get; }
        private CancellationTokenSource TokenSource { get; set; }
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
            RenewLinkedTokenSource();

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
            if (LinkedTokenSource == null || LinkedTokenSource.IsCancellationRequested)
                RenewLinkedTokenSource();

            Request.PrintRequestLog(Body);

            return Task.Run(async () =>
                {
                    for (var attempt = 1; attempt <= Settings.RequestRetry; attempt++)
                    {
                        try
                        {
                            Response = await Client.SendAsync(RequestMessage, LinkedToken);

                            var response = await Response.Content.ReadAsStringAsync();
                            var jsonResponse = response.IsValidJson()
                                ? response
                                : $"{{\"message\":\"{response ?? string.Empty}\"}}";
                            
                            if (Response.IsSuccessStatusCode)
                            {
                                if (Response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
                                {
                                    var cookie = string.Join("; ", cookieValues);
                                    Request.SetRawCookie(cookie);
                                }
                                
                                Result = JToken.Parse(jsonResponse);
                                Request.PrintSuccessLog(Body, Result);

                                return Result;
                            }

                            var httpStatusCode = Response.StatusCode;

                            switch (httpStatusCode)
                            {
                                // server errors (5xx) and rate limiting (429)
                                case HttpStatusCode.InternalServerError: 
                                case HttpStatusCode.TooManyRequests:
                                    var seEx = new ServerErrorException(httpStatusCode, response);
                                    Promise.Dispatcher.Run(EventBus.Event<IServerErrorEvent>().WithData(seEx).RaiseWithoutCache);
                                    throw seEx;
                                
                                case HttpStatusCode.BadRequest:
                                    throw new BadRequestException(response);
                                
                                case HttpStatusCode.Unauthorized:
                                    var uEx = new UnauthorizedException(response);
                                    Promise.Dispatcher.Run(EventBus.Event<IUnauthorizedEvent>().WithData(uEx).RaiseWithoutCache);
                                    throw uEx;
                                
                                case HttpStatusCode.NotFound:
                                    throw new NotFoundException(response);
                                
                                // Throw an exception for other HTTP errors
                                default:
                                    throw new HttpErrorException(httpStatusCode, response);
                            }
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
                            
                                throw ex.InnerException ?? ex;
                            }
                        }
                        catch (WebException ex)
                        {
                            retryException = ex;
                            await HandleRetry(attempt);
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
                            var e = ex.InnerException ?? ex.GetBaseException();
                            if (e != null) throw e;
                            
                            // For non-retryable errors, rethrow immediately
                            if (Settings.LoggingLevel.IsAcceptedLevel())
                                Debug.LogError($"Request failed: {ex.Message}\n" +
                                               $"Exception: {ex}");
                            throw new Exception($"Request failed: {ex.Message}", ex);
                        }
                    }

                    // Handle connection-related errors
                    var msg = retryException.Message;
                    if (Settings.LoggingLevel.IsAcceptedLevel())
                        Debug.LogError($"Max retries reached. Request failed due to: {msg}\n" +
                                       $"Exception: {retryException}");
                    
                    Promise.Dispatcher.Run(EventBus.Event<IConnectionErrorEvent>().WithData(retryException).RaiseWithoutCache);
                    throw retryException;
                },
                Promise.ApplicationAliveToken
            );
        }

        private async Task HandleRetry(int attempt)
        {
            var retryInterval = Settings.GetRetryInterval(attempt);
            
            if (Settings.LoggingLevel.IsAcceptedLevel())
                Debug.LogWarning($"Connection error on attempt {attempt}: " +
                                 $"{retryException.Message}. Retry after {retryInterval} ms...\n" +
                                 $"Exception:\n{retryException}");
            
            RequestMessage?.Dispose();
            
            await Task.Delay(retryInterval, LinkedToken);
            
            RequestMessage = RequestUtils.CreateRequestMessage(Body);
        }

        private void RenewLinkedTokenSource()
        {
            TokenSource.CancelAfter(TimeSpan.FromMilliseconds(Body.Timeout));
            
            LinkedTokenSource?.Dispose();
            Promise.LinkedOrApplicationAliveToken(TokenSource, out var source);
            LinkedTokenSource = source;
        }
    }
}
