using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LastRail.Tests.EditMode
{
    public sealed class DepartureExpiryViewTests
    {
        const string Operation = "opn_01_last_departure";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static JObject Quote() => JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/Departure549/server-quote.json"));
        sealed class Wire : IRunApiHttpTransport
        {
            public DateTimeOffset Expiry;
            public int Calls;
            readonly JObject prepared = JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/self-check-1Ys9pJ/prepare-r6.json"));
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Calls++;
                JObject body;
                switch (request.EndpointPath)
                {
                    case "/__local/ce03/support-loadout": body = new JObject { ["selection"] = prepared["commandContract"]["support"].DeepClone() }; break;
                    case "/__local/ce03/prepare": body = (JObject)prepared.DeepClone(); break;
                    case "/v1/operations/preview": body = new JObject { ["accepted"] = true, ["operationPreviewId"] = "fixture_quote", ["expiresAt"] = Expiry.ToString("O"), ["quote"] = new JObject { ["fuelCost"] = 1 } }; break;
                    case "/v1/trains/loadout": body = (JObject)typeof(TrainLoadoutTests).GetMethod("Snapshot", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { 1L }); break;
                    case "/v1/formation/ultimate": body = JObject.Parse("{\"accepted\":true,\"rulesVersion\":\"ultimate-selection-v1\",\"operatorId\":\"op_eve\",\"revision\":1,\"formationRevision\":1,\"selectionStatus\":\"SELECTED\",\"changeLocked\":false}"); break;
                    case "/v1/account/state":
                        body = JObject.Parse(AccountStateReadTests.Json());
                        var formation = (JObject)body["formation"];
                        formation["activationSupported"] = true; formation["revision"] = 1;
                        formation["activePresetId"] = "preset-a"; formation["activePresetRevision"] = 1;
                        formation["updatedAt"] = "2026-09-10T00:00:00.000Z";
                        formation["operators"] = Quote()["admission"]["formation"]["operators"].DeepClone();
                        break;
                    default: throw new InvalidOperationException("Unexpected request " + request.EndpointPath);
                }
                return Task.FromResult(RunApiHttpResponse.Success(200, body.ToString()));
            }
        }
        static LobbyCommandDeck Open(GameObject root)
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(1600, 900);
            var safe = new GameObject("SafeArea", typeof(RectTransform)); safe.transform.SetParent(root.transform, false);
            var rect = (RectTransform)safe.transform; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck = LobbyCommandDeck.Attach(root, font, "QA", 0, new MissionShellMissionRowState[0], null, null);
            typeof(LobbyCommandDeck).GetMethod("OpenDepartureQuote", Private).Invoke(deck, new object[] { Operation, "QA", 0 });
            return deck;
        }
        static Button QuoteRow(GameObject root) => root.GetComponentsInChildren<Button>(true).Single(b => b.name == "ResolveDepartureBlock_quote");
        static void Refresh(LobbyCommandDeck deck) => typeof(LobbyCommandDeck).GetMethod("UpdateDepartureDialog", Private).Invoke(deck, null);
        static AccountScreenRuntime State(Wire wire)
        {
            var session = SupportInquiryTests.Session();
            return new AccountScreenRuntime(wire, null, () => session, () => 1);
        }

        [UnityTest] public IEnumerator PublicQuoteExpiryRefreshesWithoutAccountEvent()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("PublicExpiry", typeof(RectTransform), typeof(Canvas));
            var wire = new Wire();
            var state = State(wire);
            try
            {
                foreach (var load in new Func<Task>[] { state.RefreshAccountAsync, state.RefreshTrainLoadoutAsync, state.RefreshUltimateAsync })
                {
                    var read = load();
                    while (!read.IsCompleted) yield return null;
                    read.GetAwaiter().GetResult();
                }
                var deck = Open(root);
                typeof(LobbyCommandDeck).GetField("accountState", Private).SetValue(deck, state);
                var json = Quote(); json["expiresAt"] = DateTimeOffset.UtcNow.AddSeconds(.8).UtcDateTime.ToString("O");
                var request = "{\"operationId\":\"opn_01_last_departure\",\"risk\":\"I\",\"expectedFormationRevision\":1,\"expectedLoadoutRevision\":1,\"expectedUltimateRevision\":1}";
                typeof(AccountScreenRuntime).GetProperty("Departure").SetValue(state, new DepartureQuote(json.ToString(), request));
                Assert.That(state.DepartureSelectionNotice, Is.Null);
                Refresh(deck);
                var row = QuoteRow(root); Assert.That(row.gameObject.activeSelf, Is.False);
                int calls = wire.Calls;
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(row.gameObject.activeSelf, Is.True, "Expiry must update the displayed quote block without a model event");
                Assert.That(wire.Calls, Is.EqualTo(calls), "Expiry is display-only");
            }
            finally { Object.DestroyImmediate(root); state.Dispose(); }
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator LocalQuoteExpiryAndUnbindingRefreshWithoutPolling()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("LocalExpiry", typeof(RectTransform), typeof(Canvas));
            var wire = new Wire();
            var client = new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"), "a", wire, new GameApiAccessToken("lst1.test"), raw => { });
            var flow = new Ce03LocalDeparture(client, _ => true, (_, __) => { }, _ => Assert.Fail("No start"));
            try
            {
                var deck = Open(root); deck.BindLocalDeparture(flow);
                wire.Expiry = DateTimeOffset.UtcNow.AddSeconds(.8);
                var preview = flow.PreviewAsync(Operation, "I", 1, 1, 1);
                while (!preview.IsCompleted) yield return null;
                preview.GetAwaiter().GetResult();
                Assert.That(flow.CanStart, Is.True);
                var row = QuoteRow(root); Assert.That(row.gameObject.activeSelf, Is.False);
                int calls = wire.Calls;
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(row.gameObject.activeSelf, Is.True);
                Assert.That(wire.Calls, Is.EqualTo(calls));
                wire.Expiry = DateTimeOffset.UtcNow.AddSeconds(10);
                preview = flow.PreviewAsync(Operation, "I", 1, 1, 1);
                while (!preview.IsCompleted) yield return null;
                preview.GetAwaiter().GetResult();
                Assert.That(row.gameObject.activeSelf, Is.False);
                deck.BindLocalDeparture(null);
                Assert.That(row.gameObject.activeSelf, Is.True, "Unbinding must immediately stop showing the former owner's quote");
                deck.BindLocalDeparture(flow);
                wire.Expiry = DateTimeOffset.UtcNow.AddSeconds(.3);
                preview = flow.PreviewAsync(Operation, "I", 1, 1, 1);
                while (!preview.IsCompleted) yield return null;
                preview.GetAwaiter().GetResult();
                Assert.That(row.gameObject.activeSelf, Is.False);
                deck.enabled = false;
                yield return new WaitForSecondsRealtime(.6f);
                Assert.That(row.gameObject.activeSelf, Is.False, "Disabled presentation cannot receive the old deadline callback");
                deck.enabled = true;
                Assert.That(deck.IsDialogOpen, Is.False, "Reenable discards the old preparation view");
            }
            finally { Object.DestroyImmediate(root); flow.Dispose(); }
            yield return new ExitPlayMode();
        }
    }
}
