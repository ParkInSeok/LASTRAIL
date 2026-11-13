using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RegistryStartupTests
    {
        internal static string ApprovedDecision(string distribution)
        {
            var json = JObject.Parse(InternalDecision());
            json["accepted"] = true;
            json["errorCode"] = null;
            json["clientAdmission"]["status"] = "ACCEPTED";
            json["clientAdmission"]["distribution"] = distribution;
            json["bootstrap"]["contentAuthorized"] = true;
            json["bootstrap"]["authenticationRequired"] = false;
            json["contentRelease"] = new JObject { ["status"] = "APPROVED_COMPATIBLE", ["releaseId"] = "test-release",
                ["contentVersion"] = "test-content", ["catalogUrl"] = Root + "test-release/catalog.bin",
                ["catalogHash"] = "sha256:" + new string('a', 64),
                ["compatibility"] = new JObject { ["minimumVersionCode"] = "2", ["maximumVersionCode"] = "2" } };
            return json.ToString();
        }

        [Test]
        public async Task InternalWaitsForAuthenticationAndTesterApprovalBeforeAnyResourceWork()
        {
            var backend = new RegistryBackend(true);
            var access = new DownloadAccessPolicy(); access.SetNetwork(ContentNetwork.Wifi);
            var coordinator = new DownloadCoordinator(backend, access);
            using (var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                var run = coordinator.StartAsync("0.2.0", stop.Token);
                Assert.That(coordinator.ConfigAccepted, Is.False);
                Assert.That(backend.Initialized, Is.Zero);
                Assert.That(run.IsCompleted, Is.False);
                access.SetAuthenticated(true);
                await run;
                Assert.That(backend.Authorized, Is.EqualTo(2), "Recheck entitlement after download before entry.");
                Assert.That(backend.Initialized, Is.EqualTo(1));
                Assert.That(coordinator.CanEnterGame, Is.True);
            }
        }

        [Test]
        public async Task StablePredownloadsOnWifiButRevalidatesBeforeEntry()
        {
            var backend = new RegistryBackend(false);
            var access = new DownloadAccessPolicy(); access.SetNetwork(ContentNetwork.Wifi);
            var coordinator = new DownloadCoordinator(backend, access);
            using (var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                var run = coordinator.StartAsync("0.2.0", stop.Token);
                Assert.That(backend.Downloaded, Is.EqualTo(1));
                Assert.That(coordinator.CanEnterGame, Is.False);
                access.SetAuthenticated(true);
                await run;
                Assert.That(backend.Authorized, Is.EqualTo(1));
                Assert.That(coordinator.CanEnterGame, Is.True);
            }
        }

        [Test]
        public async Task TesterDenialNeverInitializesContent()
        {
            var backend = new RegistryBackend(true) { Deny = true };
            var access = new DownloadAccessPolicy(); access.SetNetwork(ContentNetwork.Wifi);
            var coordinator = new DownloadCoordinator(backend, access);
            using (var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                var run = coordinator.StartAsync("0.2.0", stop.Token);
                access.SetAuthenticated(true);
                await run;
                Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.Failed));
                Assert.That(coordinator.ConfigAccepted, Is.False);
                Assert.That(backend.Initialized, Is.Zero);
            }
        }

        private sealed class RegistryBackend : IContentStartupBackend, IAuthenticatedContentBackend
        {
            private readonly bool internalBuild;
            public int Initialized, Downloaded, Authorized;
            public bool Deny;
            public RegistryBackend(bool internalBuild) { this.internalBuild = internalBuild; }
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken cancel) => Task.FromResult(
                ClientAdmissionContract.Parse(internalBuild ? InternalDecision() : ApprovedDecision("STABLE"), "2", Source, Root));
            public Task<ClientContentGate> FetchAuthorizedConfigAsync(CancellationToken cancel)
            {
                Authorized++;
                if (Deny) throw new InvalidOperationException("Tester denied.");
                return Task.FromResult(ClientAdmissionContract.Parse(ApprovedDecision(internalBuild ? "INTERNAL" : "STABLE"), "2", Source, Root));
            }
            public Task InitializeAsync(CancellationToken cancel)
            {
                if (internalBuild) Assert.That(Authorized, Is.EqualTo(1), "Internal authorization must precede all initialization.");
                Initialized++; return Task.CompletedTask;
            }
            public Task UpdateCatalogAsync(CancellationToken cancel) => Task.CompletedTask;
            public Task<long> RequiredBytesAsync(CancellationToken cancel) => Task.FromResult(1L);
            public Task DownloadAsync(Action<long, long> progress, CancellationToken cancel) { Downloaded++; return Task.CompletedTask; }
            public Task VerifyRequiredAssetsAsync(CancellationToken cancel) => Task.CompletedTask;
        }

        internal const string Source = "https://dev-api.lastrail.app/v1/client-config";
        internal const string Root = "https://dev-content.lastrail.app/android/releases/";

        [Test]
        public async Task MissingAuthorizedAdapterFailsInsteadOfLeavingAnUnusableLoginWait()
        {
            var coordinator = new DownloadCoordinator(new MissingAuthorizedBackend(), new DownloadAccessPolicy());
            using (var stop = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                await coordinator.StartAsync("0.2.0", stop.Token);
            Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.Failed));
            Assert.That(coordinator.CanAuthenticate, Is.False);
        }

        private sealed class MissingAuthorizedBackend : IContentStartupBackend
        {
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken cancel) => Task.FromResult(
                ClientAdmissionContract.Parse(InternalDecision(), "2", Source, Root));
            public Task InitializeAsync(CancellationToken cancel) => throw new AssertionException("Content must stay blocked.");
            public Task UpdateCatalogAsync(CancellationToken cancel) => throw new AssertionException("Content must stay blocked.");
            public Task<long> RequiredBytesAsync(CancellationToken cancel) => throw new AssertionException("Content must stay blocked.");
            public Task DownloadAsync(Action<long, long> progress, CancellationToken cancel) => throw new AssertionException("Content must stay blocked.");
            public Task VerifyRequiredAssetsAsync(CancellationToken cancel) => throw new AssertionException("Content must stay blocked.");
        }

        [Test]
        public async Task ResetCancelsPendingInternalAdmissionAndClosesLoginEligibility()
        {
            var access = new DownloadAccessPolicy();
            var backend = new RegistryBackend(true);
            var coordinator = new DownloadCoordinator(backend, access);
            using (var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                var run = coordinator.StartAsync("0.2.0", stop.Token);
                Assert.That(coordinator.CanAuthenticate, Is.True);
                await coordinator.ResetAdmissionAsync();
                Assert.That(run.IsCompleted, Is.True);
                Assert.That(coordinator.CanAuthenticate, Is.False);
                Assert.That(coordinator.ConfigAccepted, Is.False);
                Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.Idle));
                Assert.That(backend.Initialized, Is.Zero);
            }
        }

        internal static string InternalDecision(string status = "AUTH_REQUIRED") => new JObject
        {
            ["accepted"] = false,
            ["registryRevision"] = "\"revision-1\"",
            ["clientAdmission"] = new JObject { ["status"] = status, ["distribution"] = "INTERNAL",
                ["installedVersionCode"] = "2", ["minimumVersionCode"] = "1", ["updateRequired"] = false },
            ["bootstrap"] = new JObject { ["canAuthenticate"] = true, ["contentAuthorized"] = false, ["authenticationRequired"] = true },
            ["contentRelease"] = new JObject { ["status"] = "NOT_APPROVED", ["releaseId"] = null,
                ["contentVersion"] = null, ["catalogUrl"] = null, ["catalogHash"] = null },
            ["maintenance"] = new JObject { ["enabled"] = false },
            ["errorCode"] = status == "AUTH_REQUIRED" ? "authentication_required" : "tester_forbidden"
        }.ToString();

        [Test]
        public void InternalPreAuthAllowsLoginButHasNoUsableContent()
        {
            var gate = ClientAdmissionContract.Parse(InternalDecision(), "2", Source, Root);
            Assert.That(gate.Evaluate(null), Is.EqualTo(ContentStage.AwaitingAuthentication));
            Assert.That(gate.ApprovedCatalogUrl, Is.Null);
            Assert.That(gate.ApprovedReleaseId, Is.Null);
        }

        [TestCase("TESTER_FORBIDDEN")]
        [TestCase("UNREGISTERED")]
        [TestCase("DISABLED")]
        public void DenialsNeverBecomeContentOrLoginApproval(string status)
        {
            Assert.Throws<InvalidOperationException>(() => ClientAdmissionContract.Parse(InternalDecision(status), "2", Source, Root));
        }

        [TestCase("distribution", "STABLE")]
        [TestCase("installedVersionCode", "3")]
        public void InternalPreAuthMustMatchExactInstalledIdentity(string field, string value)
        {
            var json = JObject.Parse(InternalDecision());
            json["clientAdmission"][field] = value;
            Assert.Throws<InvalidOperationException>(() => ClientAdmissionContract.Parse(json.ToString(), "2", Source, Root));
        }

        [Test]
        public void InternalPreAuthRejectsCatalogLeakOrAuthorizationFlag()
        {
            var json = JObject.Parse(InternalDecision());
            json["contentRelease"]["catalogUrl"] = Root + "private/catalog.bin";
            Assert.Throws<InvalidOperationException>(() => ClientAdmissionContract.Parse(json.ToString(), "2", Source, Root));
            json = JObject.Parse(InternalDecision());
            json["bootstrap"]["contentAuthorized"] = true;
            Assert.Throws<InvalidOperationException>(() => ClientAdmissionContract.Parse(json.ToString(), "2", Source, Root));
        }
    }
}
