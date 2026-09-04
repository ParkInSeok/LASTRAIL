#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchAuthProfileResourceValidationRunner
    {
        private const string OutputRoot = "QA/Act1Launch/Reports";
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-auth-profile-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/AuthProfileResources/README.md";
        private const string MissionShellPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab";

        public static void ValidateAuthProfileResources()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var failures = new List<string>();
            var warnings = new List<string>();

            ValidateManifestAndReadme(failures);
            ValidateSprites(failures);
            ValidatePrefab(LaunchAuthProfileResourceBuilder.ShowcasePrefabPath, "showcase", failures);
            ValidatePrefab(MissionShellPrefabPath, "mission_shell", failures);

            var outputDir = ProjectPath(OutputRoot);
            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir, "auth-profile-resource-validation.json");
            File.WriteAllText(outputPath, BuildJson(failures, warnings));

            if (failures.Count > 0)
            {
                Debug.LogError("LASTRAIL AuthProfile resource validation FAILED: " + outputPath);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("LASTRAIL AuthProfile resource validation PASS: " + outputPath);
            EditorApplication.Exit(0);
        }

        private static void ValidateManifestAndReadme(List<string> failures)
        {
            var combined = ReadIfExists(ProjectPath(ManifestPath)) + "\n" + ReadIfExists(ProjectPath(ReadmePath));
            RequireContains(combined, "\"resourceCount\": 9", "resource count", failures);
            RequireContains(combined, "\"presentationOnly\": true", "presentation-only boundary", failures);
            RequireContains(combined, "\"hasRealOAuthValues\": false", "real OAuth boundary", failures);
            RequireContains(combined, "OpenAI image generation", "source note", failures);
            RequireContains(combined, "high-quality 2D", "hifi source note", failures);

            foreach (var token in ForbiddenTokens)
            {
                if (combined.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add("manifest/readme contains forbidden boundary token: " + token);
                }
            }
        }

        private static void ValidateSprites(List<string> failures)
        {
            ValidateSprite(LaunchAuthProfileResourceBuilder.SourceAtlasPath, 1024, 1536, failures);
            foreach (var resource in LaunchAuthProfileResourceBuilder.Resources)
            {
                ValidateSprite(resource.Path, resource.Width, resource.Height, failures);
            }
        }

        private static void ValidatePrefab(string prefabPath, string label, List<string> failures)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                failures.Add("missing " + label + " prefab");
                return;
            }

            if (prefab.GetComponentInChildren<UnityEngine.UI.Text>(true) != null)
            {
                failures.Add(label + " contains legacy Text");
            }

            if (prefab.GetComponentInChildren<StandaloneInputModule>(true) != null)
            {
                failures.Add(label + " contains StandaloneInputModule");
            }

            if (label == "showcase" && prefab.GetComponentInChildren<InputSystemUIInputModule>(true) == null)
            {
                failures.Add(label + " missing InputSystemUIInputModule");
            }

            if (label == "showcase" && prefab.GetComponentsInChildren<TMP_Text>(true).Length < 5)
            {
                failures.Add(label + " has too few TMP text surfaces: " + prefab.GetComponentsInChildren<TMP_Text>(true).Length);
            }

            foreach (var resource in LaunchAuthProfileResourceBuilder.Resources)
            {
                RequireSprite(prefab, resource.Id, label, failures);
            }
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
                failures.Add(assetPath + " unexpected size " + texture.width + "x" + texture.height);
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                failures.Add(assetPath + " is not an importable single Sprite");
            }
        }

        private static void RequireSprite(GameObject prefab, string spriteName, string label, List<string> failures)
        {
            foreach (var image in prefab.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != null && image.sprite.name == spriteName)
                {
                    if (image.raycastTarget)
                    {
                        failures.Add(label + " sprite blocks touch: " + spriteName);
                    }

                    return;
                }
            }

            failures.Add(label + " missing sprite binding: " + spriteName);
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
                "\"resourceCount\":9," +
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
            "full weights",
            "server submit",
            "reward amount",
            "economy grant",
            "Rail Choice",
            "World Raid",
            "PlayerPrefs"
        };
    }
}
#endif
