#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    public static class LaunchCombatArtResourceValidationRunner
    {
        private const string OutputDirectoryName = "LastRailCombatArtQa";
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-combat-art-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/CombatResources/README.md";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchCombatArtResourceShowcase.unity";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        [MenuItem("LASTRAIL/Launch/Validate Combat Art Resource Foundation")]
        public static void ValidateCombatArtResources()
        {
            var failures = new List<string>();
            var warnings = new List<string>();
            try
            {
                ValidateManifestAndReadme(failures);
                ValidateTextures(failures);
                ValidateShowcase(failures);
                ValidateForbiddenText(failures);
            }
            catch (Exception ex)
            {
                failures.Add(ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", OutputDirectoryName);
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, "combat-art-validation.json");
            File.WriteAllText(outputPath, BuildJson(failures, warnings));

            if (failures.Count > 0)
            {
                Debug.LogError("LASTRAIL launch combat art validation FAILED: " + outputPath);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("LASTRAIL launch combat art validation PASS: " + outputPath);
            EditorApplication.Exit(0);
        }

        private static void ValidateManifestAndReadme(List<string> failures)
        {
            var manifestPath = ProjectPath(ManifestPath);
            var readmePath = ProjectPath(ReadmePath);
            RequireFile(manifestPath, "manifest", failures);
            RequireFile(readmePath, "readme", failures);

            var combined = ReadIfExists(manifestPath) + "\n" + ReadIfExists(readmePath);
            RequireContains(combined, "\"normalEnemyCount\": 6", "normal enemy count", failures);
            RequireContains(combined, "\"eliteEnemyCount\": 2", "elite enemy count", failures);
            RequireContains(combined, "\"bossCount\": 1", "boss count", failures);
            RequireContains(combined, "\"vfxCount\": 8", "vfx count", failures);
            RequireContains(combined, "OpenAI image_gen built-in tool", "source note", failures);
            RequireContains(combined, "External paid asset dependency: none", "external asset note", failures);

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
            foreach (var enemy in LaunchCombatArtResourceBuilder.Enemies)
            {
                ValidateSprite(enemy.Path, enemy.Width, enemy.Height, failures);
            }

            foreach (var vfx in LaunchCombatArtResourceBuilder.VfxResources)
            {
                ValidateSprite(vfx.Path, vfx.Width, vfx.Height, failures);
            }

            foreach (var accent in LaunchCombatArtResourceBuilder.Accents)
            {
                ValidateSprite(accent.Path, accent.Width, accent.Height, failures);
            }
        }

        private static void ValidateShowcase(List<string> failures)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LaunchCombatArtResourceBuilder.ShowcasePrefabPath);
            if (prefab == null)
            {
                failures.Add("missing showcase prefab");
                return;
            }

            var scaler = prefab.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.referenceResolution != new Vector2(1080f, 1920f)) failures.Add("showcase reference resolution mismatch");
            if (prefab.GetComponentInChildren<Text>(true) != null) failures.Add("showcase contains legacy Text");
            if (prefab.GetComponentInChildren<StandaloneInputModule>(true) != null) failures.Add("showcase contains StandaloneInputModule");
            if (prefab.GetComponentInChildren<InputSystemUIInputModule>(true) == null) failures.Add("showcase missing InputSystemUIInputModule");

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != font) failures.Add("unexpected showcase font: " + text.name);
                if (text.fontSharedMaterial != material) failures.Add("unexpected showcase material: " + text.name);
            }

            if (prefab.GetComponentsInChildren<Image>(true).Length < 22) failures.Add("showcase image coverage too low");

            var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) failures.Add("showcase scene invalid");
            var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (eventSystems.Length != 1)
            {
                failures.Add("showcase scene must have one EventSystem");
            }
            else
            {
                if (eventSystems[0].GetComponent<InputSystemUIInputModule>() == null) failures.Add("scene missing InputSystemUIInputModule");
                if (eventSystems[0].GetComponent<StandaloneInputModule>() != null) failures.Add("scene contains StandaloneInputModule");
            }
        }

        private static void ValidateForbiddenText(List<string> failures)
        {
            var roots = new[] { "Assets/_LASTRAIL/UI/CombatResources", "Assets/_LASTRAIL/UI/Style/lastrail-combat-art-resources.json" };
            foreach (var root in roots)
            {
                var fullRoot = ProjectPath(root);
                if (File.Exists(fullRoot)) { ValidateForbiddenFile(fullRoot, failures); continue; }
                if (!Directory.Exists(fullRoot)) continue;
                foreach (var file in Directory.GetFiles(fullRoot, "*", SearchOption.AllDirectories)) ValidateForbiddenFile(file, failures);
            }
        }

        private static void ValidateForbiddenFile(string file, List<string> failures)
        {
            if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) return;
            var text = File.ReadAllText(file);
            foreach (var value in ForbiddenTokens)
            {
                if (text.Contains(value, StringComparison.OrdinalIgnoreCase)) failures.Add(file + " contains forbidden boundary token: " + value);
            }
        }

        private static void ValidateSprite(string assetPath, int width, int height, List<string> failures)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null) { failures.Add("missing texture: " + assetPath); return; }
            if (texture.width != width || texture.height != height) failures.Add(assetPath + " has unexpected dimensions " + texture.width + "x" + texture.height);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) { failures.Add("missing texture importer: " + assetPath); return; }
            if (importer.textureType != TextureImporterType.Sprite) failures.Add(assetPath + " is not imported as Sprite");
            if (importer.mipmapEnabled) failures.Add(assetPath + " has mipmaps enabled");
            if (importer.maxTextureSize > 2048) failures.Add(assetPath + " max texture size exceeds 2048");
        }

        private static string BuildJson(IReadOnlyList<string> failures, IReadOnlyList<string> warnings)
        {
            return "{" +
                "\"status\":\"" + (failures.Count == 0 ? "PASS" : "FAIL") + "\"," +
                "\"normalEnemyCount\":6," +
                "\"eliteEnemyCount\":2," +
                "\"bossCount\":1," +
                "\"vfxCount\":8," +
                "\"accentCount\":2," +
                "\"warningCount\":" + warnings.Count + "," +
                "\"failCount\":" + failures.Count + "," +
                "\"blockers\":[" + JoinQuoted(failures) + "]" +
                "}";
        }

        private static void RequireFile(string path, string label, List<string> failures) { if (!File.Exists(path)) failures.Add("missing " + label + ": " + path); }
        private static void RequireContains(string value, string expected, string label, List<string> failures) { if (!value.Contains(expected, StringComparison.Ordinal)) failures.Add("missing " + label + ": " + expected); }
        private static string ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        private static string ProjectPath(string assetPath) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);

        private static string JoinQuoted(IReadOnlyList<string> values)
        {
            var parts = new string[values.Count];
            for (var i = 0; i < values.Count; i++) parts[i] = "\"" + values[i].Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            return string.Join(",", parts);
        }

        private static readonly string[] ForbiddenTokens =
        {
            "accessToken", "refreshToken", "idToken", "accountId", "Authorization", "rawRng", "probability", "rewardAmount", "damageFormula", "combatDamage", "server submit", "Rail Choice", "World Raid", "PlayerPrefs"
        };
    }
}
#endif
