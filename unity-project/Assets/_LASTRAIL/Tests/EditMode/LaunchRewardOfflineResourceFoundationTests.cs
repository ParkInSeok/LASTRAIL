#if UNITY_EDITOR
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
    public sealed class LaunchRewardOfflineResourceFoundationTests
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-reward-offline-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/RewardOfflineResources/README.md";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/RewardOfflineResources/Showcase/LRLaunchRewardOfflineResourceShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchRewardOfflineResourceShowcase.unity";
        private const string RewardReviewPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab";
        private const string OfflineRewardPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        private static readonly ExpectedResource[] Resources =
        {
            new ExpectedResource("reward_result_plate", "frame", "Assets/_LASTRAIL/UI/RewardOfflineResources/Frames/reward_result_plate.png", 1024, 512),
            new ExpectedResource("reward_line_item_frame", "frame", "Assets/_LASTRAIL/UI/RewardOfflineResources/Frames/reward_line_item_frame.png", 1024, 160),
            new ExpectedResource("claim_success_badge", "badge", "Assets/_LASTRAIL/UI/RewardOfflineResources/Badges/claim_success_badge.png", 512, 192),
            new ExpectedResource("claim_pending_claimable_accent", "accent", "Assets/_LASTRAIL/UI/RewardOfflineResources/Badges/claim_pending_claimable_accent.png", 512, 192),
            new ExpectedResource("offline_settlement_meter", "meter", "Assets/_LASTRAIL/UI/RewardOfflineResources/Meters/offline_settlement_meter.png", 1024, 256),
            new ExpectedResource("offline_duration_accumulation_strip", "meter", "Assets/_LASTRAIL/UI/RewardOfflineResources/Meters/offline_duration_accumulation_strip.png", 1024, 192),
            new ExpectedResource("offline_empty_state_visual", "empty", "Assets/_LASTRAIL/UI/RewardOfflineResources/Empty/offline_empty_state_visual.png", 768, 512),
            new ExpectedResource("reward_chest_supply_crate_accent", "accent", "Assets/_LASTRAIL/UI/RewardOfflineResources/Accents/reward_chest_supply_crate_accent.png", 512, 512),
            new ExpectedResource("reward_material_glow", "accent", "Assets/_LASTRAIL/UI/RewardOfflineResources/Accents/reward_material_glow.png", 512, 512)
        };

        [Test]
        public void RewardOfflineResources_ExistAsImportableSpritesWithLaunchDimensions()
        {
            foreach (var resource in Resources)
            {
                AssertSprite(resource.Path, resource.Width, resource.Height);
            }
        }

        [Test]
        public void Manifest_RecordsResourceCoverageAndPresentationOnlyBoundaries()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
            Assert.That(File.Exists(fullPath), Is.True, ManifestPath);
            var manifest = JsonUtility.FromJson<ManifestFixture>(File.ReadAllText(fullPath));

            Assert.That(manifest.name, Is.EqualTo("LAST RAIL Act 1 Reward Offline Visual Resources"));
            Assert.That(manifest.referenceResolution, Is.EqualTo("1080x1920"));
            Assert.That(manifest.resourceCount, Is.EqualTo(Resources.Length));
            Assert.That(manifest.generatedBy, Is.EqualTo("Unity procedural texture builder"));
            Assert.That(manifest.presentationOnly, Is.True);
            Assert.That(manifest.serverContractChanged, Is.False);
            Assert.That(manifest.rewardGrantIncluded, Is.False);
            Assert.That(manifest.claimTruthIncluded, Is.False);
            Assert.That(manifest.offlineAccumulationTruthIncluded, Is.False);
            Assert.That(manifest.economyMutationIncluded, Is.False);
            Assert.That(manifest.resources.Length, Is.EqualTo(Resources.Length));

            foreach (var expected in Resources)
            {
                var match = Array.Find(manifest.resources, item => item.id == expected.Id);
                Assert.That(match, Is.Not.Null, expected.Id);
                Assert.That(match.category, Is.EqualTo(expected.Category), expected.Id);
                Assert.That(match.path, Is.EqualTo(expected.Path), expected.Id);
                Assert.That(match.sourceType, Is.EqualTo("generated_local_launch_asset"), expected.Id);
            }
        }

        [Test]
        public void Readme_ProvidesGeneratedSourceLicenseAndUsageNotes()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ReadmePath);
            Assert.That(File.Exists(fullPath), Is.True, ReadmePath);
            var readme = File.ReadAllText(fullPath);
            Assert.That(readme, Does.Contain("Unity procedural texture builder"));
            Assert.That(readme, Does.Contain("presentation resources only"));
            Assert.That(readme, Does.Contain("No external paid asset dependency"));
        }

        [Test]
        public void Showcase_IsPortraitQaReadyWithReadableTmpOverlay()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null, ShowcasePrefabPath);
            var scaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(prefab.GetComponentsInChildren<Image>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(Resources.Length));
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(8));
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
        public void RewardAndOfflinePrefabsConsumeResourcesAsVisualOnlyLayers()
        {
            var reward = AssetDatabase.LoadAssetAtPath<GameObject>(RewardReviewPrefabPath);
            var offline = AssetDatabase.LoadAssetAtPath<GameObject>(OfflineRewardPrefabPath);
            Assert.That(reward, Is.Not.Null);
            Assert.That(offline, Is.Not.Null);

            Assert.That(ContainsSprite(reward, "reward_result_plate"), Is.True);
            Assert.That(ContainsSprite(reward, "reward_line_item_frame"), Is.True);
            Assert.That(ContainsSprite(reward, "claim_success_badge"), Is.True);
            Assert.That(ContainsSprite(reward, "reward_chest_supply_crate_accent"), Is.True);
            Assert.That(ContainsSprite(offline, "offline_settlement_meter"), Is.True);
            Assert.That(ContainsSprite(offline, "offline_duration_accumulation_strip"), Is.True);
            Assert.That(ContainsSprite(offline, "offline_empty_state_visual"), Is.True);
            Assert.That(ContainsSprite(offline, "claim_pending_claimable_accent"), Is.True);
        }

        [Test]
        public void Resources_DoNotIntroduceTruthSecretOrServerPayloads()
        {
            var roots = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "RewardOfflineResources"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Style", "lastrail-reward-offline-resources.json")
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
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AssertForbiddenFile(file, forbidden);
                }
            }
        }

        private static void AssertSprite(string path, int width, int height)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(width), path + " width");
            Assert.That(texture.height, Is.EqualTo(height), path + " height");

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.mipmapEnabled, Is.False, path + " mipmaps");
            Assert.That(importer.sRGBTexture, Is.True, path + " sRGB");
            Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048), path + " max texture size");
        }

        private static bool ContainsSprite(GameObject root, string spriteName)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != null && image.sprite.name == spriteName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertForbiddenFile(string file, string[] forbidden)
        {
            var text = File.ReadAllText(file);
            foreach (var value in forbidden)
            {
                Assert.That(text, Does.Not.Contain(value), file + " leaked forbidden launch boundary token: " + value);
            }
        }

        private readonly struct ExpectedResource
        {
            public ExpectedResource(string id, string category, string path, int width, int height)
            {
                Id = id;
                Category = category;
                Path = path;
                Width = width;
                Height = height;
            }

            public string Id { get; }
            public string Category { get; }
            public string Path { get; }
            public int Width { get; }
            public int Height { get; }
        }

        [Serializable]
        private sealed class ManifestFixture
        {
            public string name;
            public string referenceResolution;
            public int resourceCount;
            public string generatedBy;
            public bool presentationOnly;
            public bool serverContractChanged;
            public bool rewardGrantIncluded;
            public bool claimTruthIncluded;
            public bool offlineAccumulationTruthIncluded;
            public bool economyMutationIncluded;
            public ResourceFixture[] resources;
        }

        [Serializable]
        private sealed class ResourceFixture
        {
            public string id;
            public string category;
            public string path;
            public string sourceType;
        }
    }
}
#endif
