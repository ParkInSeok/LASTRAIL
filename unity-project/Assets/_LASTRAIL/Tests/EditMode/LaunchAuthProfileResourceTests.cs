#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchAuthProfileResourceTests
    {
        private const string Root = "Assets/_LASTRAIL/UI/AuthProfileResources";
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-auth-profile-resources.json";
        private const string ReadmePath = Root + "/README.md";
        private const string SourcePath = Root + "/Sources/auth_profile_hifi_v1_source.png";
        private const string ShowcasePrefabPath = Root + "/Showcase/LRLaunchAuthProfileResourceShowcase.prefab";
        private const string MissionShellPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab";

        private static readonly ResourceSpec[] Resources =
        {
            new ResourceSpec("auth_profile_status_panel", "Panels", 1024, 512),
            new ResourceSpec("auth_guest_badge", "Badges", 256, 256),
            new ResourceSpec("auth_google_provider_badge", "Badges", 512, 256),
            new ResourceSpec("auth_keystore_lock_badge", "Badges", 256, 256),
            new ResourceSpec("auth_session_safe_badge", "Badges", 256, 256),
            new ResourceSpec("profile_nameplate_frame", "Frames", 1024, 256),
            new ResourceSpec("settings_toggle_panel", "Panels", 512, 384),
            new ResourceSpec("settings_audio_haptic_toggles", "Buttons", 512, 256),
            new ResourceSpec("auth_bottom_nav_accent", "Buttons", 512, 160)
        };

        [Test]
        public void ManifestReadmeAndSourceAtlasDescribeHifiVisualOnlyAuthProfileResources()
        {
            Assert.That(File.Exists(ProjectPath(ManifestPath)), Is.True, ManifestPath);
            Assert.That(File.Exists(ProjectPath(ReadmePath)), Is.True, ReadmePath);
            AssertSprite(SourcePath, 1024, 1536, 200000);

            var combined = File.ReadAllText(ProjectPath(ManifestPath)) + "\n" + File.ReadAllText(ProjectPath(ReadmePath));
            Assert.That(combined.Contains("\"resourceCount\": 9", StringComparison.Ordinal), Is.True);
            Assert.That(combined.Contains("\"presentationOnly\": true", StringComparison.Ordinal), Is.True);
            Assert.That(combined.Contains("OpenAI image generation", StringComparison.Ordinal), Is.True);
            Assert.That(combined.Contains("high-quality 2D", StringComparison.Ordinal), Is.True);
            Assert.That(combined.Contains("\"hasRealOAuthValues\": false", StringComparison.Ordinal), Is.True);

            foreach (var spec in Resources)
            {
                Assert.That(combined.Contains("\"id\": \"" + spec.Id + "\"", StringComparison.Ordinal), Is.True, spec.Id);
            }

            AssertForbidden(combined, "accessToken");
            AssertForbidden(combined, "refreshToken");
            AssertForbidden(combined, "idToken");
            AssertForbidden(combined, "Authorization");
            AssertForbidden(combined, "client_secret");
            AssertForbidden(combined, "server submit");
            AssertForbidden(combined, "reward amount");
            AssertForbidden(combined, "economy grant");
            AssertForbidden(combined, "probability");
            AssertForbidden(combined, "rawRng");
            AssertForbidden(combined, "Rail Choice");
            AssertForbidden(combined, "World Raid");
            AssertForbidden(combined, "PlayerPrefs");
        }

        [Test]
        public void AuthProfileResourcesAreImportableHifiSprites()
        {
            foreach (var spec in Resources)
            {
                AssertSprite(ResourcePath(spec), spec.Width, spec.Height, 12000);
            }
        }

        [Test]
        public void ShowcaseUsesTmpInputSystemAndEveryAuthProfileSprite()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null, ShowcasePrefabPath);
            Assert.That(prefab.GetComponentInChildren<UnityEngine.UI.Text>(true), Is.Null);
            Assert.That(prefab.GetComponentInChildren<StandaloneInputModule>(true), Is.Null);
            Assert.That(prefab.GetComponentInChildren<InputSystemUIInputModule>(true), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(true).Length, Is.GreaterThanOrEqualTo(5));

            foreach (var spec in Resources)
            {
                Assert.That(ContainsSprite(prefab, spec.Id), Is.True, spec.Id);
            }
        }

        [Test]
        public void MissionShellConsumesAuthProfileSpritesAsVisualOnlyLayers()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MissionShellPrefabPath);
            Assert.That(prefab, Is.Not.Null, MissionShellPrefabPath);

            foreach (var spec in Resources)
            {
                Assert.That(ContainsSprite(prefab, spec.Id), Is.True, spec.Id);
            }
        }

        private static string ResourcePath(ResourceSpec spec)
        {
            return Root + "/" + spec.Folder + "/" + spec.Id + ".png";
        }

        private static void AssertSprite(string path, int width, int height, long minimumBytes)
        {
            var fullPath = ProjectPath(path);
            Assert.That(File.Exists(fullPath), Is.True, path);
            Assert.That(new FileInfo(fullPath).Length, Is.GreaterThan(minimumBytes), path);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(width), path);
            Assert.That(texture.height, Is.EqualTo(height), path);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
            Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048), path);
        }

        private static bool ContainsSprite(GameObject root, string spriteName)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != null && image.sprite.name == spriteName && !image.raycastTarget)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertForbidden(string value, string token)
        {
            Assert.That(value.Contains(token, StringComparison.OrdinalIgnoreCase), Is.False, token);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private readonly struct ResourceSpec
        {
            public ResourceSpec(string id, string folder, int width, int height)
            {
                Id = id;
                Folder = folder;
                Width = width;
                Height = height;
            }

            public string Id { get; }
            public string Folder { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }
}
#endif
