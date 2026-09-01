#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace LastRail.Editor
{
    public static class LaunchOperatorVisualResourceValidationRunner
    {
        private const string OutputDirectoryName = "LastRailOperatorVisualQa";
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-operator-visual-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/Operators/README.md";

        public static void ValidateOperatorVisualResources()
        {
            var failures = new List<string>();
            var warnings = new List<string>();

            ValidateManifestAndReadme(failures);
            ValidateTextures(failures);
            ValidateShowcase(failures);

            var outputRoot = Path.Combine(Path.GetTempPath(), OutputDirectoryName);
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, "operator-visual-validation.json");
            File.WriteAllText(outputPath, BuildJson(failures, warnings));

            if (failures.Count > 0)
            {
                Debug.LogError("LASTRAIL launch operator visual validation FAILED: " + outputPath);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("LASTRAIL launch operator visual validation PASS: " + outputPath);
            EditorApplication.Exit(0);
        }

        private static void ValidateManifestAndReadme(List<string> failures)
        {
            var manifestFullPath = ProjectPath(ManifestPath);
            var readmeFullPath = ProjectPath(ReadmePath);
            RequireFile(manifestFullPath, "manifest", failures);
            RequireFile(readmeFullPath, "readme", failures);

            var combined = ReadIfExists(manifestFullPath) + "\n" + ReadIfExists(readmeFullPath);
            RequireContains(combined, "\"operatorCount\": 15", "operator count", failures);
            RequireContains(combined, "\"initialFreeCount\": 5", "initial/free count", failures);
            RequireContains(combined, "\"gachaEliteCount\": 10", "gacha elite count", failures);
            RequireContains(combined, "OpenAI image_gen built-in tool", "source note", failures);

            foreach (var token in ForbiddenTokens)
            {
                if (combined.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add("manifest/readme contains forbidden boundary token: " + token);
                }
            }
        }

        private static void ValidateTextures(List<string> failures)
        {
            foreach (var op in LaunchOperatorVisualResourceBuilder.Operators)
            {
                ValidateSprite(op.PortraitPath, 1024, 1024, failures);
                ValidateSprite(op.CardPath, 1080, 1440, failures);
                ValidateSprite(op.IconPath, 512, 512, failures);
                ValidateSprite(op.FramePath, 1080, 1440, failures);
            }
        }

        private static void ValidateShowcase(List<string> failures)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LaunchOperatorVisualResourceBuilder.ShowcasePrefabPath);
            if (prefab == null)
            {
                failures.Add("missing showcase prefab");
                return;
            }

            if (prefab.GetComponentInChildren<UnityEngine.UI.Text>(true) != null)
            {
                failures.Add("showcase contains legacy Text");
            }

            if (prefab.GetComponentInChildren<StandaloneInputModule>(true) != null)
            {
                failures.Add("showcase contains StandaloneInputModule");
            }

            if (prefab.GetComponentInChildren<InputSystemUIInputModule>(true) == null)
            {
                failures.Add("showcase missing InputSystemUIInputModule");
            }

            var textSurfaces = prefab.GetComponentsInChildren<TMP_Text>(true);
            if (textSurfaces.Length < 15)
            {
                failures.Add("showcase has too few TMP text surfaces: " + textSurfaces.Length);
            }

            var images = prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            if (images.Length < 30)
            {
                failures.Add("showcase has too few image surfaces: " + images.Length);
            }

            RequireSprite(prefab, "operator_card_slot_background", failures);
            RequireSprite(prefab, "operator_card_selected_frame", failures);
            RequireSprite(prefab, "operator_card_locked_overlay", failures);
            RequireSprite(prefab, "operator_badge_new", failures);
            RequireSprite(prefab, "operator_badge_owned", failures);
            RequireSprite(prefab, "operator_badge_level", failures);
            RequireSprite(prefab, "operator_rarity_pickup_elite", failures);
            RequireSprite(prefab, "operator_role_striker", failures);
            RequireSprite(prefab, "operator_grid_section_rail", failures);
        }

        private static void RequireSprite(GameObject prefab, string spriteName, List<string> failures)
        {
            foreach (var image in prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.sprite != null && image.sprite.name == spriteName)
                {
                    if (image.raycastTarget)
                    {
                        failures.Add("polish sprite blocks touch: " + spriteName);
                    }

                    return;
                }
            }

            failures.Add("showcase missing polish sprite: " + spriteName);
        }

        private static void ValidateSprite(string assetPath, int width, int height, List<string> failures)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                failures.Add("missing texture: " + assetPath);
                return;
            }

            if (texture.width != width || texture.height != height)
            {
                failures.Add(assetPath + " has unexpected dimensions " + texture.width + "x" + texture.height);
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                failures.Add("missing texture importer: " + assetPath);
                return;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                failures.Add(assetPath + " is not imported as Sprite");
            }

            if (importer.maxTextureSize > 2048)
            {
                failures.Add(assetPath + " max texture size exceeds 2048");
            }
        }

        private static void RequireFile(string path, string label, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add("missing " + label + ": " + path);
            }
        }

        private static void RequireContains(string value, string expected, string label, List<string> failures)
        {
            if (!value.Contains(expected, StringComparison.Ordinal))
            {
                failures.Add("missing " + label + ": " + expected);
            }
        }

        private static string ReadIfExists(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static string BuildJson(IReadOnlyList<string> failures, IReadOnlyList<string> warnings)
        {
            return "{" +
                "\"status\":\"" + (failures.Count == 0 ? "PASS" : "FAIL") + "\"," +
                "\"operatorCount\":15," +
                "\"initialFreeCount\":5," +
                "\"gachaEliteCount\":10," +
                "\"cardCount\":15," +
                "\"iconCount\":15," +
                "\"frameCount\":3," +
                "\"warningCount\":" + warnings.Count + "," +
                "\"failCount\":" + failures.Count + "," +
                "\"blockers\":[" + JoinQuoted(failures) + "]" +
                "}";
        }

        private static string JoinQuoted(IReadOnlyList<string> values)
        {
            var parts = new string[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                parts[i] = "\"" + values[i].Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            }

            return string.Join(",", parts);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private static readonly string[] ForbiddenTokens =
        {
            "accessToken",
            "refreshToken",
            "idToken",
            "accountId",
            "Authorization",
            "rawRng",
            "probability",
            "server submit",
            "Rail Choice",
            "World Raid",
            "PlayerPrefs"
        };
    }
}
#endif
