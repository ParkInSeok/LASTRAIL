#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchArtResourceValidationRunner
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-launch-art-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/Backgrounds/README.md";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/Backgrounds/Showcase/LRLaunchArtResourceShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchArtResourceShowcase.unity";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        [MenuItem("LASTRAIL/Launch/Validate Art Resource Foundation")]
        public static void ValidateArtResources()
        {
            var outputRoot = Path.Combine(Path.GetTempPath(), "LastRailArtResourceQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "art-resource-validation.json");

            try
            {
                ValidateTextures();
                ValidateManifestAndReadme();
                ValidateShowcase();
                ValidateForbiddenText();
                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"resourceCount\":8,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL launch art resource validation PASS: " + reportPath);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"resourceCount\":8,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"launch_art_resource_validation_failed\"]}");
                Debug.LogError("LASTRAIL launch art resource validation failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void ValidateTextures()
        {
            foreach (var resource in LaunchArtResourceBuilder.Resources)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(resource.Path);
                Require(texture != null, "Missing launch art texture: " + resource.Path);
                Require(texture.width == 1080, "Launch art width must be 1080: " + resource.Path);
                Require(texture.height == 1920, "Launch art height must be 1920: " + resource.Path);

                var importer = AssetImporter.GetAtPath(resource.Path) as TextureImporter;
                Require(importer != null, "Missing texture importer: " + resource.Path);
                Require(importer.textureType == TextureImporterType.Sprite, "Launch art must be Sprite: " + resource.Path);
                Require(!importer.mipmapEnabled, "Launch art mipmaps must be disabled: " + resource.Path);
                Require(importer.sRGBTexture, "Launch art sRGB must be enabled: " + resource.Path);
                Require(importer.maxTextureSize >= 2048, "Launch art max size must allow 2048: " + resource.Path);
            }
        }

        private static void ValidateManifestAndReadme()
        {
            var manifestPath = ProjectPath(ManifestPath);
            var readmePath = ProjectPath(ReadmePath);
            Require(File.Exists(manifestPath), "Launch art manifest missing.");
            Require(File.Exists(readmePath), "Launch art README missing.");

            var manifest = File.ReadAllText(manifestPath);
            Require(manifest.Contains("\"name\": \"LAST RAIL Act 1 Launch Art Resources\""), "Manifest name mismatch.");
            Require(manifest.Contains("\"resourceCount\": 8"), "Manifest resource count mismatch.");
            Require(manifest.Contains("\"generatedBy\": \"OpenAI image_gen built-in tool\""), "Manifest source note missing.");
            Require(manifest.Contains("\"uiOverlayReadable\": true"), "Manifest readability flag missing.");
            Require(manifest.Contains("\"serverTruthFieldsAllowed\": false"), "Manifest server truth boundary missing.");
            Require(manifest.Contains("\"railsChoiceIncluded\": false"), "Manifest rail choice boundary missing.");
            Require(manifest.Contains("\"worldRaidIncluded\": false"), "Manifest world raid boundary missing.");
            foreach (var resource in LaunchArtResourceBuilder.Resources)
            {
                Require(manifest.Contains(resource.Id), "Manifest missing resource id: " + resource.Id);
                Require(manifest.Contains(resource.Path), "Manifest missing resource path: " + resource.Path);
            }

            var readme = File.ReadAllText(readmePath);
            Require(readme.Contains("OpenAI image_gen built-in tool"), "README source note missing.");
            Require(readme.Contains("project-generated launch UI resource"), "README project-generated note missing.");
            Require(readme.Contains("No external paid asset dependency"), "README external asset note missing.");
            Require(readme.Contains("safe area"), "README safe area note missing.");
        }

        private static void ValidateShowcase()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Require(prefab != null, "Launch art showcase prefab missing.");
            var scaler = prefab.GetComponent<CanvasScaler>();
            Require(scaler != null, "Launch art showcase CanvasScaler missing.");
            Require(scaler.referenceResolution == new Vector2(1080f, 1920f), "Launch art showcase reference resolution mismatch.");
            Require(prefab.GetComponentsInChildren<Text>(true).Length == 0, "Launch art showcase uses legacy Text.");
            Require(prefab.GetComponentsInChildren<Image>(true).Length >= LaunchArtResourceBuilder.Resources.Length, "Launch art showcase image coverage too low.");

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Require(font != null, "Pretendard TMP font missing.");
            Require(material != null, "Pretendard TMP material missing.");
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                Require(text.font == font, "Unexpected showcase font: " + text.name);
                Require(text.fontSharedMaterial == material, "Unexpected showcase material: " + text.name);
                Require(text.overflowMode == TextOverflowModes.Truncate, "Showcase text overflow policy mismatch: " + text.name);
            }

            var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
            Require(scene.IsValid(), "Launch art showcase scene invalid.");
            var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            Require(eventSystems.Length == 1, "Launch art showcase scene must have one EventSystem.");
            Require(eventSystems[0].GetComponent<InputSystemUIInputModule>() != null, "Input System UI module missing.");
            Require(eventSystems[0].GetComponent<StandaloneInputModule>() == null, "Legacy StandaloneInputModule found.");
        }

        private static void ValidateForbiddenText()
        {
            var roots = new[]
            {
                "Assets/_LASTRAIL/UI/Backgrounds",
                "Assets/_LASTRAIL/UI/KeyVisuals",
                "Assets/_LASTRAIL/UI/Style"
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
                var fullRoot = ProjectPath(root);
                if (!Directory.Exists(fullRoot))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(fullRoot, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var text = File.ReadAllText(file);
                    foreach (var value in forbidden)
                    {
                        Require(!text.Contains(value), file + " contains forbidden boundary token: " + value);
                    }
                }
            }
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
#endif
