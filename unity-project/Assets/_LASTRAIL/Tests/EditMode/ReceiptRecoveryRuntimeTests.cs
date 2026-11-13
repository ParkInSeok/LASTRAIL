using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Presentation.Startup;
using LastRail.Resources;
using LastRail.Run;
using LastRail.Submissions;
using LastRail.Support;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    // Lobby recurring sync is independent; every other unexpected POST remains forbidden.
    // Composition RED tests. All journal/HTTP data is explicitly in-memory; no process is launched.
    public sealed class ReceiptRecoveryRuntimeTests
    {
        private const string SupportKey = "support_receipt_runtime_key";
        private const string ErrorKey = "error_receipt_runtime_key";
        private const string Story = "{\"accepted\":true,\"actId\":\"act_01_wasteland_railway\",\"storyProgress\":{\"highestStoryProgressId\":\"none\",\"highestClearedOperationId\":null,\"order\":0},\"clearedOperationIds\":[],\"unlockedOperations\":[\"opn_01_last_departure\"],\"narrativeVersion\":\"fixture_narrative\",\"narrative\":{\"contentStatus\":\"CANDIDATE\",\"localizationStatus\":\"PENDING\",\"fixtureStatus\":\"PARTIAL\",\"missingContentStatus\":\"UNKNOWN\"}}";

        private sealed class Journal : IPendingReceiptJournal
        {
            internal readonly List<PendingReceipt> Records = new List<PendingReceipt>();
            internal readonly List<string> Reads = new List<string>();
            internal int Puts;
            internal bool FailRead;
            internal TaskCompletionSource<bool> NextRead;
            public Task<bool> PutAsync(PendingReceipt record) { Puts++; Records.Add(record); return Task.FromResult(true); }
            public async Task<IReadOnlyList<PendingReceipt>> ReadAsync(string account)
            {
                Reads.Add(account);
                var wait = NextRead; NextRead = null;
                var result = Records.Where(r => r.AccountId == account).ToArray();
                if (wait != null) await wait.Task;
                if (FailRead) throw new InvalidOperationException("Synthetic receipt storage unavailable");
                return result;
            }
            public Task<bool> RemoveAsync(string account, PendingReceiptKind kind, string key)
            { Records.RemoveAll(r => r.AccountId == account && r.Kind == kind && r.IdempotencyKey == key); return Task.FromResult(true); }
        }

        private sealed class Wire : IRunApiHttpTransport
        {
            internal string Account = "account_test";
            internal readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Requests.Add(request);
                if (request.EndpointPath.EndsWith("/receipt", StringComparison.Ordinal))
                    return Task.FromResult(RunApiHttpResponse.Success(404, "{\"accepted\":false,\"receiptStatus\":\"NOT_FOUND\"}"));
                if (request.EndpointPath == "/v1/profile")
                    return Task.FromResult(RunApiHttpResponse.Success(200, new JObject {
                        ["accepted"] = true, ["account"] = new JObject { ["id"] = Account },
                        ["profile"] = new JObject { ["displayName"] = "fixture_name", ["commanderXp"] = 0 }
                    }.ToString()));
                if (request.EndpointPath == "/v1/story-progress") return Task.FromResult(RunApiHttpResponse.Success(200, Story));
                return Task.FromResult(RunApiHttpResponse.NetworkError("Unexpected fixture route"));
            }
        }

        private sealed class Store : IGameApiAuthTokenStore, IGameApiAuthTokenReader
        {
            private GameApiAuthSessionSnapshot session;
            public Task<GameApiAuthSessionSnapshot> LoadAsync() => throw new InvalidOperationException("Legacy store read forbidden");
            public Task<GameApiAuthTokenReadResult> ReadAsync() => Task.FromResult(new GameApiAuthTokenReadResult(
                session == null ? GameApiAuthTokenReadOutcome.Missing : GameApiAuthTokenReadOutcome.Loaded, session));
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value)
            { session = value; return Task.FromResult(GameApiAuthTokenStoreResult.Saved); }
        }

        private sealed class Fixture : IDisposable
        {
            internal readonly Journal Journal = new Journal();
            internal readonly Wire Wire = new Wire();
            internal readonly GameObject Root, Startup, Lobby;
            internal readonly PlayerEntryRuntime Runtime;
            internal Fixture()
            {
                var constructor = Constructor();
                Root = new GameObject("InactiveReceiptRecoveryFixture"); Root.SetActive(false);
                var bootstrap = Root.AddComponent<ResourceBootstrapController>();
                Startup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
                Lobby = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab"));
                Set(bootstrap, "startup", Startup.GetComponent<StartupScreenRootBehaviour>());
                Set(bootstrap, "access", new DownloadAccessPolicy());
                Set(bootstrap, "entry", new AuthenticatedScreenEntry(_ => Task.CompletedTask, () => { }));
                var store = new Store();
                Runtime = (PlayerEntryRuntime)constructor.Invoke(new object[] { bootstrap,
                    AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),
                    Wire, store, store, (Func<AppBuildSnapshot>)(() => new AppBuildSnapshot("1.2.3", 123)), Journal });
            }
            internal async Task Login(string account = "account_test")
            {
                Wire.Account = account;
                await Runtime.Owner.LoginAsync(() => Task.FromResult(SupportInquiryTests.Session(account)));
                Assert.That(Runtime.Owner.Ready, Is.True);
                Assert.That(Runtime.BindLobby(Lobby), Is.True);
                await Settle();
            }
            internal void Open() => Click(Lobby, "SupportInquiry");
            internal SupportInquiryPanel Panel => Root.GetComponentInChildren<SupportInquiryPanel>(true);
            public void Dispose()
            { Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(Lobby); UnityEngine.Object.DestroyImmediate(Startup); UnityEngine.Object.DestroyImmediate(Root); }
        }

        [Test]
        public void SevenArgumentCompositionAndLazyPublicRecoveryContractExist()
        {
            Constructor();
            var old = new[] { typeof(ResourceBootstrapController), typeof(TMP_FontAsset), typeof(IRunApiHttpTransport),
                typeof(IGameApiAuthTokenStore), typeof(IGameApiAuthTokenReader) };
            Assert.That(typeof(PlayerEntryRuntime).GetConstructor(old), Is.Not.Null);
            Assert.That(typeof(PlayerEntryRuntime).GetConstructor(old.Concat(new[] { typeof(Func<AppBuildSnapshot>) }).ToArray()), Is.Not.Null);
            var property = RecoveryProperty();
            Assert.That(property.PropertyType.Name, Is.EqualTo("SupportReceiptRecovery"));
            Assert.That(property.PropertyType.FullName, Is.EqualTo("LastRail.Support.SupportReceiptRecovery"));
            Assert.That(property.PropertyType.IsPublic, Is.True);
            Assert.That(property.GetSetMethod(), Is.Null);
            Assert.That(property.PropertyType.GetConstructor(new[] { typeof(SupportInquiryController), typeof(SupportInquiryHttpClient),
                typeof(IPendingReceiptJournal), typeof(Func<GameApiAuthSessionSnapshot>), typeof(Func<long>) }), Is.Not.Null);
            foreach (var name in new[] { "Records", "Selected", "Busy", "StorageFailed", "Notice" })
                Assert.That(property.PropertyType.GetProperty(name), Is.Not.Null, name);
            Assert.That(property.PropertyType.GetMethod("RefreshAsync", Type.EmptyTypes), Is.Not.Null);
            Assert.That(property.PropertyType.GetMethod("Select", new[] { typeof(PendingReceipt) }), Is.Not.Null);
            Assert.That(property.PropertyType.GetMethod("StartIndependentInquiry", Type.EmptyTypes), Is.Not.Null);
            Assert.That(property.PropertyType.GetMethod("ForgetSelectedAsync", new[] { typeof(bool) }), Is.Not.Null);
            Assert.That(typeof(IDisposable).IsAssignableFrom(property.PropertyType), Is.True);
            using (var f = new Fixture()) Assert.That(property.GetValue(f.Runtime), Is.Null);
        }

        [Test]
        public async Task PublicSupportOpenRestoresReceiptOnlyAndReconcileUsesOriginalKey()
        {
            using (var f = new Fixture())
            {
                f.Journal.Records.Add(Record(PendingReceiptKind.Support, SupportKey));
                await f.Login();
                Assert.That(RecoveryProperty().GetValue(f.Runtime), Is.Null);
                var reads = f.Journal.Reads.Count;
                f.Open(); await Settle();
                Assert.That(RecoveryProperty().GetValue(f.Runtime), Is.Not.Null);
                Assert.That(f.Journal.Reads.Count, Is.GreaterThan(reads));
                Assert.That(f.Journal.Reads.Last(), Is.EqualTo("account_test"));
                SelectSupport(f);
                Assert.That(f.Panel, Is.Not.Null);
                Assert.That(Find<Button>(f.Panel.gameObject, "Send").interactable, Is.False);
                Assert.That(Find<TMP_InputField>(f.Panel.gameObject, "Content").text, Is.Empty);
                Assert.That(Find<TMP_InputField>(f.Panel.gameObject, "Content").interactable, Is.False);
                Click(f.Panel.gameObject, "Reconcile"); await Settle();
                var lookups = f.Wire.Requests.Where(r => r.EndpointPath == "/v1/support/tickets/receipt").ToArray();
                Assert.That(lookups.Length, Is.GreaterThan(0));
                foreach (var lookup in lookups) AssertLookup(lookup, SupportKey);
                Assert.That(f.Wire.Requests.Any(r => r.Method == "POST" && r.EndpointPath != "/v1/recurring/sync"), Is.False);
                Assert.That(f.Journal.Puts, Is.Zero);
                Assert.That(f.Journal.Records.Count, Is.EqualTo(1), "404 must preserve metadata.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task DelayedSupportReadCannotLookupAfterCancelOrAccountSwitch(bool switchAccount)
        {
            using (var f = new Fixture())
            {
                f.Journal.Records.Add(Record(PendingReceiptKind.Support, SupportKey));
                await f.Login();
                var delay = new TaskCompletionSource<bool>(); f.Journal.NextRead = delay;
                f.Open();
                var oldRecovery = RecoveryProperty().GetValue(f.Runtime);
                Assert.That(oldRecovery, Is.Not.Null);
                if (switchAccount) await f.Login("account_other");
                else Click(f.Root, "Cancel");
                delay.SetResult(true); await Settle();
                Assert.That(f.Wire.Requests.Any(r => r.EndpointPath == "/v1/support/tickets/receipt"), Is.False);
                Assert.That(f.Panel == null || !f.Panel.gameObject.activeSelf, Is.True);
                Assert.That(f.Journal.Records.Count, Is.EqualTo(1));
                if (switchAccount)
                {
                    f.Open(); await Settle();
                    Assert.That(RecoveryProperty().GetValue(f.Runtime), Is.Not.SameAs(oldRecovery));
                    Assert.That(f.Journal.Reads.Last(), Is.EqualTo("account_other"));
                    Assert.That(Find<TMP_InputField>(f.Panel.gameObject, "Content").text, Is.Empty);
                    Assert.That(f.Wire.Requests.Any(r => r.EndpointPath == "/v1/support/tickets/receipt"), Is.False);
                }
            }
        }

        [Test]
        public async Task ReadyReconcilesOnlyCurrentAccountErrorsThroughTheSameJournalWithoutPost()
        {
            using (var f = new Fixture())
            {
                f.Journal.Records.Add(Record(PendingReceiptKind.ClientError, ErrorKey));
                f.Journal.Records.Add(Record(PendingReceiptKind.Support, SupportKey));
                f.Journal.Records.Add(Record(PendingReceiptKind.ClientError, "foreign_error_receipt_key", "account_other"));
                Assert.That(f.Wire.Requests, Is.Empty);
                await f.Login();
                var errors = f.Wire.Requests.Where(r => r.EndpointPath == "/v1/client-errors/receipt").ToArray();
                Assert.That(errors.Length, Is.EqualTo(1)); AssertLookup(errors[0], ErrorKey);
                Assert.That(f.Runtime.Diagnostics.PendingCount, Is.Zero);
                Assert.That(f.Wire.Requests.Any(r => r.EndpointPath == "/v1/support/tickets/receipt"), Is.False);
                f.Open(); await Settle();
                var recovery = RecoveryProperty().GetValue(f.Runtime);
                var records = ((IEnumerable<PendingReceipt>)recovery.GetType().GetProperty("Records").GetValue(recovery)).ToArray();
                Assert.That(records.Select(r => r.Kind), Is.EquivalentTo(new[] { PendingReceiptKind.Support, PendingReceiptKind.ClientError }));
                Assert.That(records.All(r => r.AccountId == "account_test"), Is.True);
                SelectSupport(f); Click(f.Panel.gameObject, "Reconcile"); await Settle();
                Assert.That(f.Wire.Requests.Any(r => r.EndpointPath == "/v1/support/tickets/receipt"), Is.True);
                Assert.That(f.Wire.Requests.Any(r => r.Method == "POST" && r.EndpointPath != "/v1/recurring/sync"), Is.False);
                Assert.That(f.Journal.Puts, Is.Zero);
                Assert.That(f.Journal.Records.Count, Is.EqualTo(3));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task SupportStorageFailureIsVisibleAndCannotOfferAnUntrackedSend(bool failsAtReady)
        {
            using (var f = new Fixture())
            {
                f.Journal.FailRead = failsAtReady;
                await f.Login(); f.Journal.FailRead = true;
                f.Open(); await Settle();
                var recovery = RecoveryProperty().GetValue(f.Runtime);
                Assert.That(recovery, Is.Not.Null);
                Assert.That(recovery.GetType().GetProperty("StorageFailed").GetValue(recovery), Is.EqualTo(true));
                Assert.That(string.IsNullOrWhiteSpace((string)recovery.GetType().GetProperty("Notice").GetValue(recovery)), Is.False);
                Assert.That(f.Panel, Is.Not.Null);
                Assert.That(f.Panel.gameObject.activeSelf, Is.True);
                Assert.That(string.IsNullOrWhiteSpace(Find<TMP_Text>(f.Panel.gameObject, "Status").text), Is.False,
                    "Receipt storage failure must be visible in the public support panel.");
                Assert.That(Find<Button>(f.Panel.gameObject, "Send").interactable, Is.False);
                Assert.That(f.Wire.Requests.Any(r => r.EndpointPath.EndsWith("/receipt", StringComparison.Ordinal)), Is.False);
                Assert.That(f.Journal.Puts, Is.Zero);
            }
        }

        [Test]
        public async Task ClosingLobbyInvalidatesPendingReadEvenForSameAccount()
        {
            using (var f = new Fixture())
            {
                f.Journal.Records.Add(Record(PendingReceiptKind.Support, SupportKey));
                await f.Login();
                var delay = new TaskCompletionSource<bool>(); f.Journal.NextRead = delay;
                f.Open(); f.Lobby.SetActive(false);
                delay.SetResult(true); await Settle();
                Assert.That(f.Wire.Requests.Any(r => r.EndpointPath.EndsWith("/receipt", StringComparison.Ordinal)), Is.False);
                Assert.That(f.Panel == null || !f.Panel.gameObject.activeSelf, Is.True);
            }
        }

        [Test]
        public async Task SameAccountReloginAutomaticallyRestoresInProcessUncertainSubmissionAsReceiptOnly()
        {
            using (var f = new Fixture())
            {
                await f.Login(); f.Open(); await Settle();
                var originalRecovery = RecoveryProperty().GetValue(f.Runtime);
                var content = Find<TMP_InputField>(f.Panel.gameObject, "Content");
                content.text = "Synthetic uncertain inquiry before same-account relogin";
                Click(f.Panel.gameObject, "Send"); await Settle();
                var post = f.Wire.Requests.Single(r => r.Method == "POST" && r.EndpointPath == "/v1/support/tickets");
                var key = post.Headers.Single(h => h.Name == "Idempotency-Key").Value;
                Assert.That(f.Journal.Records.Single().IdempotencyKey, Is.EqualTo(key));
                Assert.That(Find<Button>(f.Panel.gameObject, "Reconcile").interactable, Is.True);
                var generation = f.Runtime.Owner.Generation;
                f.Runtime.InvalidateAuthentication();
                Assert.That(RecoveryProperty().GetValue(f.Runtime), Is.Null);
                await f.Login();
                Assert.That(f.Runtime.Owner.Generation, Is.GreaterThan(generation));
                var reads = f.Journal.Reads.Count;
                f.Open(); await Settle();
                Assert.That(RecoveryProperty().GetValue(f.Runtime), Is.Not.SameAs(originalRecovery));
                Assert.That(f.Journal.Reads.Count, Is.GreaterThan(reads));
                // No manual SelectSupport: opening the new owner UI must restore the saved key.
                Assert.That(Find<TMP_InputField>(f.Panel.gameObject, "Content").text, Is.Empty);
                Assert.That(Find<Button>(f.Panel.gameObject, "Send").interactable, Is.False);
                Click(f.Panel.gameObject, "Reconcile"); await Settle();
                var gets = f.Wire.Requests.Where(r => r.EndpointPath == "/v1/support/tickets/receipt").ToArray();
                Assert.That(gets.Length, Is.EqualTo(1)); AssertLookup(gets[0], key);
                Assert.That(f.Wire.Requests.Count(r => r.Method == "POST" && r.EndpointPath != "/v1/recurring/sync"), Is.EqualTo(1), "Relogin must not replay the old body.");
                Assert.That(f.Journal.Puts, Is.EqualTo(1));
                Assert.That(f.Journal.Records.Single().IdempotencyKey, Is.EqualTo(key), "404 retains the original metadata.");
            }
        }

        private static PendingReceipt Record(PendingReceiptKind kind, string key, string account = "account_test") =>
            new PendingReceipt(kind, account, key, kind == PendingReceiptKind.Support ? "BUG_OTHER" : "UNITY_ERROR",
                new AppBuildSnapshot("1.2.3", 123), DateTimeOffset.Parse("2026-09-06T00:00:00Z"), DateTimeOffset.Parse("2026-09-06T00:00:00Z"));
        private static ConstructorInfo Constructor()
        {
            var constructor = typeof(PlayerEntryRuntime).GetConstructor(new[] { typeof(ResourceBootstrapController), typeof(TMP_FontAsset),
                typeof(IRunApiHttpTransport), typeof(IGameApiAuthTokenStore), typeof(IGameApiAuthTokenReader), typeof(Func<AppBuildSnapshot>), typeof(IPendingReceiptJournal) });
            Assert.That(constructor, Is.Not.Null, "RED: missing seven-argument runtime receipt composition."); return constructor;
        }
        private static PropertyInfo RecoveryProperty()
        {
            var property = typeof(PlayerEntryRuntime).GetProperty("ReceiptRecovery");
            Assert.That(property, Is.Not.Null, "RED: missing public lazy ReceiptRecovery property."); return property;
        }
        private static async Task Settle() { for (var i = 0; i < 30; i++) await Task.Yield(); }
        private static void SelectSupport(Fixture fixture)
        {
            var recovery = RecoveryProperty().GetValue(fixture.Runtime);
            Assert.That(recovery, Is.Not.Null);
            var records = (IEnumerable<PendingReceipt>)recovery.GetType().GetProperty("Records").GetValue(recovery);
            var selected = records.Single(r => r.Kind == PendingReceiptKind.Support && r.IdempotencyKey == SupportKey);
            recovery.GetType().GetMethod("Select", new[] { typeof(PendingReceipt) }).Invoke(recovery, new object[] { selected });
            Assert.That(recovery.GetType().GetProperty("Selected").GetValue(recovery), Is.SameAs(selected));
        }
        private static void AssertLookup(RunApiHttpRequest request, string key)
        {
            Assert.That(request.Method, Is.EqualTo("GET")); Assert.That(request.BodyJson, Is.Null);
            Assert.That(request.Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo(key));
        }
        private static T Find<T>(GameObject root, string name) where T : Component => root.GetComponentsInChildren<T>(true).Single(c => c.name == name);
        private static void Click(GameObject root, string name) => Find<Button>(root, name).onClick.Invoke();
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
