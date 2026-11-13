using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Resources;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class SessionContentAuthorizationTests
    {
        const string Source = "https://api.lastrail.app/v1/client-config", Root = "https://example.test/";
        static string Receipt(string name) => File.ReadAllText("QA/Unity1Artifacts/Reports/Server2-AdmissionSamples-20260914/" + name + ".json");
        static GameApiAuthSessionSnapshot Session(string account = "local-a") => new GameApiAuthSessionSnapshot(account, "ACTIVE",
            new GameApiAccessToken("lst1.local-fixture"), DateTime.UtcNow.AddMinutes(5), new GameApiRefreshToken("local-fixture"), DateTime.UtcNow.AddMinutes(10));

        [TestCase(401)][TestCase(500)][TestCase(200)]
        public void ForbiddenBodyOnOtherHttpStatusNeverBecomesTesterPopup(int status)
        {
            var error = Assert.Throws<InvalidOperationException>(() => SessionContentAuthorization.ParseResponse(status, Receipt("tester-forbidden"), "2", Source, Root));
            Assert.That(error, Is.Not.InstanceOf<TesterContentForbiddenException>());
        }

        [TestCase("errorCode", "ce03_account_not_allowed")]
        [TestCase("clientAdmission.status", "AUTH_REQUIRED")]
        [TestCase("clientAdmission.distribution", "STABLE")]
        [TestCase("clientAdmission.installedVersionCode", "3")]
        [TestCase("contentRelease.catalogUrl", "https://example.test/catalog.bin")]
        public void UnrelatedOrInconsistent403NeverGetsTesterClassification(string path, string value)
        {
            var receipt = JObject.Parse(Receipt("tester-forbidden")); receipt.SelectToken(path).Replace(new JValue(value));
            var error = Assert.Throws<InvalidOperationException>(() => SessionContentAuthorization.ParseResponse(403, receipt.ToString(), "2", Source, Root));
            Assert.That(error, Is.Not.InstanceOf<TesterContentForbiddenException>());
        }

        [TestCase(false)][TestCase(true)]
        public async Task MissingOrExpiredSessionSendsNothing(bool expired)
        {
            var owner = expired ? Session() : null; int sends = 0;
            using var client = new SessionContentAuthorization(Source, "2", Root, () => owner, () => 1,
                (url, token, code, cancel) => { sends++; return Task.FromResult(RunApiHttpResponse.Success(200, Receipt("authorized"))); },
                () => DateTime.UtcNow.AddHours(1));
            try { await client.FetchAsync(CancellationToken.None); Assert.Fail("No usable session"); } catch (InvalidOperationException) { }
            Assert.That(sends, Is.Zero); Assert.That(client.HasAuthorization, Is.False);
        }

        [TestCase("account")][TestCase("expiry")][TestCase("cancel")][TestCase("generation")]
        public async Task DelayedReplyCannotAuthorizeChangedOrCancelledOwner(string change)
        {
            var owner = Session(); long generation = 1; var now = DateTime.UtcNow;
            var reply = new TaskCompletionSource<RunApiHttpResponse>(); using var cancel = new CancellationTokenSource();
            using var client = new SessionContentAuthorization(Source, "2", Root, () => owner, () => generation, (u,t,c,k) => reply.Task, () => now);
            var pending = client.FetchAsync(cancel.Token);
            if (change == "account") owner = Session("local-b");
            else if (change == "expiry") now = now.AddHours(1);
            else if (change == "generation") generation++;
            else cancel.Cancel();
            reply.SetResult(RunApiHttpResponse.Success(200, Receipt("authorized")));
            try { await pending; Assert.Fail("Stale response published"); }
            catch (ContentSessionExpiredException) { Assert.That(change, Is.EqualTo("expiry")); }
            catch (OperationCanceledException) { Assert.That(change, Is.Not.EqualTo("expiry")); }
            Assert.That(client.HasAuthorization, Is.False);
        }

        [Test]
        public async Task ReversedSameSessionRepliesPublishOnlyNewestRequest()
        {
            var owner = Session(); var first = new TaskCompletionSource<RunApiHttpResponse>(); int sends = 0;
            using var client = new SessionContentAuthorization(Source, "2", Root, () => owner, () => 1,
                (url, token, code, cancel) =>
                {
                    Assert.That(url, Is.EqualTo(Source + "/authorized")); Assert.That(code, Is.EqualTo("2"));
                    return ++sends == 1 ? first.Task : Task.FromResult(RunApiHttpResponse.Success(200, Receipt("authorized")));
                });
            var older = client.FetchAsync(CancellationToken.None); await client.FetchAsync(CancellationToken.None);
            first.SetResult(RunApiHttpResponse.Success(403, Receipt("tester-forbidden")));
            try { await older; Assert.Fail("Older reply published"); } catch (OperationCanceledException) { }
            Assert.That(client.AuthorizationCurrent, Is.True);
            owner = Session("local-b"); Assert.That(client.AuthorizationCurrent, Is.False);
        }

        sealed class Backend : IContentStartupBackend, IAuthenticatedContentBackend
        {
            public SessionContentAuthorization Client; public int ContentCalls;
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken c) => Task.FromResult(ClientAdmissionContract.Parse(Receipt("auth-required"), "2", Source, Root));
            public Task<ClientContentGate> FetchAuthorizedConfigAsync(CancellationToken c) => Client.FetchAsync(c);
            public Task InitializeAsync(CancellationToken c) { ContentCalls++; return Task.CompletedTask; }
            public Task UpdateCatalogAsync(CancellationToken c) { ContentCalls++; return Task.CompletedTask; }
            public Task<long> RequiredBytesAsync(CancellationToken c) { ContentCalls++; return Task.FromResult(10L); }
            public Task DownloadAsync(Action<long,long> progress, CancellationToken c) { ContentCalls++; progress(10,10); return Task.CompletedTask; }
            public Task VerifyRequiredAssetsAsync(CancellationToken c) { ContentCalls++; return Task.CompletedTask; }
        }

        [Test]
        public async Task ExpiryAtReplyIsFailedWithLoginRecoveryInsteadOfCancelledWaiting()
        {
            var owner = Session(); var now = DateTime.UtcNow; var reply = new TaskCompletionSource<RunApiHttpResponse>();
            var access = new DownloadAccessPolicy(); access.SetNetwork(ContentNetwork.Wifi); access.SetAuthenticated(true);
            using var client = new SessionContentAuthorization(Source, "2", Root, () => owner, () => 1, (u,t,c,k) => reply.Task, () => now);
            var backend = new Backend { Client = client }; var coordinator = new DownloadCoordinator(backend, access);
            var operation = coordinator.StartAsync("0.2.0", CancellationToken.None);
            now = now.AddHours(1); reply.SetResult(RunApiHttpResponse.Success(200, Receipt("authorized"))); await operation;
            Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.Failed));
            Assert.That(coordinator.Failure, Is.TypeOf<ContentSessionExpiredException>());
            Assert.That(backend.ContentCalls, Is.Zero); Assert.That(client.HasSessionWork, Is.False);
            var view = LastRail.Presentation.Startup.StartupScreenViewState.SessionExpired();
            Assert.That(view.IsBusy, Is.False); Assert.That(view.PrimaryLabel, Is.EqualTo("로그인 화면으로"));
        }

        [TestCase(false)][TestCase(true)]
        public async Task ActualCoordinatorKeepsContentBlockedUntilServerAuthorization(bool allow)
        {
            var owner = Session(); var access = new DownloadAccessPolicy(); access.SetNetwork(ContentNetwork.Wifi);
            using var client = new SessionContentAuthorization(Source, "2", Root, () => owner, () => 1,
                (u,t,c,k) => Task.FromResult(RunApiHttpResponse.Success(allow ? 200 : 403, Receipt(allow ? "authorized" : "tester-forbidden"))));
            var backend = new Backend { Client = client }; var coordinator = new DownloadCoordinator(backend, access);
            var operation = coordinator.StartAsync("0.2.0", CancellationToken.None);
            Assert.That(coordinator.CanAuthenticate, Is.True); Assert.That(coordinator.ConfigAccepted, Is.False); Assert.That(backend.ContentCalls, Is.Zero);
            access.SetAuthenticated(true); await operation;
            Assert.That(coordinator.CanEnterGame, Is.EqualTo(allow)); Assert.That(backend.ContentCalls, Is.EqualTo(allow ? 5 : 0));
            if (!allow) Assert.That(coordinator.Failure, Is.TypeOf<TesterContentForbiddenException>());
        }
        [Test]
        public void ProductionBackendExposesExistingAuthenticatedContract() =>
            Assert.That(typeof(IAuthenticatedContentBackend).IsAssignableFrom(typeof(AddressableContentBackend)), Is.True);

        [Test]
        public void ExactServerForbiddenReceiptHasDistinctDeniedState()
        {
            var type = typeof(ClientAdmissionContract).Assembly.GetType("LastRail.Resources.SessionContentAuthorization");
            Assert.That(type, Is.Not.Null, "Session-owned production content authorization is missing.");
            var json = File.ReadAllText("QA/Unity1Artifacts/Reports/Server2-AdmissionSamples-20260914/tester-forbidden.json");
            try
            {
                type.GetMethod("ParseResponse").Invoke(null, new object[] { 403, json, "2", "https://api.lastrail.app/v1/client-config", "https://example.test/" });
                Assert.Fail("Rejected content must not return a plan.");
            }
            catch (TargetInvocationException error) { Assert.That(error.InnerException.GetType().Name, Is.EqualTo("TesterContentForbiddenException")); }
        }
    }
}
