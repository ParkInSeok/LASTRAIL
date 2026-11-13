using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace LastRail.Tests.EditMode
{
    public sealed class PassClaimAllTests
    {
        sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest, Task<RunApiHttpResponse>> Reply;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { Requests.Add(request); return Reply == null ? Task.FromResult(RunApiHttpResponse.NetworkError("local unavailable")) : Reply(request); }
        }

        sealed class Journal : IAccountActionJournal
        {
            public PendingAccountAction Row;
            public PendingAccountAction Read(string account) => Row?.Account == account ? Row : null;
            public bool Save(PendingAccountAction action) { Row = action; return true; }
            public bool Remove(PendingAccountAction action) { if (!ReferenceEquals(Row, action)) return false; Row = null; return true; }
        }

        static JObject Sample(string name) => JObject.Parse(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
            "../QA/Unity1Artifacts/Reports/Server2-PassBulkSamples-20260914/" + name + ".json")));
        static GameApiAuthSessionSnapshot Session() => new GameApiAuthSessionSnapshot("a", "ACTIVE", new GameApiAccessToken("lst1.fixture"), DateTime.UtcNow.AddMinutes(5), new GameApiRefreshToken("fixture"), DateTime.UtcNow.AddMinutes(10));
        static string Key(RunApiHttpRequest request) => request.Headers.Single(h => h.Name == "Idempotency-Key").Value;
        static RunApiHttpRequest[] Posts(Wire wire) => wire.Requests.Where(r => r.Method == "POST").ToArray();

        static JObject Snapshot(bool premium, bool allFifty = false)
        {
            var state = Sample(premium ? "recurring-state-premium-owned" : "recurring-state-premium-required");
            if (allFifty)
            {
                // Explicit derived mock state for the 50-per-track boundary; producer originals stay unchanged.
                var policy = state["policies"].Single(p => (string)p["policyId"] == "pass-sep");
                policy["xp"] = policy["earnedXp"] = 5000; policy["level"] = 50;
                foreach (var row in policy["rewardRows"])
                { row["freeState"] = row["premiumState"] = "EARNED"; row["cosmeticSlot"] = null; row["cosmeticItemId"] = null; }
            }
            return state;
        }

        static JObject ReplyFor(RunApiHttpRequest request, bool partial = false)
        {
            var body = JObject.Parse(request.BodyJson);
            var track = (string)body["track"];
            if (((JArray)body["levels"]).Count == 50 && track == "FREE")
                return Sample(partial ? "bulk-free50-partial" : "bulk-free50-complete");
            var response = Sample(track == "FREE" ? "bulk-free-complete" : "bulk-premium-complete");
            var originals = (JArray)response["items"];
            var items = new JArray();
            foreach (var level in body["levels"])
            {
                var original = originals.FirstOrDefault(i => (int)i["action"]["level"] == (int)level);
                var item = original == null ? new JObject { ["action"] = new JObject { ["kind"] = "PASS_LEVEL", ["policyId"] = "pass-sep", ["track"] = track, ["level"] = (int)level }, ["status"] = "FAILED", ["receipt"] = null, ["errorCode"] = "pass_level_not_earned" } : (JObject)original.DeepClone();
                item["index"] = items.Count; items.Add(item);
            }
            // Adapt membership/counters only for derived local mock requests, retaining producer receipts.
            response["request"] = body; response["items"] = items; response["totalCount"] = items.Count;
            response["successCount"] = items.Count(i => (string)i["status"] == "SUCCESS" || (string)i["status"] == "REPLAYED");
            response["failureCount"] = items.Count(i => (string)i["status"] == "FAILED");
            response["pendingCount"] = 0; response["refreshRequired"] = (int)response["successCount"] > 0;
            return response;
        }

        static Task<RunApiHttpResponse> ReadReply(RunApiHttpRequest request, JObject snapshot) => Task.FromResult(
            request.EndpointPath == "/v1/recurring/state" ? RunApiHttpResponse.Success(200, snapshot.ToString()) : RunApiHttpResponse.NetworkError("outside local test scope"));

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public async Task OverallActionIncludesEligibleRowsAcrossTracksWithoutClaimedLockedOrMissingPremium(bool premium, int tracks)
        {
            var snapshot = Snapshot(premium); var wire = new Wire(); var journal = new Journal(); var session = Session();
            wire.Reply = request => request.Method == "POST" ? Task.FromResult(RunApiHttpResponse.Success(200, ReplyFor(request).ToString())) : ReadReply(request, snapshot);
            using var state = new AccountScreenRuntime(wire, journal, () => session, () => 1);
            await state.RefreshRecurringAsync(); Assert.That(state.RecurringReady, Is.True, state.RecurringError);
            await state.ClaimAllPassAsync("pass-sep");
            Assert.That(Posts(wire).Length, Is.EqualTo(tracks));
            foreach (var request in Posts(wire)) Assert.That(JObject.Parse(request.BodyJson)["levels"].Values<int>(), Is.EqualTo(new[] { 2 }));
            Assert.That(state.PassClaimAllItems.Count, Is.EqualTo(tracks));
            Assert.That(state.PassClaimAllItems.All(i => (string)i["status"] == "SUCCESS"), Is.True);
            Assert.That(state.Pending, Is.Null); Assert.That(state.Busy, Is.False); Assert.That(state.CanContinuePassClaimAll, Is.False);
        }

        [Test]
        public async Task FiftyPerTrackContinuesOriginalPartialRequestAndRetainsFailuresFromBothTracks()
        {
            var snapshot = Snapshot(true, true); var wire = new Wire(); var journal = new Journal(); var session = Session(); int writes = 0;
            wire.Reply = request => request.Method == "POST" ? Task.FromResult(RunApiHttpResponse.Success(200, ReplyFor(request, ++writes == 1).ToString())) : ReadReply(request, snapshot);
            using var state = new AccountScreenRuntime(wire, journal, () => session, () => 1);
            await state.RefreshRecurringAsync(); Assert.That(state.RecurringReady, Is.True, state.RecurringError);
            await state.ClaimAllPassAsync("pass-sep");
            var posts = Posts(wire); Assert.That(posts.Length, Is.EqualTo(3));
            Assert.That(posts[0].BodyJson, Is.EqualTo(posts[1].BodyJson)); Assert.That(Key(posts[0]), Is.EqualTo(Key(posts[1])));
            Assert.That(JObject.Parse(posts[2].BodyJson)["track"].Value<string>(), Is.EqualTo("PREMIUM"));
            Assert.That(posts.All(p => JObject.Parse(p.BodyJson)["levels"].Count() == 50), Is.True);
            Assert.That(state.PassClaimAllItems.Count, Is.EqualTo(100));
            Assert.That(state.PassClaimAllItems.Count(i => (string)i["status"] == "FAILED"), Is.EqualTo(96));
            Assert.That(state.PassClaimAllNotice, Does.Contain("실패 96"));
            Assert.That(state.Pending, Is.Null); Assert.That(state.CanContinuePassClaimAll, Is.False);
        }

        [Test]
        public async Task LostReplyPausesOtherTrackThenContinuesSameKeyAndOriginalMembership()
        {
            var snapshot = Snapshot(true); var wire = new Wire(); var journal = new Journal(); var session = Session(); int writes = 0;
            wire.Reply = request => request.Method == "POST" ? Task.FromResult(++writes == 1 ? RunApiHttpResponse.NetworkError("lost local response") : RunApiHttpResponse.Success(200, ReplyFor(request).ToString())) : ReadReply(request, snapshot);
            using var state = new AccountScreenRuntime(wire, journal, () => session, () => 1);
            await state.RefreshRecurringAsync(); await state.ClaimAllPassAsync("pass-sep");
            Assert.That(Posts(wire).Length, Is.EqualTo(1)); Assert.That(state.Pending, Is.Not.Null); Assert.That(state.CanContinuePassClaimAll, Is.True);
            var pending = journal.Row;
            await state.ContinuePassClaimAllAsync();
            var posts = Posts(wire); Assert.That(posts.Length, Is.EqualTo(3));
            Assert.That(posts[1].BodyJson, Is.EqualTo(pending.Payload)); Assert.That(Key(posts[1]), Is.EqualTo(pending.Key));
            Assert.That(state.PassClaimAllItems.Count, Is.EqualTo(2)); Assert.That(journal.Row, Is.Null);
        }

        [Test]
        public async Task DuplicateActionIsBlockedAndOwnerChangeStopsRemainingTrackWithoutConsumingOldJournal()
        {
            var snapshot = Snapshot(true); var wire = new Wire(); var journal = new Journal(); var session = Session(); long generation = 1;
            var reply = new TaskCompletionSource<RunApiHttpResponse>();
            wire.Reply = request => request.Method == "POST" ? reply.Task : ReadReply(request, snapshot);
            using var state = new AccountScreenRuntime(wire, journal, () => session, () => generation);
            await state.RefreshRecurringAsync(); var operation = state.ClaimAllPassAsync("pass-sep");
            await state.ClaimAllPassAsync("pass-sep"); Assert.That(Posts(wire).Length, Is.EqualTo(1)); Assert.That(state.Busy, Is.True);
            var pending = journal.Row; generation++; state.SynchronizeSession();
            reply.SetResult(RunApiHttpResponse.Success(200, ReplyFor(Posts(wire)[0]).ToString())); await operation;
            Assert.That(journal.Row, Is.SameAs(pending)); Assert.That(Posts(wire).Length, Is.EqualTo(1));
            Assert.That(state.HasPassClaimAllResult, Is.False); Assert.That(state.PassClaimAllItems, Is.Empty);
        }

        [Test]
        public async Task ActualPassButtonRunsBothTracksAndShowsCombinedFailureResults()
        {
            var snapshot = Snapshot(true, true); var wire = new Wire(); var journal = new Journal(); var session = Session();
            wire.Reply = request => request.EndpointPath == "/v1/recurring/bulk-claims" ? Task.FromResult(RunApiHttpResponse.Success(200, ReplyFor(request).ToString()))
                : request.EndpointPath == "/v1/recurring/sync" ? Task.FromResult(RunApiHttpResponse.Success(200, "{\"accepted\":true,\"addedObjectives\":0,\"lastEventId\":0}")) : ReadReply(request, snapshot);
            using var state = new AccountScreenRuntime(wire, journal, () => session, () => 1);
            await state.RefreshRecurringAsync();
            var root = new UnityEngine.GameObject("PassAllActualUi", typeof(UnityEngine.RectTransform));
            ((UnityEngine.RectTransform)root.transform).sizeDelta = new UnityEngine.Vector2(720, 1280);
            var safe = new UnityEngine.GameObject("SafeArea", typeof(UnityEngine.RectTransform)); safe.transform.SetParent(root.transform, false);
            var safeRect = (UnityEngine.RectTransform)safe.transform; safeRect.anchorMin = UnityEngine.Vector2.zero; safeRect.anchorMax = UnityEngine.Vector2.one; safeRect.sizeDelta = UnityEngine.Vector2.zero;
            try
            {
                var deck = LobbyCommandDeck.Attach(root, UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"), "LOCAL QA", 1, new MissionShellMissionRowState[0], null, null);
                deck.BindAccount(state); deck.OpenPass();
                while (state.Busy || state.RecurringLoading) await Task.Yield();
                var button = root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "PassBulk");
                Assert.That(button.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("전체 수령")); Assert.That(button.interactable, Is.True);
                Capture(root, "gallery26-pass-all-before-20260914.png");
                button.onClick.Invoke(); while (state.Busy || state.RecurringLoading) await Task.Yield();
                Assert.That(wire.Requests.Count(r => r.EndpointPath == "/v1/recurring/bulk-claims"), Is.EqualTo(2));
                Assert.That(root.GetComponentsInChildren<TMPro.TMP_Text>().Single(t => t.name == "BulkSummary").text, Does.Contain("실패 96"));
                Assert.That(root.GetComponentsInChildren<TMPro.TMP_Text>().Single(t => t.name == "BulkPage").text, Is.EqualTo("1 / 20"));
                Capture(root, "gallery26-pass-all-result-20260914.png");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void Capture(UnityEngine.GameObject root, string name) => typeof(AccountScreenIntegrationTests)
            .GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { root, name });

        static async Task Claim(AccountScreenRuntime state)
        {
            var method = typeof(AccountScreenRuntime).GetMethod("ClaimAllPassAsync", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "The pass action needs an actual overall claim use case.");
            await (Task)method.Invoke(state, new object[] { "fixture-pass" });
        }

        [Test]
        public async Task MissingAuthoritativeSnapshotCannotIssueAnyClaim()
        {
            var wire = new Wire();
            var session = new GameApiAuthSessionSnapshot("account_test", "ACTIVE", new GameApiAccessToken("lst1.fixture"), DateTime.UtcNow.AddMinutes(5), new GameApiRefreshToken("fixture"), DateTime.UtcNow.AddMinutes(10));
            using var state = new AccountScreenRuntime(wire, null, () => session, () => 1);
            await Claim(state);
            Assert.That(wire.Requests, Is.Empty);
            Assert.That(state.Busy, Is.False);
        }

        [Test]
        public async Task MissingSessionCannotIssueAnyClaim()
        {
            var wire = new Wire();
            using var state = new AccountScreenRuntime(wire, null, () => null, () => 1);
            await Claim(state);
            Assert.That(wire.Requests, Is.Empty);
        }
    }
}
