using System;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
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
            Promise.LinkedOrApplicationAliveToken(TokenSource, out var linkedTokenSource);
            LinkedTokenSource = linkedTokenSource;
        }

        ~RequestHandle() => Dispose(false);

        protected override void DisposeManagedResources()
        {
            Response?.Dispose();
            LinkedTokenSource?.Dispose();
            LinkedRequestTokenSource?.Dispose();
            TokenSource?.Dispose();
            RequestTokenSource?.Dispose();
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
        private CancellationTokenSource RequestTokenSource { get; set; }
        private CancellationTokenSource LinkedRequestTokenSource { get; set; }
        private CancellationToken LinkedToken => LinkedTokenSource.Token;
        private CancellationToken LinkedRequestToken => LinkedRequestTokenSource.Token;

        private Exception retryException;

        public void Cancel() => TokenSource?.Cancel();

        public async Task<T> Send<T>()
        {
            var result = await Send().ConfigureAwait(false);
            try
            {
                return result.ToObject<T>();
            }
            catch (JsonException ex)
            {
                Request.PrintFailLog(Body, ex);
                throw;
            }
        }

        public Task<JToken> Send()
        {
            RenewLinkedTokenSource();
            Request.PrintRequestLog(Body);

            return Task.Run(SendInternal, LinkedToken);
        }

        private async Task<JToken> SendInternal()
        {
            var maxAttempts = Math.Max(1, Settings.RequestRetry);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Response?.Dispose();
                Response = null;

                try
                {
                    Response = await Client.SendAsync(RequestMessage, LinkedRequestToken)
                        .ConfigureAwait(false);

                    var response = await Response.Content.ReadAsStringAsync()
                        .ConfigureAwait(false);

                    // properly escape non-JSON payloads
                    var jsonResponse = response.IsValidJson()
                        ? response
                        : new JObject { ["message"] = response ?? string.Empty }.ToString();

                    // 2xx
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
                    var statusInt = (int)httpStatusCode;

                    // treat ALL 5xx and 429 as retryable
                    if (statusInt >= 500 || httpStatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var seEx = new ServerErrorException(httpStatusCode, response);
                        Request.PrintFailLog(Body, seEx);
                        Promise.Dispatcher.Run(EventBus.Event<IServerErrorEvent>()
                            .WithData(seEx).RaiseWithoutCache);

                        if (attempt >= maxAttempts)
                            throw seEx;

                        retryException = seEx;
                        await HandleRetry(attempt).ConfigureAwait(false);
                        continue;
                    }

                    // Non-retryable HTTP errors -> throw and let them propagate.
                    switch (httpStatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                        {
                            var ex = new BadRequestException(response);
                            Request.PrintFailLog(Body, ex);
                            throw ex;
                        }
                        case HttpStatusCode.Unauthorized:
                        {
                            var ex = new UnauthorizedException(response);
                            Request.PrintFailLog(Body, ex);
                            Promise.Dispatcher.Run(EventBus.Event<IUnauthorizedEvent>()
                                .WithData(ex).RaiseWithoutCache);
                            throw ex;
                        }
                        case HttpStatusCode.Forbidden:
                        {
                            var ex = new ForbiddenException(response);
                            Request.PrintFailLog(Body, ex);
                            throw ex;
                        }
                        case HttpStatusCode.NotFound:
                        {
                            var ex = new NotFoundException(response);
                            Request.PrintFailLog(Body, ex);
                            throw ex;
                        }
                        case HttpStatusCode.Conflict:
                        {
                            var ex = new ConflictException(response);
                            Request.PrintFailLog(Body, ex);
                            throw ex;
                        }
                        default:
                        {
                            var ex = new HttpErrorException(httpStatusCode, response);
                            Request.PrintFailLog(Body, ex);
                            throw ex;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (ex.InnerException is WebException we)
                    {
                        retryException = we;
                        if (attempt >= maxAttempts) break;
                        await HandleRetry(attempt).ConfigureAwait(false);
                    }
                    else
                    {
                        Request.PrintFailLog(Body, ex);
                        if (ex.InnerException != null)
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        throw;
                    }
                }
                catch (WebException ex)
                {
                    retryException = ex;
                    if (attempt >= maxAttempts) break;
                    await HandleRetry(attempt).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                    // covers both TaskCanceledException and OperationCanceledException
                {
                    // User-initiated cancel (this also covers cancel during Task.Delay)
                    if (LinkedToken.IsCancellationRequested)
                    {
                        var ocEx = new OperationCanceledException(
                            "The request was canceled by the user.",
                            ex,
                            LinkedToken);
                        Request.PrintFailLog(Body, ocEx);
                        throw ocEx;
                    }

                    // Otherwise it's a per-request timeout -> retryable
                    retryException = new WebException("Canceled by Connection Error", ex);
                    if (attempt >= maxAttempts) break;
                    await HandleRetry(attempt).ConfigureAwait(false);
                }
                catch (JsonException ex)
                {
                    // Covers JsonReaderException and other Newtonsoft Json failures
                    Request.PrintFailLog(Body, ex);
                    throw;
                }
                // exception propagate naturally with its original stack trace.
            }

            Request.PrintFailLog(Body, retryException);
            
            switch (retryException)
            {
                case WebException e:
                    Promise.Dispatcher.Run(EventBus.Event<IConnectionErrorEvent>().WithData(e).RaiseWithoutCache);
                    break;
                case ServerErrorException e:
                    Promise.Dispatcher.Run(EventBus.Event<IServerErrorEvent>().WithData(e).RaiseWithoutCache);
                    break;
            }
            
            throw retryException;
        }

        private async Task HandleRetry(int attempt)
        {
            if (attempt >= Math.Max(1, Settings.RequestRetry))
                return;

            var retryInterval = Settings.GetRetryInterval(attempt);
            Request.PrintRetryLog(Body, attempt, retryInterval, retryException);

            RequestMessage?.Dispose();

            await Task.Delay(retryInterval, LinkedToken).ConfigureAwait(false);

            RequestMessage = RequestUtils.CreateRequestMessage(Body);
            RenewLinkedTokenSource();
        }

        private void RenewLinkedTokenSource()
        {
            RequestTokenSource?.Dispose();
            LinkedRequestTokenSource?.Dispose();

            RequestTokenSource = new CancellationTokenSource();
            RequestTokenSource.CancelAfter(TimeSpan.FromMilliseconds(Body.Timeout));
            Promise.LinkedOrApplicationAliveToken(RequestTokenSource, out var linkedRequestTokenSource);
            LinkedRequestTokenSource = linkedRequestTokenSource;
        }
    }
}