#if UNITY_EDITOR
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
using LastRail.Support;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class SupportDiagnosticsLocalHttpTests
    {
        private const string Origin = "http://127.0.0.1:4174";
        private const long VersionCode = 42061;

        [Test]
        public void RuntimeExposesBuildSnapshotAndCurrentSessionDiagnosticContracts()
        {
            RuntimeConstructor();
            var diagnostics = typeof(PlayerEntryRuntime).GetProperty("Diagnostics");
            Assert.That(diagnostics, Is.Not.Null, "RED: public PlayerEntryRuntime.Diagnostics is required.");
            Assert.That(diagnostics.PropertyType.Name, Is.EqualTo("CurrentSessionErrorRuntime"));
            Assert.That(diagnostics.GetSetMethod(), Is.Null);
            Assert.That(diagnostics.PropertyType.GetMethod("Record", new[] { typeof(SafeClientErrorKind), typeof(long) })?.ReturnType, Is.EqualTo(typeof(bool)));
            var flush = diagnostics.PropertyType.GetMethod("FlushAsync", Type.EmptyTypes);
            Assert.That(flush, Is.Not.Null);
            Assert.That(typeof(Task).IsAssignableFrom(flush.ReturnType), Is.True);
            Assert.That(diagnostics.PropertyType.GetProperty("PendingCount")?.PropertyType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public async Task PublicSupportReceiptAndHydrationErrorReachActualLocalAdmin()
        {
            if (Environment.GetEnvironmentVariable("LASTRAIL_SUPPORT_HTTP_QA") != "1")
                Assert.Ignore("Explicit parent-owned local Hono/sql.js harness on 127.0.0.1:4174 required.");
            RuntimeExposesBuildSnapshotAndCurrentSessionDiagnosticContracts();
            var version = "1.0.0-support-" + Guid.NewGuid().ToString("N");
            using (var fixture = new Fixture(new AppBuildSnapshot(version, VersionCode)))
            {
                var runtime = fixture.Runtime;
                await runtime.Owner.RestoreAsync();
                fixture.Wire.MalformNextProfileResponse = true;
                Click(fixture.Root, "Guest");
                await Until(() => runtime.Owner.Session != null && !runtime.Owner.Busy,
                    "Actual guest/profile HTTP did not complete the failed hydration.");
                var account = runtime.Owner.Session.AccountId;
                Assert.That(account, Is.EqualTo(fixture.Wire.GuestAccount));
                Assert.That(fixture.Wire.GuestPosts, Is.EqualTo(1));
                Assert.That(fixture.Wire.MalformedAfterRealProfile, Is.True);
                Assert.That(runtime.Owner.Stage, Is.EqualTo(PlayerAuthStage.Recovery));
                var generation = runtime.Owner.Generation;
                await AssertActualDiagnostic(fixture, version, account);
                Click(fixture.Root, "Retry");
                await Until(() => Find<TMP_InputField>(fixture.Root, "Nickname").gameObject.activeSelf,
                    "Public retry did not reach the actual nickname action.");
                Find<TMP_InputField>(fixture.Root, "Nickname").text = "qa" + Guid.NewGuid().ToString("N").Substring(0, 10);
                Click(fixture.Root, "SubmitNickname");
                await Until(() => !runtime.Owner.Busy, "Actual profile/story hydration did not complete.");
                Assert.That(runtime.Owner.Ready, Is.True);
                Assert.That(runtime.Owner.Generation, Is.EqualTo(generation));
                Assert.That(runtime.Owner.Session.AccountId, Is.EqualTo(account));
                Assert.That(runtime.LobbySnapshot.Profile.AccountId, Is.EqualTo(account));
                Assert.That(fixture.Wire.ProfileReads, Is.GreaterThan(0));
                Assert.That(fixture.Wire.StoryReads, Is.GreaterThan(0));
                Assert.That(runtime.BindLobby(fixture.Lobby), Is.True);
                Click(fixture.Lobby, "SupportInquiry");
                var panel = fixture.Root.GetComponentInChildren<SupportInquiryPanel>(true);
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.gameObject.activeSelf, Is.True);
                var model = (SupportInquiryController)typeof(SupportInquiryPanel).GetField("model",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
                var body = "LOCAL Unity support proof <b>plain text</b> " + Guid.NewGuid().ToString("N");
                Find<TMP_Dropdown>(panel.gameObject, "Category").value = SupportInquiryHttpClient.Categories.ToList().IndexOf("BUG_OTHER");
                Find<TMP_InputField>(panel.gameObject, "Content").text = body;
                fixture.Wire.LoseSupportResponse = true;
                Click(panel.gameObject, "Send");
                await Until(() => model.State == InquiryState.Uncertain, "A real received support POST must become uncertain only after response interception.");
                Assert.That(fixture.Wire.SupportPosts, Is.EqualTo(1));
                Assert.That(fixture.Wire.SupportReceived, Is.Not.Null);
                Assert.That((bool)fixture.Wire.SupportReceived["accepted"], Is.True);
                Assert.That((string)fixture.Wire.SupportSubmitted["appVersion"], Is.EqualTo(version));
                Assert.That((string)fixture.Wire.SupportSubmitted["appVersionCode"], Is.EqualTo(VersionCode.ToString()));
                Click(panel.gameObject, "Reconcile");
                await Until(() => model.State == InquiryState.Received, "Actual receipt GET did not reconcile the public panel.");
                Assert.That(fixture.Wire.ReceiptGets, Is.EqualTo(1));
                Assert.That(fixture.Wire.SupportPosts, Is.EqualTo(1), "Reconcile must not repost support.");
                var ticket = model.Receipt.TicketId;
                Assert.That(ticket, Is.EqualTo((string)fixture.Wire.SupportReceived["ticketId"]));
                var tickets = (JArray)(await Admin("/api/support/tickets?accountId=" + Uri.EscapeDataString(account)))["items"];
                Assert.That(tickets.Count, Is.EqualTo(1));
                Assert.That((string)tickets[0]["ticketId"], Is.EqualTo(ticket));
                var detail = (await Admin("/api/support/tickets/" + Uri.EscapeDataString(ticket)))["ticket"];
                Assert.That((string)detail["accountId"], Is.EqualTo(account));
                Assert.That((string)detail["body"], Is.EqualTo(body));
                Assert.That((string)detail["appVersion"], Is.EqualTo(version));
                Assert.That((string)detail["appVersionCode"], Is.EqualTo(VersionCode.ToString()));

                TestContext.WriteLine("LOCAL_HTTP_SUPPORT_DIAGNOSTICS account=" + account + " ticket=" + ticket
                    + " appVersion=" + version + " appVersionCode=" + VersionCode
                    + " support=actualPOST+lostResponse+receiptGET error=actualProfileGET+clientMalformedBody+runtimeProducer+actualPOST admin=actualGET");
            }
        }

        private static async Task AssertActualDiagnostic(Fixture fixture, string version, string account)
        {
                var diagnostics = typeof(PlayerEntryRuntime).GetProperty("Diagnostics").GetValue(fixture.Runtime);
                Assert.That(diagnostics, Is.Not.Null);
                Assert.That((int)diagnostics.GetType().GetProperty("PendingCount").GetValue(diagnostics)
                    + fixture.Wire.ErrorPosts.Count, Is.GreaterThan(0), "The actual profile failure producer must record the current-generation error.");
                await (Task)diagnostics.GetType().GetMethod("FlushAsync").Invoke(diagnostics, null);
                Assert.That((int)diagnostics.GetType().GetProperty("PendingCount").GetValue(diagnostics), Is.Zero);
                Assert.That(fixture.Wire.ErrorPosts.Count, Is.EqualTo(1));
                Assert.That(fixture.Wire.ErrorReceipts.Count, Is.EqualTo(1));
                Assert.That((bool)fixture.Wire.ErrorReceipts[0]["accepted"], Is.True);
                var errorEvent = fixture.Wire.ErrorPosts[0]["events"][0];
                Assert.That((string)errorEvent["kind"], Is.EqualTo("UNITY_ERROR"));
                Assert.That((string)errorEvent["scope"], Is.EqualTo("AUTHENTICATED_CURRENT_SESSION"));
                Assert.That((string)errorEvent["appVersion"], Is.EqualTo(version));
                Assert.That((string)errorEvent["appVersionCode"], Is.EqualTo(VersionCode.ToString()));
                var query = "/api/ops/error-logs?category=frontend&appVersion=" + Uri.EscapeDataString(version) + "&appVersionCode=";
                var logs = (JArray)(await Admin(query + VersionCode))["items"];
                Assert.That(logs.Count, Is.EqualTo(1), "Unique version excludes all harness-seeded pagination rows.");
                Assert.That((string)logs[0]["accountId"], Is.EqualTo(account));
                Assert.That((string)logs[0]["errorCode"], Is.EqualTo("frontend_unity_error"));
                Assert.That((string)logs[0]["appVersion"], Is.EqualTo(version));
                Assert.That((string)logs[0]["appVersionCode"], Is.EqualTo(VersionCode.ToString()));
                Assert.That(((JArray)(await Admin(query + (VersionCode + 1)))["items"]).Count, Is.Zero);
                Assert.That(((JArray)(await Admin("/api/ops/error-logs?category=frontend&appVersion="
                    + Uri.EscapeDataString(version + "-absent") + "&appVersionCode=" + VersionCode))["items"]).Count, Is.Zero);
        }

        private sealed class Store : IGameApiAuthTokenStore, IGameApiAuthTokenReader
        {
            private GameApiAuthSessionSnapshot session;
            public Task<GameApiAuthSessionSnapshot> LoadAsync() => Task.FromResult(session);
            public Task<GameApiAuthTokenReadResult> ReadAsync() => Task.FromResult(new GameApiAuthTokenReadResult(
                session == null ? GameApiAuthTokenReadOutcome.Missing : GameApiAuthTokenReadOutcome.Loaded, session));
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value)
            { session = value; return Task.FromResult(GameApiAuthTokenStoreResult.Saved); }
        }

        private sealed class ObservedWire : IRunApiHttpTransport
        {
            private readonly IRunApiHttpTransport actual = NewWire();
            internal bool LoseSupportResponse, MalformNextProfileResponse, MalformedAfterRealProfile;
            internal string GuestAccount;
            internal int GuestPosts, ProfileReads, StoryReads, SupportPosts, ReceiptGets;
            internal JObject SupportSubmitted, SupportReceived;
            internal readonly List<JObject> ErrorPosts = new List<JObject>();
            internal readonly List<JObject> ErrorReceipts = new List<JObject>();
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                var allowed = new[] { "/v1/auth/guest", "/v1/profile", "/v1/profile/display-name", "/v1/story-progress",
                    "/v1/support/tickets", "/v1/support/tickets/receipt", "/v1/client-errors" };
                if (!allowed.Contains(request.EndpointPath)) throw new InvalidOperationException("Unexpected local QA route.");
                var response = await actual.SendAsync(request);
                Assert.That(response.IsNetworkError, Is.False, request.EndpointPath);
                Assert.That(response.StatusCode, Is.EqualTo(200), request.EndpointPath);
                var json = JObject.Parse(response.BodyJson);
                if (request.EndpointPath == "/v1/auth/guest") { GuestPosts++; GuestAccount = (string)json["account"]["id"]; }
                if (request.EndpointPath == "/v1/story-progress") StoryReads++;
                if (request.EndpointPath == "/v1/profile")
                {
                    ProfileReads++;
                    if (MalformNextProfileResponse)
                    {
                        Assert.That((string)json["account"]["id"], Is.EqualTo(GuestAccount));
                        MalformNextProfileResponse = false; MalformedAfterRealProfile = true;
                        return RunApiHttpResponse.Success(200, "{malformed-after-actual-local-response");
                    }
                }
                if (request.EndpointPath == "/v1/support/tickets" && request.Method == "POST")
                {
                    SupportPosts++; SupportSubmitted = JObject.Parse(request.BodyJson); SupportReceived = json;
                    if (LoseSupportResponse) { LoseSupportResponse = false; return RunApiHttpResponse.NetworkError("Synthetic loss AFTER actual server response"); }
                }
                if (request.EndpointPath == "/v1/support/tickets/receipt") ReceiptGets++;
                if (request.EndpointPath == "/v1/client-errors")
                { ErrorPosts.Add(JObject.Parse(request.BodyJson)); ErrorReceipts.Add(json); }
                return response;
            }
        }

        private sealed class Fixture : IDisposable
        {
            internal readonly GameObject Root, Startup, Lobby;
            internal readonly ObservedWire Wire = new ObservedWire();
            internal readonly PlayerEntryRuntime Runtime;
            internal Fixture(AppBuildSnapshot build)
            {
                var constructor = RuntimeConstructor();
                Root = new GameObject("InactiveSupportHttpBootstrap"); Root.SetActive(false);
                var bootstrap = Root.AddComponent<ResourceBootstrapController>();
                Startup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
                Lobby = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab"));
                Set(bootstrap, "startup", Startup.GetComponent<StartupScreenRootBehaviour>());
                Set(bootstrap, "access", new DownloadAccessPolicy());
                Set(bootstrap, "entry", new AuthenticatedScreenEntry(_ => Task.CompletedTask, () => { }));
                var store = new Store();
                Runtime = (PlayerEntryRuntime)constructor.Invoke(new object[] { bootstrap,
                    AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"), Wire, store, store, (Func<AppBuildSnapshot>)(() => build) });
            }
            public void Dispose()
            {
                Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(Lobby);
                UnityEngine.Object.DestroyImmediate(Startup); UnityEngine.Object.DestroyImmediate(Root);
            }
        }

        private static ConstructorInfo RuntimeConstructor()
        {
            var constructor = typeof(PlayerEntryRuntime).GetConstructor(new[] { typeof(ResourceBootstrapController), typeof(TMP_FontAsset),
                typeof(IRunApiHttpTransport), typeof(IGameApiAuthTokenStore), typeof(IGameApiAuthTokenReader), typeof(Func<AppBuildSnapshot>) });
            Assert.That(constructor, Is.Not.Null, "RED: add Func<AppBuildSnapshot> capture overload; preserve five-argument constructor and feed support AND diagnostics.");
            return constructor;
        }
        private static IRunApiHttpTransport NewWire() => new UnityWebRequestRunApiHttpTransport(new UnityWebRequestRunApiHttpAdapter(Origin, 10));
        private static async Task<JObject> Admin(string path)
        {
            Assert.That(path.StartsWith("/api/", StringComparison.Ordinal), Is.True);
            var response = await NewWire().SendAsync(new RunApiHttpRequest("GET", path, null, null));
            Assert.That(response.IsNetworkError, Is.False); Assert.That(response.StatusCode, Is.EqualTo(200));
            return JObject.Parse(response.BodyJson);
        }
        private static async Task Until(Func<bool> done, string message)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (!done() && DateTime.UtcNow < deadline) await Task.Delay(20);
            Assert.That(done(), Is.True, message);
        }
        private static T Find<T>(GameObject root, string name) where T : Component =>
            root.GetComponentsInChildren<T>(true).Single(component => component.name == name);
        private static void Click(GameObject root, string name)
        {
            var button = Find<Button>(root, name);
            Assert.That(button.interactable, Is.True, name); button.onClick.Invoke();
        }
        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
#endif
