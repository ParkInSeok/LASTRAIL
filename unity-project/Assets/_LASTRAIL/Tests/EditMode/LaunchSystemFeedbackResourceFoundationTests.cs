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
    public sealed class LaunchSystemFeedbackResourceFoundationTests
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-system-feedback-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/SystemFeedbackResources/README.md";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/SystemFeedbackResources/Showcase/LRLaunchSystemFeedbackResourceShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchSystemFeedbackResourceShowcase.unity";
        private const string MissionShellPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab";
        private const string GachaPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab";
        private const string RewardReviewPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab";
        private const string OfflineRewardPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        private static readonly ExpectedResource[] Resources =
        {
            new ExpectedResource("loading_rail_indicator", "loading", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Loading/loading_rail_indicator.png", 768, 192),
            new ExpectedResource("feedback_success_badge", "badge", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Badges/feedback_success_badge.png", 512, 192),
            new ExpectedResource("feedback_failure_badge", "badge", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Badges/feedback_failure_badge.png", 512, 192),
            new ExpectedResource("feedback_network_error_badge", "error", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Badges/feedback_network_error_badge.png", 512, 192),
            new ExpectedResource("maintenance_notice_panel", "notice", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Notices/maintenance_notice_panel.png", 1024, 512),
            new ExpectedResource("retry_refresh_accent", "retry", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Retry/retry_refresh_accent.png", 512, 512),
            new ExpectedResource("warning_strip_banner_accent", "warning", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Warning/warning_strip_banner_accent.png", 1024, 160),
            new ExpectedResource("disabled_unavailable_overlay", "disabled", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Disabled/disabled_unavailable_overlay.png", 768, 512),
            new ExpectedResource("inline_status_dot_set", "status", "Assets/_LASTRAIL/UI/SystemFeedbackResources/Status/inline_status_dot_set.png", 512, 128)
        };

        [Test]
        public void SystemFeedbackResources_ExistAsImportableSpritesWithLaunchDimensions()
        {
            foreach (var resource in Resources)
            {
                AssertSprite(resource.Path, resource.Width, resource.Height);
            }
        }

        [Test]
        public void Manifest_RecordsFeedbackCoverageAndPresentationOnlyBoundaries()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
            Assert.That(File.Exists(fullPath), Is.True, ManifestPath);
            var manifest = JsonUtility.FromJson<ManifestFixture>(File.ReadAllText(fullPath));

            Assert.That(manifest.name, Is.EqualTo("LAST RAIL Act 1 System Feedback Visual Resources"));
            Assert.That(manifest.referenceResolution, Is.EqualTo("1080x1920"));
            Assert.That(manifest.resourceCount, Is.EqualTo(Resources.Length));
            Assert.That(manifest.generatedBy, Is.EqualTo("OpenAI image_gen built-in tool"));
            Assert.That(manifest.presentationOnly, Is.True);
            Assert.That(manifest.serverContractChanged, Is.False);
            Assert.That(manifest.hasNetworkSourceOfTruth, Is.False);
            Assert.That(manifest.hasAuthSourceOfTruth, Is.False);
            Assert.That(manifest.hasErrorSourceOfTruth, Is.False);
            Assert.That(manifest.rewardCalculationIncluded, Is.False);
            Assert.That(manifest.economyMutationIncluded, Is.False);
            Assert.That(manifest.resources.Length, Is.EqualTo(Resources.Length));

            foreach (var expected in Resources)
            {
                var match = Array.Find(manifest.resources, item => item.id == expected.Id);
                Assert.That(match, Is.Not.Null, expected.Id);
                Assert.That(match.category, Is.EqualTo(expected.Category), expected.Id);
                Assert.That(match.path, Is.EqualTo(expected.Path), expected.Id);
                Assert.That(match.sourceType, Is.EqualTo("generated_image_gen_launch_asset"), expected.Id);
            }
        }

        [Test]
        public void Readme_ProvidesGeneratedSourceLicenseAndUsageNotes()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ReadmePath);
            Assert.That(File.Exists(fullPath), Is.True, ReadmePath);
            var readme = File.ReadAllText(fullPath);
            Assert.That(readme, Does.Contain("OpenAI image_gen built-in tool"));
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
        public void LaunchPrefabsConsumeFeedbackResourcesAsVisualOnlyLayers()
        {
            var mission = AssetDatabase.LoadAssetAtPath<GameObject>(MissionShellPrefabPath);
            var gacha = AssetDatabase.LoadAssetAtPath<GameObject>(GachaPrefabPath);
            var reward = AssetDatabase.LoadAssetAtPath<GameObject>(RewardReviewPrefabPath);
            var offline = AssetDatabase.LoadAssetAtPath<GameObject>(OfflineRewardPrefabPath);
            Assert.That(mission, Is.Not.Null);
            Assert.That(gacha, Is.Not.Null);
            Assert.That(reward, Is.Not.Null);
            Assert.That(offline, Is.Not.Null);

            Assert.That(ContainsSprite(mission, "inline_status_dot_set"), Is.True);
            Assert.That(ContainsSprite(mission, "loading_rail_indicator"), Is.True);
            Assert.That(ContainsSprite(mission, "warning_strip_banner_accent"), Is.True);
            Assert.That(ContainsSprite(gacha, "feedback_network_error_badge"), Is.True);
            Assert.That(ContainsSprite(gacha, "retry_refresh_accent"), Is.True);
            Assert.That(ContainsSprite(reward, "feedback_success_badge"), Is.True);
            Assert.That(ContainsSprite(reward, "inline_status_dot_set"), Is.True);
            Assert.That(ContainsSprite(offline, "maintenance_notice_panel"), Is.True);
            Assert.That(ContainsSprite(offline, "disabled_unavailable_overlay"), Is.True);
        }

        [Test]
        public void Resources_DoNotIntroduceTruthSecretOrServerPayloads()
        {
            var roots = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "SystemFeedbackResources"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Style", "lastrail-system-feedback-resources.json")
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
                "networkTruth",
                "authTruth",
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
            public bool hasNetworkSourceOfTruth;
            public bool hasAuthSourceOfTruth;
            public bool hasErrorSourceOfTruth;
            public bool rewardCalculationIncluded;
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
