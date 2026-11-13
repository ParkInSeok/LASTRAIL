using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class NetworkContentPolicyTests
    {
        private SynchronizationContext context;
        [SetUp] public void PureModelContext() { context = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(null); }
        [TearDown] public void RestoreContext() { SynchronizationContext.SetSynchronizationContext(context); }
        [TestCase(ContentNetwork.Wifi, true, false)]
        [TestCase(ContentNetwork.OtherOnline, false, true)]
        [TestCase(ContentNetwork.UnknownOnline, false, true)]
        [TestCase(ContentNetwork.Offline, false, false)]
        public void InitialNetworkPolicy(ContentNetwork network, bool allowed, bool consent)
        {
            var policy = new DownloadAccessPolicy(); policy.SetNetwork(network); policy.SetPlan("a", 100);
            Assert.That(policy.Allowed, Is.EqualTo(allowed)); Assert.That(policy.NeedsConsent, Is.EqualTo(consent));
            policy.Approve();
            Assert.That(policy.Allowed, Is.EqualTo(network != ContentNetwork.Offline));
        }

        [Test]
        public void WifiTransitionDefersModalUntilLoginAndWifiReturnAutoResumes()
        {
            var p = new DownloadAccessPolicy(); p.SetNetwork(ContentNetwork.Wifi); p.SetPlan("a", 100);
            p.SetNetwork(ContentNetwork.OtherOnline);
            Assert.That(p.Allowed, Is.False); Assert.That(p.NeedsConsent, Is.False); Assert.That(p.AwaitingNonWifiConsent, Is.True);
            p.SetAuthenticated(false); Assert.That(p.NeedsConsent, Is.False, "Login failure must not trigger modal");
            p.SetAuthenticated(true); Assert.That(p.NeedsConsent, Is.True);
            p.SetNetwork(ContentNetwork.Wifi); Assert.That(p.Allowed, Is.True); Assert.That(p.NeedsConsent, Is.False);
            p.SetRemaining(0); p.SetNetwork(ContentNetwork.OtherOnline); Assert.That(p.NeedsConsent, Is.False);
        }

        [Test]
        public void DeclineAndRetryDoNotCreatePermanentOrRepeatedConsent()
        {
            var p = new DownloadAccessPolicy(); p.SetPlan("a", 100);
            p.Decline(); Assert.That(p.NeedsConsent, Is.False); Assert.That(p.Allowed, Is.False);
            p.Retry(); Assert.That(p.NeedsConsent, Is.True); p.Approve();
            p.SetPlan("a", 60); Assert.That(p.Allowed, Is.True); Assert.That(p.NeedsConsent, Is.False);
            p.SetNetwork(ContentNetwork.Offline); Assert.That(p.Allowed, Is.False);
            p.SetNetwork(ContentNetwork.OtherOnline); Assert.That(p.Allowed, Is.True);
            p.SetPlan("b", 60); Assert.That(p.Allowed, Is.False); Assert.That(p.NeedsConsent, Is.True);
            Assert.That(new DownloadAccessPolicy().Allowed, Is.False);
        }

        [Test]
        public async Task SuspendResumeAndCancelReleaseWaiters()
        {
            var p = new DownloadAccessPolicy(); p.SetNetwork(ContentNetwork.Wifi); p.SetSuspended(true);
            using var c = new CancellationTokenSource();
            var pending = p.WaitForAccessAsync(c.Token); Assert.That(pending.IsCompleted, Is.False);
            p.SetSuspended(false); await pending;
            p.SetNetwork(ContentNetwork.Offline);
            pending = p.WaitForAccessAsync(c.Token); c.Cancel();
            Assert.CatchAsync<OperationCanceledException>(async () => await pending);
        }

        [TestCase(false, false, false, false, false, ContentNetwork.Offline)]
        [TestCase(true, true, false, true, false, ContentNetwork.UnknownOnline)]
        [TestCase(true, true, true, true, false, ContentNetwork.Wifi)]
        [TestCase(true, true, true, true, true, ContentNetwork.OtherOnline)]
        public void CapabilitiesAreNotReachabilityOrUnmeteredClaims(bool net, bool internet, bool valid, bool wifi, bool cellular, ContentNetwork result)
            => Assert.That(ContentNetworkAdapter.Classify(net, internet, valid, wifi, cellular), Is.EqualTo(result));

        private sealed class Backend : IContentStartupBackend, IContentRevision
        {
            public string ContentRevision => "revision-a";
            public long Bytes = 100;
            public int Downloads, Loads;
            public bool Maintenance, FailLoad, FailDownload;
            public string Minimum = "0.1.0";
            public TaskCompletionSource<bool> Hold;
            public Action<long, long> Report;
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken c) => Task.FromResult(new ClientContentGate { Maintenance = Maintenance, MinimumVersion = Minimum });
            public Task InitializeAsync(CancellationToken c) => Task.CompletedTask;
            public Task UpdateCatalogAsync(CancellationToken c) => Task.CompletedTask;
            public Task<long> RequiredBytesAsync(CancellationToken c) => Task.FromResult(Bytes);
            public async Task DownloadAsync(Action<long, long> progress, CancellationToken c)
            {
                Downloads++; Report = progress; progress(40, 100);
                if (FailDownload) throw new InvalidOperationException("download failed");
                if (Hold != null) await Hold.Task;
            }
            public Task VerifyRequiredAssetsAsync(CancellationToken c)
            {
                Loads++; if (FailLoad) throw new InvalidOperationException("load failed"); return Task.CompletedTask;
            }
        }

        [Test]
        public async Task LoginAndContentShareOneOperationWithLiveByteProgress()
        {
            var p = new DownloadAccessPolicy(); p.SetNetwork(ContentNetwork.Wifi);
            var b = new Backend { Hold = new TaskCompletionSource<bool>() }; var owner = new DownloadCoordinator(b, p);
            var loginShown = false; owner.ConfigPassed += () => loginShown = true;
            var task = owner.StartAsync("0.1.0", CancellationToken.None);
            Assert.That(loginShown, Is.True); Assert.That(b.Downloads, Is.EqualTo(1));
            p.SetAuthenticated(true);
            Assert.That(owner.StartAsync("0.1.0", CancellationToken.None), Is.SameAs(task));
            Assert.That(owner.ReceivedBytes, Is.EqualTo(40)); Assert.That(owner.TotalBytes, Is.EqualTo(100));
            b.Report(100, 100); Assert.That(owner.CanEnterGame, Is.False);
            b.Hold.SetResult(true); await task;
            Assert.That(owner.CanEnterGame, Is.True); Assert.That(b.Loads, Is.EqualTo(1)); Assert.That(b.Downloads, Is.EqualTo(1));
        }

        [TestCase(0)][TestCase(100)]
        public async Task DownloadCompletedBeforeLoginDoesNotLoadOrEnter(long bytes)
        {
            var p = new DownloadAccessPolicy(); p.SetNetwork(ContentNetwork.Wifi);
            var b = new Backend { Bytes = bytes }; var owner = new DownloadCoordinator(b, p);
            var task = owner.StartAsync("0.1.0", CancellationToken.None);
            Assert.That(owner.Stage, Is.EqualTo(ContentStage.AwaitingAuthentication)); Assert.That(b.Loads, Is.Zero);
            Assert.That(p.NeedsConsent, Is.False); p.SetAuthenticated(true); await task;
            Assert.That(owner.CanEnterGame, Is.True); Assert.That(b.Downloads, Is.EqualTo(bytes == 0 ? 0 : 1));
        }

        [Test]
        public async Task InitialNonWifiDoesNotDownloadUntilExplicitApproval()
        {
            var p = new DownloadAccessPolicy(); var b = new Backend(); var owner = new DownloadCoordinator(b, p);
            var task = owner.StartAsync("0.1.0", CancellationToken.None);
            Assert.That(owner.ConfigAccepted, Is.True); Assert.That(b.Downloads, Is.Zero);
            p.Decline(); p.SetAuthenticated(true); Assert.That(b.Downloads, Is.Zero); Assert.That(owner.CanEnterGame, Is.False);
            p.Retry(); p.Approve(); await task; Assert.That(b.Downloads, Is.EqualTo(1));
        }

        [TestCase(true, "0.1.0")][TestCase(false, "9.0.0")]
        public async Task VersionOrMaintenanceBlocksLoginAndDownload(bool maintenance, string minimum)
        {
            var p = new DownloadAccessPolicy(); var b = new Backend { Maintenance = maintenance, Minimum = minimum };
            var owner = new DownloadCoordinator(b, p); var restore = false; owner.ConfigPassed += () => restore = true;
            await owner.StartAsync("0.1.0", CancellationToken.None);
            Assert.That(restore, Is.False); Assert.That(owner.ConfigAccepted, Is.False); Assert.That(b.Downloads, Is.Zero);
        }

        [Test]
        public async Task LoadFailureNeverBecomesReadyAndRetainsAuthGate()
        {
            var p = new DownloadAccessPolicy(); p.SetNetwork(ContentNetwork.Wifi); p.SetAuthenticated(true);
            var owner = new DownloadCoordinator(new Backend { Bytes = 0, FailLoad = true }, p);
            await owner.StartAsync("0.1.0", CancellationToken.None);
            Assert.That(owner.Stage, Is.EqualTo(ContentStage.Failed)); Assert.That(owner.CanEnterGame, Is.False);
            Assert.That(p.Authenticated, Is.True);
        }

        [Test]
        public async Task DownloadFailureDoesNotReplaceAuthenticationAndCancellationRejectsLateSuccess()
        {
            var p = new DownloadAccessPolicy(); p.SetNetwork(ContentNetwork.Wifi); p.SetAuthenticated(true);
            var b = new Backend { FailDownload = true }; var owner = new DownloadCoordinator(b, p);
            await owner.StartAsync("0.1.0", CancellationToken.None);
            Assert.That(owner.Stage, Is.EqualTo(ContentStage.Failed)); Assert.That(p.Authenticated, Is.True);
            b.FailDownload = false; b.Hold = new TaskCompletionSource<bool>();
            using var stop = new CancellationTokenSource();
            var run = owner.StartAsync("0.1.0", stop.Token); stop.Cancel(); b.Hold.SetResult(true); await run;
            Assert.That(owner.Stage, Is.EqualTo(ContentStage.Cancelled)); Assert.That(owner.CanEnterGame, Is.False); Assert.That(b.Loads, Is.Zero);
        }
    }
}
