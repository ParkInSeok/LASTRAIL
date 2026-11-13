#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Presentation.Audio;
using LastRail.Presentation.MissionShell;
using LastRail.Presentation.Settings;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LastRail.Diagnostics
{
    public static class LobbyDepartureMusicPlayerCheck
    {
        const string NodeFixture = "D:/Project/LASTRAIL/QA/CommanderS1-20260922/HpOwner839/node-source.json";

        static void Require(bool ok, string reason)
        {
            if (!ok) throw new InvalidOperationException(reason);
        }

        static async Task Until(Func<bool> ready, double timeout)
        {
            var timer = Stopwatch.StartNew();
            while (!ready())
            {
                if (timer.Elapsed.TotalSeconds > timeout) throw new TimeoutException("DSP observation timed out.");
                await Task.Yield();
            }
        }

        static CombatSceneDriver CreateDriver()
        {
            var fixture = JObject.Parse(File.ReadAllText(NodeFixture));
            Require((string)fixture["scope"] == "ACTUAL_NODE_FACTORY_SYNTHETIC_ARRIVAL_NOT_LIVE_ENTRY", "Unexpected QA node scope.");
            var packet = (JObject)fixture["binding"];
            var route = BRouteBinding.Read(packet.ToString(), (string)packet["packetHash"], (string)packet["accountId"], (string)packet["runId"]);
            var request = BRouteCommand.FromOriginal(route, (JObject)fixture["command"]);
            var arrival = BRouteCommandResult.Read(200, fixture["response"].ToString(), request);
            var original = OrdinaryFiveStartInput.Read((JObject)fixture["originalStart"], route.AccountId, route.RunId);
            var factory = OrdinaryFiveCombatFactory.FromNodeStart(original, arrival.NodeStart, arrival.Scaling, arrival.NodeStart.ReadActions(original).StepTimeSpanTicks);
            var attempt = factory.Attempt;
            // Only a synthetic stationary, waiting gunner is needed to publish the first native step.
            var waiting = new OrdinaryNodeGunnerSources((id, tick) => new JObject {
                ["tick"] = tick, ["revision"] = tick + 1, ["nodeId"] = attempt.NodeId, ["snapshotHash"] = attempt.SnapshotHash,
                ["attemptGeneration"] = attempt.Generation, ["targets"] = new JArray(), ["noTargetStatus"] = "WAITING_FOR_RETURN",
                ["source"] = new JObject { ["id"] = id, ["generation"] = 1, ["executorId"] = id + ":weapon",
                    ["muzzleMm"] = new JArray(0, 0, 0), ["motionStatus"] = "STATIONARY" }
            }, (_, __, ___) => throw new InvalidOperationException("Unexpected QA attack."), (_, __) => throw new InvalidOperationException("Unexpected QA defense."),
               (_, __) => throw new InvalidOperationException("Unexpected QA interception."));
            return CombatSceneDriver.ForOrdinaryNode(factory.WithGunnerSources(waiting), () => true);
        }

        public static async Task Run(string output, GameObject prefab, TMP_FontAsset font)
        {
            Application.runInBackground = true;
            var rows = new JArray();
            var report = new JObject {
                ["scope"] = "WINDOWS_PLAYER_REAL_LOBBY_AND_DEPARTURE_CONTROLS_REAL_AUDIO_SYNTHETIC_ORDINARY_NODE_FIRST_COMMIT",
                ["fullFiveLiveCombat"] = false, ["liveApi"] = false, ["audition"] = false, ["nodeFixture"] = NodeFixture
            };
            var root = new GameObject("BgmPlayerCheck");
            report["listenersBefore"] = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled);
            GameObject screen = null;
            CombatSceneDriver driver = null;
            LobbyDepartureMusic owner = null;
            try
            {
                screen = Object.Instantiate(prefab);
                var battle = screen.GetComponentInChildren<MissionShellScreenRootBehaviour>(true).BattleMusicClip;
                Require(battle != null && battle.name == "bgm_act1_rail_loop", "Mission shell battle binding missing.");
                var mission = new MissionShellMissionRowState(DepartureContract.Operations[0], "QA 첫 출발", RunEncounterType.Ordinary15Wave, true, "qa.start", "qa.loading", false);
                var deck = LobbyCommandDeck.Attach(screen, font, "QA", 0, new[] { mission }, null, null);
                owner = new LobbyDepartureMusic(root.transform, battle);
                owner.EnterLobby();
                var voice = root.GetComponentsInChildren<AudioSource>().Single();
                int listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled);
                report["listenersDuring"] = listeners;
                Require(listeners == 1, "Exactly one active audio listener is required.");
                var master = LastRail.Resources.PreparedContent.Load<AudioClip>("CombatPresentation/Audio/LobbyDepartureV01");
                Require(master != null && master.frequency == 44100 && master.channels == 2 && master.samples == 5292001, "Approved full stereo Runway loop required.");
                report["importedSamples"] = master.samples;
                report["sampleRateHz"] = master.frequency;
                report["channels"] = master.channels;
                report["savedMusic"] = UnitySettingsBackend.SavedMusic;
                Require(Mathf.Approximately(voice.volume, UnitySettingsBackend.SavedMusic), "Master needs the user's saved volume.");
                void Sample(string stage, AudioClip expected)
                {
                    int voices = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(s => s.isPlaying && (s.clip == master || s.clip == battle));
                    Require(voice.clip == expected && voice.loop && voice.isPlaying && voices == 1, stage + " must have exactly one playing music source.");
                    rows.Add(new JObject { ["stage"] = stage, ["clip"] = voice.clip.name, ["sourceId"] = voice.GetInstanceID(), ["dspTime"] = AudioSettings.dspTime,
                        ["samples"] = voice.timeSamples, ["volume"] = voice.volume, ["musicVoices"] = voices });
                }
                await Until(() => voice.timeSamples > 12000, 8);
                Sample("lobby", master);
                for (int i = 0; i < 2; ++i)
                {
                    int before = voice.timeSamples;
                    deck.OpenOperations();
                    deck.GetComponentsInChildren<Button>().Single(b => b.name == "SelectDeparture0").onClick.Invoke();
                    Require(deck.GetComponentInChildren<DeparturePreparationView>() != null, "Departure preparation did not open.");
                    Sample("departure-" + i, master);
                    Require(voice.timeSamples >= before, "Opening departure rewound the track.");
                    await Task.Delay(180);
                    before = voice.timeSamples;
                    deck.GetComponentsInChildren<Button>().Single(b => b.name == "BackDeparture").onClick.Invoke();
                    Require(!deck.IsDialogOpen, "Departure back did not return to lobby.");
                    owner.EnterLobby();
                    Sample("lobby-return-" + i, master);
                    Require(voice.timeSamples >= before, "Returning to the lobby rewound the track.");
                }
                var mix = voice.GetComponent<LastRailAudioCuePlayer>();
                mix.ApplyMixSettings(.37f, UnitySettingsBackend.SavedEffects);
                Require(Mathf.Approximately(voice.volume, .37f), "The current BGM gain was not applied once.");
                Sample("mix-change", master);
                mix.ApplyMixSettings(UnitySettingsBackend.SavedMusic, UnitySettingsBackend.SavedEffects);
                driver = CreateDriver();
                driver.Open();
                owner.WatchCombat(driver);
                Require(!driver.HasBegunExecution, "Preparation must precede execution.");
                Sample("prepared-wait", master);
                int previous = voice.timeSamples;
                await Until(() => {
                    int now = voice.timeSamples;
                    if (now < previous)
                    {
                        Require(previous > master.samples - master.frequency && now < master.frequency, "Unexpected rewind away from the loop boundary.");
                        report["loopLastSampleObserved"] = previous;
                        report["loopNextSampleObserved"] = now;
                        return true;
                    }
                    previous = now;
                    return false;
                }, (double)master.samples / master.frequency + 15);
                Sample("natural-loop-during-entry-wait", master);
                int events = 0;
                driver.OrdinaryExecutionBegan += () => events++;
                driver.Advance(170000, 1);
                Require(driver.HasBegunExecution && events == 1 && driver.LastPresentationFailure == null, "First native execution must publish once.");
                await Until(() => voice.clip == battle && voice.timeSamples > 6000, 5);
                Sample("first-native-commit", battle);
                int first = voice.timeSamples;
                owner.WatchCombat(driver);
                driver.Advance(170000, 1);
                Require(voice.timeSamples >= first && events == 1, "Repeated binding or commit restarted the battle cue.");
                await Task.Delay(180);
                Sample("next-native-commit", battle);
                Require(voice.timeSamples > first, "Battle playback did not progress.");
                report["executionEvents"] = events;
                report["nodeTick"] = driver.Tick;
                report["fullCombatReady"] = driver.IsExecutionReady;
                owner.Dispose();
                owner = null;
                await Task.Yield();
                Require(voice == null || !voice.isPlaying, "Disposed owner retained playback.");
                report["passed"] = true;
            }
            catch (Exception error)
            {
                report["passed"] = false;
                report["error"] = error.ToString();
                throw;
            }
            finally
            {
                owner?.Dispose();
                driver?.Retire();
                Object.Destroy(screen);
                Object.Destroy(root);
                report["samples"] = rows;
                File.WriteAllText(Path.Combine(output, "lobby-bgm-player.json"), report.ToString());
            }
        }
    }
}
#endif
