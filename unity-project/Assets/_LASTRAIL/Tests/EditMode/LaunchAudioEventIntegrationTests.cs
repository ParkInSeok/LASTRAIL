#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Presentation.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchAudioEventIntegrationTests
    {
        private const string IntegrationManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-audio-integrations.json";

        private static readonly string[] LaunchScreenPrefabPaths =
        {
            "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/CodexArchiveScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/StoryProgressSummaryScreen.prefab"
        };

        [Test]
        public void DefaultIntegrationMap_ResolvesPresentationAliasesToLaunchClipIds()
        {
            var catalog = LaunchAudioCueCatalog.CreateDefault();

            Assert.That(catalog.ResolveClipId("ui_tap"), Is.EqualTo("ui_tap"));
            Assert.That(catalog.ResolveClipId("sfx_gacha_elite"), Is.EqualTo("gacha_result_elite"));
            Assert.That(catalog.ResolveClipId("sfx_gacha_pickup_elite"), Is.EqualTo("gacha_result_pickup_elite"));
            Assert.That(catalog.ResolveClipId("sfx_gacha_duplicate_conversion"), Is.EqualTo("gacha_duplicate_conversion"));
            Assert.That(catalog.ResolveClipId("sfx_hit"), Is.EqualTo("combat_hit"));
            Assert.That(catalog.ResolveClipId("sfx_boss_phase"), Is.EqualTo("story_raid_warning"));
            Assert.That(catalog.ResolveClipId("bgm_act1_loop"), Is.EqualTo("bgm_act1_rail_loop"));
            Assert.That(catalog.ResolveClipId("unknown_cue"), Is.Null);
            Assert.That(catalog.KnownClipCount, Is.EqualTo(22));
            Assert.That(catalog.HasServerTransport, Is.False);
            Assert.That(catalog.HasGameplayTimingMutation, Is.False);
            Assert.That(catalog.HasRewardCalculation, Is.False);
            Assert.That(catalog.HasEconomyMutation, Is.False);
            Assert.That(catalog.HasClientSideRng, Is.False);
        }

        [Test]
        public void CuePlayer_UsesClipBindingsWithoutAutoplayAndKeepsBgmOptIn()
        {
            var root = new GameObject("AudioCuePlayerTest");
            try
            {
                var sfxSource = root.AddComponent<AudioSource>();
                var bgmSource = root.AddComponent<AudioSource>();
                var player = root.AddComponent<LastRailAudioCuePlayer>();
                player.ConfigureForTests(
                    sfxSource,
                    bgmSource,
                    new[]
                    {
                        new LastRailAudioClipBinding("ui_tap", TestClip("ui_tap"), false, 0.8f),
                        new LastRailAudioClipBinding("gacha_result_elite", TestClip("gacha_result_elite"), false, 0.8f),
                        new LastRailAudioClipBinding("bgm_act1_rail_loop", TestClip("bgm_act1_rail_loop"), true, 0.35f)
                    });

                var sfx = player.TryPlayCue("sfx_gacha_elite");
                var bgm = player.TryPrepareBgmLoop("bgm_act1_loop");

                Assert.That(sfx.Played, Is.True);
                Assert.That(sfx.RequestedCueId, Is.EqualTo("sfx_gacha_elite"));
                Assert.That(sfx.ResolvedClipId, Is.EqualTo("gacha_result_elite"));
                Assert.That(sfx.Loop, Is.False);
                Assert.That(bgm.Played, Is.True);
                Assert.That(bgm.ResolvedClipId, Is.EqualTo("bgm_act1_rail_loop"));
                Assert.That(bgm.Loop, Is.True);
                Assert.That(sfxSource.playOnAwake, Is.False);
                Assert.That(bgmSource.playOnAwake, Is.False);
                Assert.That(bgmSource.loop, Is.True);
                Assert.That(player.HasServerTransport, Is.False);
                Assert.That(player.HasRewardCalculation, Is.False);
                Assert.That(player.HasEconomyMutation, Is.False);
                Assert.That(player.HasClientSideRng, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LaunchScreenPrefabs_HaveAudioCuePlayersWithReferencedClipsAndNoUnintendedAutoplay()
        {
            foreach (var prefabPath in LaunchScreenPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab, Is.Not.Null, prefabPath);
                var player = prefab.GetComponent<LastRailAudioCuePlayer>();
                Assert.That(player, Is.Not.Null, prefabPath);
                Assert.That(player.BindingCount, Is.GreaterThanOrEqualTo(1), prefabPath);
                Assert.That(player.HasMissingClipBinding, Is.False, prefabPath);
                Assert.That(player.HasUnintendedAutoplay, Is.False, prefabPath);

                foreach (var source in prefab.GetComponentsInChildren<AudioSource>(includeInactive: true))
                {
                    Assert.That(source.playOnAwake, Is.False, prefabPath + " / " + source.name);
                }
            }
        }

        [Test]
        public void IntegrationManifest_RecordsConnectedSurfacesWithoutSensitiveTruthOrRawPayload()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), IntegrationManifestPath);
            Assert.That(File.Exists(fullPath), Is.True, IntegrationManifestPath);
            var json = File.ReadAllText(fullPath);

            Assert.That(Compact(json), Does.Contain("\"connectedCueCount\":22"));
            var compact = Compact(json);
            Assert.That(compact, Does.Contain("\"surface\":\"common_ui\""));
            Assert.That(compact, Does.Contain("\"surface\":\"gacha_screen\""));
            Assert.That(compact, Does.Contain("\"surface\":\"combat_hud\""));
            Assert.That(compact, Does.Contain("\"surface\":\"reward_offline\""));
            Assert.That(compact, Does.Contain("\"surface\":\"story_raid\""));
            Assert.That(compact, Does.Contain("\"bgmAct1RailLoopAutoplay\":false"));
            Assert.That(compact, Does.Contain("\"serverContractChanged\":false"));
            Assert.That(compact, Does.Contain("\"gameplayTimingChanged\":false"));
            Assert.That(compact, Does.Contain("\"rewardCalculationIncluded\":false"));
            Assert.That(compact, Does.Contain("\"economyMutationIncluded\":false"));

            var forbidden = new[]
            {
                "accessToken",
                "refreshToken",
                "idToken",
                "client_secret",
                "accountId",
                "Authorization",
                "rawRng",
                "probability",
                "fullWeights",
                "rewardAmount",
                "ledger",
                "economyGrant",
                "serverSubmit",
                "RailChoice",
                "WorldRaid"
            };
            foreach (var value in forbidden)
            {
                Assert.That(json, Does.Not.Contain(value), value);
            }
        }

        private static AudioClip TestClip(string name)
        {
            return AudioClip.Create(name, 16, 1, 44100, false);
        }

        private static string Compact(string json)
        {
            return json.Replace(" ", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty);
        }
    }
}
#endif
