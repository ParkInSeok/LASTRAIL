using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LastRail.Diagnostics
{
    // Owned client and queue; all producers and flush calls belong to the main thread.
    public sealed class CurrentSessionErrorRuntime : IDisposable
    {
        private readonly CurrentSessionErrorClient client;
        private readonly List<CurrentSessionErrorClient.Request> pending = new List<CurrentSessionErrorClient.Request>();
        private Task inFlight;
        private bool disposed;
        public int PendingCount => pending.Count;
        public long RejectedDeliveryCount { get; private set; }

        public CurrentSessionErrorRuntime(CurrentSessionErrorClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public bool Record(SafeClientErrorKind kind, long originatingGeneration)
        {
            if (disposed) return false;
            CurrentSessionErrorClient.Request request;
            try { request = client.Capture(kind, originatingGeneration); }
            catch { return false; }
            if (request == null) return false;
            // Capture bounds its pending set at 25 and prunes the previous owner first.
            pending.RemoveAll(item => item.Generation != request.Generation
                || !string.Equals(item.AccountId, request.AccountId, StringComparison.Ordinal));
            pending.Add(request);
            return true;
        }

        public Task FlushAsync()
        {
            if (inFlight != null && !inFlight.IsCompleted) return inFlight;
            if (disposed || pending.Count == 0) return Task.CompletedTask;
            // Publish the joinable task before any synchronously completing transport call.
            var completion = new TaskCompletionSource<bool>();
            inFlight = completion.Task;
            _ = DrainAsync(pending.ToArray(), completion);
            return completion.Task;
        }

        public Task ReconcilePendingAsync() => disposed ? Task.CompletedTask : client.ReconcilePendingAsync();

        private async Task DrainAsync(CurrentSessionErrorClient.Request[] snapshot, TaskCompletionSource<bool> completion)
        {
            try
            {
                foreach (var request in snapshot)
                {
                    if (disposed) break;
                    if (!pending.Contains(request)) continue;
                    ClientErrorDelivery delivery;
                    try { delivery = await client.SendAsync(request); }
                    catch { delivery = ClientErrorDelivery.Unknown; }
                    if (disposed) break;
                    if(delivery==ClientErrorDelivery.Rejected && RejectedDeliveryCount<long.MaxValue)RejectedDeliveryCount++;
                    if (delivery == ClientErrorDelivery.Received || delivery == ClientErrorDelivery.Stale || delivery == ClientErrorDelivery.Rejected)
                        pending.Remove(request);
                }
            }
            finally { completion.TrySetResult(true); }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            pending.Clear();
            client.Dispose();
        }
    }
}
