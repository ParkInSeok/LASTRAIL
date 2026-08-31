#if UNITY_5_3_OR_NEWER
using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchArtResourceFoundationTests
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-launch-art-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/Backgrounds/README.md";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/Backgrounds/Showcase/LRLaunchArtResourceShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchArtResourceShowcase.unity";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        private static readonly ExpectedArtResource[] RequiredResources =
        {
            new ExpectedArtResource("main_lobby_train_interior", "MainLobby", "Assets/_LASTRAIL/UI/Backgrounds/bg_main_lobby_train_interior.png"),
            new ExpectedArtResource("mission_select_rail_map", "MissionSelect", "Assets/_LASTRAIL/UI/Backgrounds/bg_mission_select_rail_map.png"),
            new ExpectedArtResource("combat_wasteland_rail", "CombatHud", "Assets/_LASTRAIL/UI/Backgrounds/bg_combat_wasteland_rail.png"),
            new ExpectedArtResource("gacha_recruitment_chamber", "Gacha", "Assets/_LASTRAIL/UI/KeyVisuals/kv_gacha_recruitment_chamber.png"),
            new ExpectedArtResource("codex_archive_records_room", "CodexArchive", "Assets/_LASTRAIL/UI/Backgrounds/bg_codex_archive_records_room.png"),
            new ExpectedArtResource("reward_result_supply_bay", "RewardReview", "Assets/_LASTRAIL/UI/Backgrounds/bg_reward_result_supply_bay.png"),
            new ExpectedArtResource("offline_reward_settlement", "OfflineReward", "Assets/_LASTRAIL/UI/Backgrounds/bg_offline_reward_settlement.png"),
            new ExpectedArtResource("story_raid_boss_entrance", "StoryRaidBoss", "Assets/_LASTRAIL/UI/KeyVisuals/kv_story_raid_boss_entrance.png")
        };

        [Test]
        public void LaunchArtResources_ExistAsPortraitSpriteAssets()
        {
            foreach (var resource in RequiredResources)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(resource.Path);
                Assert.That(texture, Is.Not.Null, resource.Path);
                Assert.That(texture.width, Is.EqualTo(1080), resource.Path + " width");
                Assert.That(texture.height, Is.EqualTo(1920), resource.Path + " height");

                var importer = AssetImporter.GetAtPath(resource.Path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, resource.Path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), resource.Path);
                Assert.That(importer.mipmapEnabled, Is.False, resource.Path + " mipmaps");
                Assert.That(importer.sRGBTexture, Is.True, resource.Path + " sRGB");
                Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(2048), resource.Path + " max texture size");
            }
        }

        [Test]
        public void LaunchArtManifest_RecordsEightResourcesAndUsageBoundaries()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
            Assert.That(File.Exists(fullPath), Is.True, ManifestPath);
            var manifest = JsonUtility.FromJson<LaunchArtManifestFixture>(File.ReadAllText(fullPath));
            Assert.That(manifest.name, Is.EqualTo("LAST RAIL Act 1 Launch Art Resources"));
            Assert.That(manifest.referenceResolution, Is.EqualTo("1080x1920"));
            Assert.That(manifest.resourceCount, Is.EqualTo(8));
            Assert.That(manifest.generatedBy, Is.EqualTo("OpenAI image_gen built-in tool"));
            Assert.That(manifest.uiOverlayReadable, Is.True);
            Assert.That(manifest.serverTruthFieldsAllowed, Is.False);
            Assert.That(manifest.railsChoiceIncluded, Is.False);
            Assert.That(manifest.worldRaidIncluded, Is.False);
            Assert.That(manifest.resources.Length, Is.EqualTo(RequiredResources.Length));

            foreach (var expected in RequiredResources)
            {
                var match = Array.Find(manifest.resources, item => item.id == expected.Id);
                Assert.That(match, Is.Not.Null, expected.Id);
                Assert.That(match.surface, Is.EqualTo(expected.Surface), expected.Id);
                Assert.That(match.path, Is.EqualTo(expected.Path), expected.Id);
                Assert.That(match.sourceType, Is.EqualTo("generated_local_launch_asset"), expected.Id);
                Assert.That(match.licenseNote, Is.EqualTo("Project-generated image resource; no external paid asset dependency."), expected.Id);
            }
        }

        [Test]
        public void LaunchArtReadme_ProvidesSourceLicenseAndReleaseUsageNotes()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ReadmePath);
            Assert.That(File.Exists(fullPath), Is.True, ReadmePath);
            var readme = File.ReadAllText(fullPath);
            Assert.That(readme, Does.Contain("OpenAI image_gen built-in tool"));
            Assert.That(readme, Does.Contain("project-generated launch UI resource"));
            Assert.That(readme, Does.Contain("No external paid asset dependency"));
            Assert.That(readme, Does.Contain("1080x1920"));
            Assert.That(readme, Does.Contain("safe area"));
        }

        [Test]
        public void LaunchArtShowcase_IsPortraitQaReadyWithReadableTmpOverlay()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null, ShowcasePrefabPath);
            var scaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(prefab.GetComponentsInChildren<Image>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(RequiredResources.Length));
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(RequiredResources.Length));
            Assert.That(prefab.GetComponentsInChildren<Text>(includeInactive: true), Is.Empty);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.That(font, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                Assert.That(text.font, Is.EqualTo(font), text.name);
                Assert.That(text.fontSharedMaterial, Is.EqualTo(material), text.name);
                Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Truncate), text.name);
                Assert.That(text.color.a, Is.GreaterThan(0.8f), text.name);
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
        public void LaunchArtResources_DoNotIntroduceServerTruthOrSecretPayloads()
        {
            var roots = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Backgrounds"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "KeyVisuals"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Style")
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
                "rewardAmount",
                "storyProgressMultiplier",
                "RailChoice",
                "World Raid"
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var text = File.ReadAllText(file);
                    foreach (var value in forbidden)
                    {
                        Assert.That(text, Does.Not.Contain(value), file + " leaked forbidden launch boundary token: " + value);
                    }
                }
            }
        }

        private readonly struct ExpectedArtResource
        {
            public ExpectedArtResource(string id, string surface, string path)
            {
                Id = id;
                Surface = surface;
                Path = path;
            }

            public string Id { get; }
            public string Surface { get; }
            public string Path { get; }
        }

        [Serializable]
        private sealed class LaunchArtManifestFixture
        {
            public string name;
            public string referenceResolution;
            public int resourceCount;
            public string generatedBy;
            public bool uiOverlayReadable;
            public bool serverTruthFieldsAllowed;
            public bool railsChoiceIncluded;
            public bool worldRaidIncluded;
            public LaunchArtResourceFixture[] resources;
        }

        [Serializable]
        private sealed class LaunchArtResourceFixture
        {
            public string id;
            public string surface;
            public string path;
            public string sourceType;
            public string licenseNote;
        }
    }
}
#endif
