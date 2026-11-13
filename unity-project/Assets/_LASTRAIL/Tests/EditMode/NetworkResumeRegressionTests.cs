using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class NetworkResumeRegressionTests
    {
        [TestCase(ContentNetwork.OtherOnline)]
        [TestCase(ContentNetwork.UnknownOnline)]
        public void FirstPlanAfterWifiDepartureDefersConsentUntilAuthentication(ContentNetwork destination)
        {
            var access = new DownloadAccessPolicy();
            access.SetNetwork(ContentNetwork.Wifi);
            access.SetNetwork(destination);
            access.SetPlan("first-release", 100);

            Assert.That(access.Allowed, Is.False);
            Assert.That(access.NeedsConsent, Is.False, "Metadata-time Wi-Fi departure must not interrupt authentication with consent.");
            Assert.That(access.AwaitingNonWifiConsent, Is.True);
            access.SetAuthenticated(false);
            access.Retry();
            access.SetPlan("first-release", 100);
            Assert.That(access.NeedsConsent, Is.False, "Failed authentication and same-plan retry must retain deferral.");
            access.SetAuthenticated(true);
            Assert.That(access.NeedsConsent, Is.True);
            access.Approve();
            Assert.That(access.Allowed, Is.True);
        }

        [TestCase(ContentNetwork.OtherOnline, false, 1)]
        [TestCase(ContentNetwork.Wifi, true, 0)]
        public void ResumeWaitsForAppliedFreshSnapshotEvenWhenUnchanged(ContentNetwork snapshot, bool allowed, int expectedChanges)
        {
            var previous = SynchronizationContext.Current;
            var main = new QueuedContext();
            SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                var observed = ContentNetwork.Wifi;
                using var service = CreateService(() => observed);
                service.Start();
                main.Drain();
                Assert.That(service.Current, Is.EqualTo(ContentNetwork.Wifi));
                var access = new DownloadAccessPolicy();
                access.SetNetwork(service.Current);
                access.SetPlan("release", 100);
                access.SetSuspended(true);
                var changes = 0;
                var mainThread = Thread.CurrentThread.ManagedThreadId;
                service.NetworkStateChanged += value =>
                {
                    Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThread));
                    Assert.That(access.Suspended, Is.True, "Apply transport before releasing suspension.");
                    changes++;
                    access.SetNetwork(value);
                };

                observed = snapshot;
                var refresh = RefreshAsync(service, CancellationToken.None);
                var resumed = ResumeAfterSnapshotAsync(service, access, refresh, mainThread);
                Assert.That(refresh.IsCompleted, Is.False, "Queued snapshot publication must precede completion.");
                Assert.That(access.Suspended, Is.True);
                Assert.That(access.Allowed, Is.False);

                main.Drain();
                Assert.That(resumed.Status, Is.EqualTo(TaskStatus.RanToCompletion), resumed.Exception?.ToString());
                Assert.That(service.Current, Is.EqualTo(snapshot));
                Assert.That(access.Network, Is.EqualTo(snapshot));
                Assert.That(access.Suspended, Is.False, "An unchanged snapshot must also complete refresh.");
                Assert.That(access.Allowed, Is.EqualTo(allowed));
                Assert.That(changes, Is.EqualTo(expectedChanges));
                if (!allowed)
                {
                    Assert.That(access.NeedsConsent, Is.False);
                    access.SetAuthenticated(true);
                    Assert.That(access.NeedsConsent, Is.True);
                }
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        [Test]
        public void DisposedRefreshCannotPublishIntoRestartedService()
        {
            var previous = SynchronizationContext.Current;
            var main = new QueuedContext();
            SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                var observed = ContentNetwork.Wifi;
                using var service = CreateService(() => observed);
                service.Start();
                main.Drain();
                observed = ContentNetwork.OtherOnline;
                var oldRefresh = RefreshAsync(service, CancellationToken.None);
                Assert.That(oldRefresh.IsCompleted, Is.False);
                service.Dispose();
                observed = ContentNetwork.Wifi;
                service.Start();
                var changes = 0;
                service.NetworkStateChanged += _ => changes++;

                main.Drain();
                Assert.That(oldRefresh.IsCanceled, Is.True, "Disposal must settle the old refresh without authorizing resume.");
                Assert.That(service.Current, Is.EqualTo(ContentNetwork.Wifi));
                Assert.That(changes, Is.Zero, "An old owner cannot publish into the restarted service.");
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        [Test]
        public void UnrelatedNetworkChangeCannotCompletePendingRefresh()
        {
            var previous = SynchronizationContext.Current;
            var main = new QueuedContext();
            SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                using var service = CreateService(() => ContentNetwork.Wifi);
                service.Start();
                main.Drain();
                var refresh = RefreshAsync(service, CancellationToken.None);
                service.InjectForTests(ContentNetwork.OtherOnline);
                Assert.That(refresh.IsCompleted, Is.False, "An unrelated notification cannot supply this request's snapshot.");
                main.Drain();
                Assert.That(refresh.Status, Is.EqualTo(TaskStatus.RanToCompletion), refresh.Exception?.ToString());
                Assert.That(refresh.Result, Is.EqualTo(ContentNetwork.Wifi));
                Assert.That(service.Current, Is.EqualTo(ContentNetwork.Wifi));
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        [Test]
        public void CancelledRefreshDoesNotPublishQueuedSnapshot()
        {
            var previous = SynchronizationContext.Current;
            var main = new QueuedContext();
            SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                var observed = ContentNetwork.Wifi;
                using var service = CreateService(() => observed);
                using var cancel = new CancellationTokenSource();
                service.Start();
                main.Drain();
                var changes = 0;
                service.NetworkStateChanged += _ => changes++;
                observed = ContentNetwork.OtherOnline;
                var refresh = RefreshAsync(service, cancel.Token);
                cancel.Cancel();
                main.Drain();
                Assert.That(refresh.IsCanceled, Is.True);
                Assert.That(service.Current, Is.EqualTo(ContentNetwork.Wifi));
                Assert.That(changes, Is.Zero);
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        // Reflection preserves the original RED contract tests alongside their behavior assertions.
        private static ContentNetworkService CreateService(Func<ContentNetwork> snapshot)
        {
            var constructor = typeof(ContentNetworkService).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(Func<ContentNetwork>) }, null);
            Assert.That(constructor, Is.Not.Null, "ContentNetworkService needs an injectable platform snapshot reader for deterministic refresh ordering.");
            return (ContentNetworkService)constructor.Invoke(new object[] { snapshot });
        }

        private static Task<ContentNetwork> RefreshAsync(ContentNetworkService service, CancellationToken cancel)
        {
            var method = typeof(ContentNetworkService).GetMethod("RefreshAsync", new[] { typeof(CancellationToken) });
            Assert.That(method, Is.Not.Null, "RefreshAsync(CancellationToken) must complete after publishing a fresh snapshot, even when unchanged.");
            var result = method.Invoke(service, new object[] { cancel });
            Assert.That(result, Is.InstanceOf<Task<ContentNetwork>>());
            return (Task<ContentNetwork>)result;
        }

        private static async Task ResumeAfterSnapshotAsync(ContentNetworkService service, DownloadAccessPolicy access,
            Task<ContentNetwork> refresh, int mainThread)
        {
            var snapshot = await refresh;
            Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThread));
            Assert.That(service.Current, Is.EqualTo(snapshot), "Completion cannot precede publication.");
            access.SetNetwork(snapshot);
            access.SetSuspended(false);
        }

        private sealed class QueuedContext : SynchronizationContext
        {
            private readonly Queue<Action> callbacks = new Queue<Action>();

            public override void Post(SendOrPostCallback callback, object state)
            {
                lock (callbacks) callbacks.Enqueue(() => callback(state));
            }

            public void Drain()
            {
                for (var count = 0; count < 100; count++)
                {
                    Action next;
                    lock (callbacks)
                    {
                        if (callbacks.Count == 0) return;
                        next = callbacks.Dequeue();
                    }
                    next();
                }
                Assert.Fail("Snapshot refresh did not settle within 100 queued callbacks.");
            }
        }
    }
}
