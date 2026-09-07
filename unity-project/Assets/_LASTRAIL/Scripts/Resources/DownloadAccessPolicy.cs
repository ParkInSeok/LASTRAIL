using System;
using System.Threading;
using System.Threading.Tasks;

namespace LastRail.Resources
{
    public enum ContentNetwork { Offline, UnknownOnline, Wifi, OtherOnline }

    public sealed class DownloadAccessPolicy
    {
        private TaskCompletionSource<bool> signal = new TaskCompletionSource<bool>();
        private string revision;
        private bool approved;
        private bool declined;
        private bool deferUntilLogin;
        public ContentNetwork Network { get; private set; } = ContentNetwork.UnknownOnline;
        public bool Authenticated { get; private set; }
        public bool Suspended { get; private set; }
        public long RemainingBytes { get; private set; }
        public bool Allowed => !Suspended && Network != ContentNetwork.Offline && (Network == ContentNetwork.Wifi || approved);
        public bool NeedsConsent => RemainingBytes > 0 && !Allowed && !Suspended && Network != ContentNetwork.Offline
            && !declined && (!deferUntilLogin || Authenticated);
        public bool AwaitingNonWifiConsent => deferUntilLogin && !approved && RemainingBytes > 0;
        public event Action Changed;

        public void SetNetwork(ContentNetwork network)
        {
            if (Network == network) return;
            if (Network == ContentNetwork.Wifi && network != ContentNetwork.Wifi && !approved)
                deferUntilLogin = true;
            Network = network;
            Notify();
        }

        public void SetPlan(string contentRevision, long remaining)
        {
            if (remaining < 0) throw new ArgumentOutOfRangeException(nameof(remaining));
            // Departure belongs to the login flow, including the time before its first content plan.
            if (revision != contentRevision) { revision = contentRevision; approved = false; declined = false; }
            RemainingBytes = remaining;
            Notify();
        }

        public void SetRemaining(long bytes) { RemainingBytes = Math.Max(0, bytes); Notify(); }
        public void SetAuthenticated(bool value) { Authenticated = value; Notify(); }
        public void SetSuspended(bool value) { Suspended = value; Notify(); }
        public void Decline() { declined = true; Notify(); }
        public void Retry() { declined = false; Notify(); }
        public void Approve()
        {
            if (!NeedsConsent) return;
            approved = true; declined = false; Notify();
        }

        public async Task WaitForAccessAsync(CancellationToken cancel)
        {
            while (!Allowed) await WaitForChangeAsync(cancel);
            cancel.ThrowIfCancellationRequested();
        }

        public async Task WaitForAuthenticationAsync(CancellationToken cancel)
        {
            while (!Authenticated || Suspended) await WaitForChangeAsync(cancel);
            cancel.ThrowIfCancellationRequested();
        }

        private async Task WaitForChangeAsync(CancellationToken cancel)
        {
            var next = signal.Task;
            var stopped = new TaskCompletionSource<bool>();
            using (cancel.Register(() => stopped.TrySetCanceled()))
                await await Task.WhenAny(next, stopped.Task);
        }

        private void Notify()
        {
            var old = signal; signal = new TaskCompletionSource<bool>();
            Changed?.Invoke(); old.TrySetResult(true);
        }
    }
}
