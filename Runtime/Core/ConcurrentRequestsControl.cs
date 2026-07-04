using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AceLand.TaskUtils;
using UnityEngine;

namespace AceLand.WebRequest.Core
{
    internal interface IConcurrentRequestsControl
    {
        Task AwaitForConcurrentReady(IRequestBody body);
        void Completed(IRequestBody body);
    }
    
    internal class ConcurrentRequestsControl : IConcurrentRequestsControl
    {
        public static IConcurrentRequestsControl Instance { get; } = new ConcurrentRequestsControl();
        private ConcurrentRequestsControl() { }
        
        private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores = new();

        public async Task AwaitForConcurrentReady(IRequestBody body)
        {
            if (!TryGetDomainFromUrl(body.Url, out var domain))
                throw new Exception($"The url {body.Url} is not valid.");

            if (body.MaxConcurrentRequests <= 0)
                return;

            var semaphore = semaphores.GetOrAdd(domain, _ => 
                new SemaphoreSlim(
                    body.MaxConcurrentRequests,
                    body.MaxConcurrentRequests)
            );
            
            await semaphore.WaitAsync(Promise.ApplicationAliveToken).ConfigureAwait(false);
            
            Debug.Log($"Concurrent Acquired - {domain} (Available slots: {semaphore.CurrentCount})");
        }

        public void Completed(IRequestBody body)
        {
            if (!TryGetDomainFromUrl(body.Url, out var domain))
                return;

            if (body.MaxConcurrentRequests <= 0)
                return;

            if (!semaphores.TryGetValue(domain, out var semaphore)) return;
            
            semaphore.Release();
            
            Debug.Log($"Concurrent Released - {domain} (Available slots: {semaphore.CurrentCount})");
        }

        private static bool TryGetDomainFromUrl(string url, out string domain)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                domain = null;
                return false;
            }
            
            domain = uri.Host;
            
            return true;
        }
    }
}