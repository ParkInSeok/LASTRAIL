#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using LastRail.Presentation.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchAudioResourceFoundationTests
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-audio-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/Audio/README.md";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/Audio/Showcase/LRLaunchAudioResourceShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchAudioResourceShowcase.unity";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        private static readonly ExpectedAudio[] Clips =
        {
            new ExpectedAudio("ui_tap", "ui_sfx", "Assets/_LASTRAIL/Audio/Sfx/UI/ui_tap.wav", false),
            new ExpectedAudio("ui_back", "ui_sfx", "Assets/_LASTRAIL/Audio/Sfx/UI/ui_back.wav", false),
            new ExpectedAudio("ui_confirm", "ui_sfx", "Assets/_LASTRAIL/Audio/Sfx/UI/ui_confirm.wav", false),
            new ExpectedAudio("ui_error", "ui_sfx", "Assets/_LASTRAIL/Audio/Sfx/UI/ui_error.wav", false),
            new ExpectedAudio("ui_toggle_or_tab", "ui_sfx", "Assets/_LASTRAIL/Audio/Sfx/UI/ui_toggle_or_tab.wav", false),
            new ExpectedAudio("gacha_reveal_start", "gacha_sfx", "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_reveal_start.wav", false),
            new ExpectedAudio("gacha_result_rare", "gacha_sfx", "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_result_rare.wav", false),
            new ExpectedAudio("gacha_result_elite", "gacha_sfx", "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_result_elite.wav", false),
            new ExpectedAudio("gacha_result_pickup_elite", "gacha_sfx", "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_result_pickup_elite.wav", false),
            new ExpectedAudio("gacha_duplicate_conversion", "gacha_sfx", "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_duplicate_conversion.wav", false),
            new ExpectedAudio("reward_claim", "reward_sfx", "Assets/_LASTRAIL/Audio/Sfx/Reward/reward_claim.wav", false),
            new ExpectedAudio("reward_line_tick", "reward_sfx", "Assets/_LASTRAIL/Audio/Sfx/Reward/reward_line_tick.wav", false),
            new ExpectedAudio("combat_hit", "combat_sfx", "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_hit.wav", false),
            new ExpectedAudio("combat_skill", "combat_sfx", "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_skill.wav", false),
            new ExpectedAudio("combat_ultimate", "combat_sfx", "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_ultimate.wav", false),
            new ExpectedAudio("combat_shield", "combat_sfx", "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_shield.wav", false),
            new ExpectedAudio("combat_heal", "combat_sfx", "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_heal.wav", false),
            new ExpectedAudio("combat_buff", "combat_sfx", "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_buff.wav", false),
            new ExpectedAudio("combat_debuff", "combat_sfx", "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_debuff.wav", false),
            new ExpectedAudio("story_raid_warning", "story_raid_sfx", "Assets/_LASTRAIL/Audio/Sfx/StoryRaid/story_raid_warning.wav", false),
            new ExpectedAudio("boss_defeat", "story_raid_sfx", "Assets/_LASTRAIL/Audio/Sfx/StoryRaid/boss_defeat.wav", false),
            new ExpectedAudio("bgm_act1_rail_loop", "bgm", "Assets/_LASTRAIL/Audio/Bgm/bgm_act1_rail_loop.wav", true)
        };

        [Test]
        public void AudioClips_ExistWithNonzeroDurationAndSaneSampleRate()
        {
            foreach (var expected in Clips)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(expected.Path);
                Assert.That(clip, Is.Not.Null, expected.Path);
                Assert.That(clip.length, Is.GreaterThan(expected.Loop ? 8f : 0.04f), expected.Id + " duration");
                Assert.That(clip.frequency, Is.EqualTo(44100), expected.Id + " sample rate");
                Assert.That(clip.channels, Is.EqualTo(1), expected.Id + " channel count");

                var importer = AssetImporter.GetAtPath(expected.Path) as AudioImporter;
                Assert.That(importer, Is.Not.Null, expected.Path);
                Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.True, expected.Id + " preload");
                Assert.That(importer.forceToMono, Is.True, expected.Id + " mono import");
            }
        }

        [Test]
        public void Manifest_RecordsLaunchAudioCoverageAndPresentationOnlyBoundaries()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
            Assert.That(File.Exists(fullPath), Is.True, ManifestPath);
            var manifest = JsonUtility.FromJson<ManifestFixture>(File.ReadAllText(fullPath));

            Assert.That(manifest.name, Is.EqualTo("LAST RAIL Act 1 Launch Audio Resources"));
            Assert.That(manifest.generatedBy, Is.EqualTo("Unity procedural audio builder"));
            Assert.That(manifest.presentationOnly, Is.True);
            Assert.That(manifest.serverContractChanged, Is.False);
            Assert.That(manifest.gameplayTimingChanged, Is.False);
            Assert.That(manifest.rewardCalculationIncluded, Is.False);
            Assert.That(manifest.economyMutationIncluded, Is.False);
            Assert.That(manifest.clipCount, Is.EqualTo(Clips.Length));
            Assert.That(manifest.clips.Length, Is.EqualTo(Clips.Length));

            foreach (var expected in Clips)
            {
                var match = Array.Find(manifest.clips, clip => clip.id == expected.Id);
                Assert.That(match, Is.Not.Null, expected.Id);
                Assert.That(match.category, Is.EqualTo(expected.Category), expected.Id);
                Assert.That(match.path, Is.EqualTo(expected.Path), expected.Id);
                Assert.That(match.loop, Is.EqualTo(expected.Loop), expected.Id);
                Assert.That(match.sourceType, Is.EqualTo("generated_local_launch_audio"), expected.Id);
                Assert.That(match.durationSeconds, Is.GreaterThan(expected.Loop ? 8f : 0.04f), expected.Id);
            }
        }

        [Test]
        public void Readme_ProvidesGeneratedSourceLicenseAndReplacementPolicy()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ReadmePath);
            Assert.That(File.Exists(fullPath), Is.True, ReadmePath);
            var readme = File.ReadAllText(fullPath);
            Assert.That(readme, Does.Contain("Unity procedural audio builder"));
            Assert.That(readme, Does.Contain("No external paid asset dependency"));
            Assert.That(readme, Does.Contain("temporary launch-safe placeholders"));
        }

        [Test]
        public void Showcase_IsPortraitQaReadyWithAudioSourcesAndReadableTmpOverlay()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null, ShowcasePrefabPath);
            var scaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(prefab.GetComponentsInChildren<AudioSource>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(Clips.Length));
            var cuePlayer = prefab.GetComponentInChildren<LastRailAudioCuePlayer>(includeInactive: true);
            Assert.That(cuePlayer, Is.Not.Null);
            Assert.That(cuePlayer.BindingCount, Is.EqualTo(Clips.Length));
            Assert.That(cuePlayer.HasMissingClipBinding, Is.False);
            Assert.That(cuePlayer.HasUnintendedAutoplay, Is.False);
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(prefab.GetComponentsInChildren<Text>(includeInactive: true), Is.Empty);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.That(font, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                Assert.That(text.font, Is.EqualTo(font), text.name);
                Assert.That(text.fontSharedMaterial, Is.EqualTo(material), text.name);
                Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis).Or.EqualTo(TextOverflowModes.Truncate), text.name);
            }

            foreach (var source in prefab.GetComponentsInChildren<AudioSource>(includeInactive: true))
            {
                Assert.That(source.playOnAwake, Is.False, source.name);
                if (source.GetComponent<LastRailAudioCuePlayer>() == null)
                {
                    Assert.That(source.clip, Is.Not.Null, source.name);
                }
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ShowcaseScenePath);
            Assert.That(sceneAsset, Is.Not.Null, ShowcaseScenePath);
            var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
            try
            {
                Assert.That(scene.IsValid(), Is.True);
                var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
                Assert.That(eventSystems.Length, Is.EqualTo(1));
                Assert.That(eventSystems[0].GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
                Assert.That(eventSystems[0].GetComponent<StandaloneInputModule>(), Is.Null);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void AudioResources_DoNotIntroduceTruthSecretOrServerPayloads()
        {
            var roots = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "Audio"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Style", "lastrail-audio-resources.json")
            };
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
                "itemGrant",
                "economyGrant",
                "offlineSecondsTruth",
                "storyUnlockTruth",
                "server payload",
                "RailChoice",
                "World Raid"
            };

            foreach (var root in roots)
            {
                if (File.Exists(root))
                {
                    AssertForbiddenFile(root, forbidden);
                    continue;
                }

                if (!Directory.Exists(root)) continue;
                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AssertForbiddenFile(file, forbidden);
                }
            }
        }

        private static void AssertForbiddenFile(string file, string[] forbidden)
        {
            var text = File.ReadAllText(file);
            foreach (var value in forbidden)
            {
                Assert.That(text, Does.Not.Contain(value), file + " leaked forbidden launch boundary token: " + value);
            }
        }

        [Serializable]
        private sealed class ManifestFixture
        {
            public string name;
            public string generatedBy;
            public bool presentationOnly;
            public bool serverContractChanged;
            public bool gameplayTimingChanged;
            public bool rewardCalculationIncluded;
            public bool economyMutationIncluded;
            public int clipCount;
            public ClipFixture[] clips;
        }

        [Serializable]
        private sealed class ClipFixture
        {
            public string id;
            public string category;
            public string path;
            public bool loop;
            public float durationSeconds;
            public string sourceType;
        }

        private readonly struct ExpectedAudio
        {
            public ExpectedAudio(string id, string category, string path, bool loop)
            {
                Id = id;
                Category = category;
                Path = path;
                Loop = loop;
            }

            public readonly string Id;
            public readonly string Category;
            public readonly string Path;
            public readonly bool Loop;
        }
    }
}
#endif
