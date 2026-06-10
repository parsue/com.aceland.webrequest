using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AceLand.Disposable;
using AceLand.EventDriven.Bus;
using AceLand.Serialization.Json;
using AceLand.TaskUtils;
using AceLand.WebRequest.Core;
using AceLand.WebRequest.Events;
using AceLand.WebRequest.Exceptions;
using AceLand.WebRequest.ProjectSetting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                                    Request.PrintFailLog(Body, seEx);
                                    Promise.Dispatcher.Run(EventBus.Event<IServerErrorEvent>().WithData(seEx).RaiseWithoutCache);
                                    throw seEx;
                                
                                case HttpStatusCode.BadRequest:
                                    var brEx = new BadRequestException(response);
                                    Request.PrintFailLog(Body, brEx);
                                    throw brEx;
                                
                                case HttpStatusCode.Unauthorized:
                                    var uEx = new UnauthorizedException(response);
                                    Request.PrintFailLog(Body, uEx);
                                    Promise.Dispatcher.Run(EventBus.Event<IUnauthorizedEvent>().WithData(uEx).RaiseWithoutCache);
                                    throw uEx;
                                
                                case HttpStatusCode.NotFound:
                                    var nfEx = new NotFoundException(response);
                                    Request.PrintFailLog(Body, nfEx);
                                    throw nfEx;
                                
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
                                Request.PrintFailLog(Body, ex);
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
                                var ocEx = new OperationCanceledException(
                                    "The request was canceled by the user.",
                                    ex,
                                    LinkedToken
                                );
                                Request.PrintFailLog(Body, ocEx);
                                throw ocEx;
                            }
                            
                            retryException = new WebException("Canceled by Connection Error", ex);
                            await HandleRetry(attempt);
                        }
                        catch (JsonReaderException ex)
                        {
                            Request.PrintFailLog(Body, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            var e = ex.InnerException ?? ex.GetBaseException();
                            if (e != null) throw e;
                            
                            // For non-retryable errors, rethrow immediately
                            Request.PrintFailLog(Body, ex);
                            throw new Exception($"Request failed: {ex.Message}", ex);
                        }
                    }

                    // Handle connection-related errors
                    Request.PrintFailLog(Body, retryException);
                    Promise.Dispatcher.Run(EventBus.Event<IConnectionErrorEvent>().WithData(retryException).RaiseWithoutCache);
                    throw retryException;
                },
                Promise.ApplicationAliveToken
            );
        }

        private async Task HandleRetry(int attempt)
        {
            var retryInterval = Settings.GetRetryInterval(attempt);
            
            Request.PrintRetryLog(Body, attempt, retryInterval, retryException);
            
            RequestMessage?.Dispose();
            TokenSource.CancelAfter(retryInterval + 500);
            
            await Task.Delay(retryInterval, LinkedToken);
            
            RequestMessage = RequestUtils.CreateRequestMessage(Body);
            
            RenewLinkedTokenSource();
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
