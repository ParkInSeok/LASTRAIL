using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Resources;
using LastRail.Run;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using LastRail.Presentation.AuthGate;
using LastRail.Presentation.Startup;
using UnityEditor;

namespace LastRail.Tests.EditMode
{
    public sealed class StartupRecoveryTests
    {
        private sealed class Transport : IRunApiHttpTransport
        {
            private readonly RunApiHttpResponse response;
            public Transport(int code, string body) { response = RunApiHttpResponse.Success(code, body); }
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request) => Task.FromResult(response);
        }
        [TestCase(429, "RateLimited")]
        [TestCase(503, "ServiceUnavailable")]
        [TestCase(401, "AuthRejected")]
        [TestCase(200, "InvalidResponse")]
        public async Task RefreshFailuresHaveSafeDistinctOutcomes(int status, string expected)
        {
            var client = new GameApiRefreshAuthHttpClient(new Transport(status, "{}"));
            var result = await client.RefreshAsync(GameApiRefreshAuthRequest.Create(new GameApiRefreshToken("fixture")));
            Assert.That(result.Outcome.ToString(), Is.EqualTo(expected));
        }
        [Test]
        public void TypedReadAndEntryLifecycleExist()
        {
            Assert.That(typeof(AndroidUnityKeystoreAuthTokenBridge).GetMethod("ReadAsync"), Is.Not.Null);
            Assert.That(typeof(DownloadCoordinator).Assembly.GetType("LastRail.Resources.AuthenticatedScreenEntry"), Is.Not.Null);
        }
        [TestCase(GameApiRefreshAuthClientOutcome.NetworkError)]
        [TestCase(GameApiRefreshAuthClientOutcome.RateLimited)]
        [TestCase(GameApiRefreshAuthClientOutcome.ServiceUnavailable)]
        [TestCase(GameApiRefreshAuthClientOutcome.InvalidResponse)]
        [TestCase(GameApiRefreshAuthClientOutcome.UnknownError)]
        public void RecoverableOrUnknownRefreshNeverOffersNewGuest(GameApiRefreshAuthClientOutcome outcome)
        {
            var result = outcome == GameApiRefreshAuthClientOutcome.NetworkError ? GameApiRefreshAuthClientResult.NetworkError("fixture") : GameApiRefreshAuthClientResult.Failure(outcome);
            var view = AuthGateUxState.FromRefreshResult(result);
            Assert.That(view.Kind, Is.EqualTo(AuthGateUxStateKind.RetryableNetworkError));
            Assert.That(view.CanContinueAsGuest, Is.False);
            Assert.That(view.SecondaryActionLabel, Is.Null);
        }
        [Test]
        public async Task OldEntryCannotClearNewAccountLatch()
        {
            var calls = new List<TaskCompletionSource<bool>>();
            var entry = new AuthenticatedScreenEntry(_ => { var next = new TaskCompletionSource<bool>(); calls.Add(next); return next.Task; }, () => { });
            var first = entry.EnterAsync(CancellationToken.None);
            entry.Reset();
            var second = entry.EnterAsync(CancellationToken.None);
            calls[0].SetResult(true); Assert.That(await first, Is.False);
            Assert.That(entry.Entering, Is.True);
            Assert.That(await entry.EnterAsync(CancellationToken.None), Is.False);
            calls[1].SetResult(true); Assert.That(await second, Is.True);
            Assert.That(entry.Entered, Is.True); Assert.That(entry.Failed, Is.False);
        }
        [Test]
        public async Task EntryFailureRemainsUntilExplicitRetryAndOldFailureCannotOverwriteNewEntry()
        {
            var count = 0;
            var entry = new AuthenticatedScreenEntry(_ => ++count == 1 ? Task.FromException(new IOException("fixture")) : Task.CompletedTask, () => { });
            Assert.That(await entry.EnterAsync(CancellationToken.None), Is.False);
            Assert.That(entry.Failed, Is.True); Assert.That(entry.Entering, Is.False);
            Assert.That(await entry.EnterAsync(CancellationToken.None), Is.True); Assert.That(entry.Failed, Is.False);
            var old = new TaskCompletionSource<bool>(); var first = true;
            entry = new AuthenticatedScreenEntry(_ => { if (first) { first = false; return old.Task; } return Task.CompletedTask; }, () => { });
            var pending = entry.EnterAsync(CancellationToken.None); entry.Reset();
            Assert.That(await entry.EnterAsync(CancellationToken.None), Is.True);
            old.SetException(new IOException("fixture")); await pending;
            Assert.That(entry.Entered, Is.True); Assert.That(entry.Failed, Is.False);
        }
        [TestCase("missing", GameApiAuthTokenReadOutcome.Missing)]
        [TestCase("unavailable", GameApiAuthTokenReadOutcome.Unavailable)]
        [TestCase("read", GameApiAuthTokenReadOutcome.ReadError)]
        [TestCase("corrupt", GameApiAuthTokenReadOutcome.Corrupt)]
        [TestCase("decrypt", GameApiAuthTokenReadOutcome.Corrupt)]
        [TestCase("empty", GameApiAuthTokenReadOutcome.Corrupt)]
        [TestCase("loaded", GameApiAuthTokenReadOutcome.Loaded)]
        public async Task SecureReadDistinguishesNoValueFromFailures(string mode, GameApiAuthTokenReadOutcome expected)
        {
            var native = new Native { Mode = mode };
            var store = new AndroidKeystoreGameApiAuthTokenStore(new AndroidUnityKeystoreAuthTokenBridge(native));
            var result = await store.ReadAsync();
            Assert.That(result.Outcome, Is.EqualTo(expected));
            Assert.That(result.Session != null, Is.EqualTo(expected == GameApiAuthTokenReadOutcome.Loaded));
            Assert.That(native.Writes, Is.Zero);
        }
        private sealed class Native : IAndroidKeystoreNativeStoragePort
        {
            public string Mode; public int Writes;
            public bool IsAndroidRuntime => true;
            public bool IsKeystoreAvailable => Mode != "unavailable";
            public string ProviderName => "fixture";
            public bool UsedPlayerPrefsFallback => false;
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string text) => throw new NotSupportedException();
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload payload)
            {
                if (Mode == "decrypt") throw new InvalidOperationException();
                if (Mode == "empty") return "";
                return AndroidKeystoreAuthTokenPayloadJsonAdapter.ToJson(AndroidKeystoreAuthTokenPayload.FromSnapshot(SupportInquiryTests.Session()));
            }
            public AndroidKeystoreEncryptedTokenPayload Load()
            {
                if (Mode == "read") throw new IOException();
                if (Mode == "corrupt") throw new FormatException();
                return Mode == "missing" ? null : new AndroidKeystoreEncryptedTokenPayload("YQ==", "Yg==");
            }
            public bool Save(AndroidKeystoreEncryptedTokenPayload value) { Writes++; return true; }
            public bool Clear() { Writes++; return true; }
        }
        [Test]
        public void MaintenancePrimaryActionRetriesConfig()
        {
            var root = new GameObject("InactiveBootstrapTest"); root.SetActive(false);
            var controller = root.AddComponent<ResourceBootstrapController>();
            var access = new DownloadAccessPolicy(); var backend = new MaintenanceBackend();
            var coordinator = new DownloadCoordinator(backend, access);
            var network = new ContentNetworkService(); var lifetime = new CancellationTokenSource();
            try
            {
                coordinator.StartAsync("1.0", lifetime.Token).GetAwaiter().GetResult();
                Set(controller, "access", access); Set(controller, "network", network);
                Set(controller, "coordinator", coordinator); Set(controller, "lifetime", lifetime);
                typeof(ResourceBootstrapController).GetMethod("Primary", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(controller, null);
                Assert.That(backend.Calls, Is.EqualTo(2));
            }
            finally { lifetime.Cancel(); lifetime.Dispose(); network.Dispose(); UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public async Task TruncatedSuccessIsNotAnAuthenticatedSession()
        {
            var json = "{\"tokenType\":\"Bearer\",\"account\":{\"id\":\"account_test\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture\",\"accessTokenExpiresAt\":\"2026-09-06T10:00:00.000Z\",\"refreshToken\":\"fixture\",\"refreshTokenExpiresAt\":\"2026-09-07T10:00:00.000Z\"";
            var result = await new GameApiRefreshAuthHttpClient(new Transport(200, json)).RefreshAsync(GameApiRefreshAuthRequest.Create(new GameApiRefreshToken("fixture")));
            Assert.That(result.Outcome, Is.EqualTo(GameApiRefreshAuthClientOutcome.InvalidResponse));
        }
        [Test]
        public async Task AccountReplacementReopensStartupForEntryFailure()
        {
            var root = new GameObject("InactiveBootstrapTest"); root.SetActive(false);
            var controller = root.AddComponent<ResourceBootstrapController>();
            var startup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
            var access = new DownloadAccessPolicy(); access.SetAuthenticated(true);
            var coordinator = new DownloadCoordinator(new MaintenanceBackend { Maintenance = false }, access);
            var network = new ContentNetworkService(); var lifetime = new CancellationTokenSource();
            try
            {
                await coordinator.StartAsync("1.0", lifetime.Token);
                Set(controller, "access", access); Set(controller, "network", network); Set(controller, "coordinator", coordinator);
                Set(controller, "lifetime", lifetime); Set(controller, "startup", startup.GetComponent<StartupScreenRootBehaviour>());
                Set(controller, "entry", new AuthenticatedScreenEntry(_ => Task.FromException(new IOException()), () => { }));
                Set(controller, "session", SupportInquiryTests.Session()); startup.SetActive(false);
                await controller.AcceptAuthenticatedSessionAsync(SupportInquiryTests.Session("account_second"));
                Assert.That(startup.activeSelf, Is.True);
            }
            finally { lifetime.Cancel(); lifetime.Dispose(); network.Dispose(); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(startup); }
        }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private sealed class MaintenanceBackend : IContentStartupBackend
        {
            public int Calls;
            public bool Maintenance = true;
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken cancel) { Calls++; return Task.FromResult(new ClientContentGate { Maintenance = Maintenance, MinimumVersion = "1.0" }); }
            public Task InitializeAsync(CancellationToken cancel) => Task.CompletedTask;
            public Task UpdateCatalogAsync(CancellationToken cancel) => Task.CompletedTask;
            public Task<long> RequiredBytesAsync(CancellationToken cancel) => Task.FromResult(0L);
            public Task DownloadAsync(Action<long, long> progress, CancellationToken cancel) => Task.CompletedTask;
            public Task VerifyRequiredAssetsAsync(CancellationToken cancel) => Task.CompletedTask;
        }
    }
}
