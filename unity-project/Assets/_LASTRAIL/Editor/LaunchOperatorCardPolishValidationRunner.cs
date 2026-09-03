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
    public static class LaunchOperatorCardPolishValidationRunner
    {
        private const string OutputDirectoryName = "LastRailOperatorCardPolishQa";
        private const string Root = "Assets/_LASTRAIL/UI/Operators/CardPolish";
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-operator-card-polish-resources.json";
        private const string ReadmePath = Root + "/README.md";
        private const string ShowcasePrefabPath = Root + "/Showcase/LRLaunchOperatorCardPolishShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchOperatorCardPolishShowcase.unity";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        public static void ValidateOperatorCardPolishResources()
        {
            var failures = new List<string>();
            var warnings = new List<string>();

            try
            {
                ValidateManifestAndReadme(failures);
                ValidateResources(failures);
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

            var outputRoot = Path.Combine(Path.GetTempPath(), OutputDirectoryName);
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, "operator-card-polish-validation.json");
            File.WriteAllText(outputPath, BuildJson(failures, warnings));

            if (failures.Count > 0)
            {
                Debug.LogError("LASTRAIL operator card polish validation FAILED: " + outputPath);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("LASTRAIL operator card polish validation PASS: " + outputPath);
            EditorApplication.Exit(0);
        }

        private static void ValidateManifestAndReadme(List<string> failures)
        {
            RequireFile(ProjectPath(ManifestPath), "manifest", failures);
            RequireFile(ProjectPath(ReadmePath), "readme", failures);

            var combined = ReadIfExists(ProjectPath(ManifestPath)) + "\n" + ReadIfExists(ProjectPath(ReadmePath));
            RequireContains(combined, "\"resourceCount\": 21", "resource count", failures);
            RequireContains(combined, "\"operatorVisualScopeCount\": 15", "operator visual scope", failures);
            RequireContains(combined, "\"initialFreeCount\": 5", "initial/free scope", failures);
            RequireContains(combined, "\"gachaEliteCount\": 10", "gacha elite scope", failures);
            RequireContains(combined, "\"presentationOnly\": true", "presentation boundary", failures);
            RequireContains(combined, "\"qualityClass\": \"HIGH_QUALITY_2D_UI_SKIN\"", "high fidelity quality class", failures);
            RequireContains(combined, "\"sourceType\": \"generated_high_fidelity_local_launch_ui_skin\"", "high fidelity source note", failures);
            RequireContains(combined, "low-fidelity", "replacement note", failures);
            RequireContains(combined, "External paid asset dependency: none", "external asset note", failures);

            if (combined.Contains("procedurally generated local launch UI assets", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("operator card polish still uses old procedural placeholder source note");
            }

            if (combined.Contains("PROCEDURAL_PLACEHOLDER", StringComparison.OrdinalIgnoreCase) ||
                combined.Contains("LOW_FIDELITY", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("operator card polish still contains placeholder/low-fidelity quality class");
            }
        }

        private static void ValidateResources(List<string> failures)
        {
            foreach (var spec in ExpectedResources)
            {
                ValidateSprite(spec.Path, spec.Width, spec.Height, failures);
            }
        }

        private static void ValidateShowcase(List<string> failures)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            if (prefab == null)
            {
                failures.Add("missing showcase prefab");
                return;
            }

            var scaler = prefab.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.referenceResolution != new Vector2(1080f, 1920f))
            {
                failures.Add("showcase reference resolution mismatch");
            }

            if (prefab.GetComponentsInChildren<Text>(true).Length > 0)
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

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != font)
                {
                    failures.Add("unexpected showcase font: " + text.name);
                }

                if (text.fontSharedMaterial != material)
                {
                    failures.Add("unexpected showcase material: " + text.name);
                }

                if (text.overflowMode != TextOverflowModes.Ellipsis && text.overflowMode != TextOverflowModes.Truncate)
                {
                    failures.Add("text overflow policy missing: " + text.name);
                }
            }

            if (prefab.GetComponentsInChildren<Image>(true).Length < 80)
            {
                failures.Add("showcase image coverage too low");
            }

            if (prefab.transform.Find("EventSystem") == null)
            {
                failures.Add("showcase missing EventSystem child");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ShowcaseScenePath) == null)
            {
                failures.Add("missing showcase scene");
            }
        }

        private static void ValidateForbiddenText(List<string> failures)
        {
            var roots = new[]
            {
                ProjectPath(Root),
                ProjectPath(ManifestPath)
            };

            foreach (var root in roots)
            {
                if (File.Exists(root))
                {
                    ValidateForbiddenFile(root, failures);
                    continue;
                }

                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    ValidateForbiddenFile(file, failures);
                }
            }
        }

        private static void ValidateForbiddenFile(string file, List<string> failures)
        {
            if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var text = File.ReadAllText(file);
            foreach (var token in ForbiddenTokens)
            {
                if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add(file + " contains forbidden boundary token: " + token);
                }
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

            if (importer.mipmapEnabled)
            {
                failures.Add(assetPath + " has mipmaps enabled");
            }

            if (importer.maxTextureSize > 2048)
            {
                failures.Add(assetPath + " max texture size exceeds 2048");
            }

            if (assetPath.EndsWith("operator_card_slot_background.png", StringComparison.Ordinal) ||
                assetPath.EndsWith("operator_card_detail_panel_accent.png", StringComparison.Ordinal) ||
                assetPath.EndsWith("operator_grid_section_rail.png", StringComparison.Ordinal))
            {
                ValidateHighFidelityTexture(assetPath, failures);
            }
        }

        private static void ValidateHighFidelityTexture(string assetPath, List<string> failures)
        {
            var absolutePath = ProjectPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                failures.Add("missing high fidelity texture: " + assetPath);
                return;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePath)))
                {
                    failures.Add("could not decode high fidelity texture: " + assetPath);
                    return;
                }

                var pixels = texture.GetPixels32();
                var opaqueCount = 0;
                var vividCount = 0;
                var transitions = 0;
                var colors = new HashSet<int>();
                var previous = default(Color32);
                for (var i = 0; i < pixels.Length; i++)
                {
                    var pixel = pixels[i];
                    if (pixel.a < 32)
                    {
                        continue;
                    }

                    opaqueCount++;
                    var max = Math.Max(pixel.r, Math.Max(pixel.g, pixel.b));
                    var min = Math.Min(pixel.r, Math.Min(pixel.g, pixel.b));
                    if (max - min > 42)
                    {
                        vividCount++;
                    }

                    colors.Add((pixel.r / 8 << 16) | (pixel.g / 8 << 8) | (pixel.b / 8));
                    if (i > 0)
                    {
                        var delta = Math.Abs(pixel.r - previous.r) + Math.Abs(pixel.g - previous.g) + Math.Abs(pixel.b - previous.b);
                        if (delta > 54)
                        {
                            transitions++;
                        }
                    }

                    previous = pixel;
                }

                if (opaqueCount <= texture.width * texture.height / 3)
                {
                    failures.Add(assetPath + " has too little opaque detail coverage");
                }

                if (colors.Count <= 120)
                {
                    failures.Add(assetPath + " has low quantized color variety");
                }

                if (vividCount <= opaqueCount / 32)
                {
                    failures.Add(assetPath + " has weak accent/detail coverage");
                }

                if (transitions <= Math.Min(texture.width * 6, (texture.width + texture.height) * 2))
                {
                    failures.Add(assetPath + " has too few edge/detail transitions");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
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
                "\"resourceCount\":21," +
                "\"operatorVisualScopeCount\":15," +
                "\"showcaseScreen\":\"operator_card_grid\"," +
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

        private static readonly ExpectedResource[] ExpectedResources =
        {
            new ExpectedResource("operator_card_slot_background", Root + "/Frames/operator_card_slot_background.png", 512, 720),
            new ExpectedResource("operator_card_selected_frame", Root + "/Frames/operator_card_selected_frame.png", 512, 720),
            new ExpectedResource("operator_card_locked_overlay", Root + "/Frames/operator_card_locked_overlay.png", 512, 720),
            new ExpectedResource("operator_card_detail_panel_accent", Root + "/Accents/operator_card_detail_panel_accent.png", 1024, 256),
            new ExpectedResource("operator_badge_new", Root + "/Badges/operator_badge_new.png", 256, 96),
            new ExpectedResource("operator_badge_owned", Root + "/Badges/operator_badge_owned.png", 256, 96),
            new ExpectedResource("operator_badge_level", Root + "/Badges/operator_badge_level.png", 256, 96),
            new ExpectedResource("operator_badge_upgrade", Root + "/Badges/operator_badge_upgrade.png", 256, 96),
            new ExpectedResource("operator_rarity_initial_free", Root + "/Rarity/operator_rarity_initial_free.png", 256, 96),
            new ExpectedResource("operator_rarity_rare", Root + "/Rarity/operator_rarity_rare.png", 256, 96),
            new ExpectedResource("operator_rarity_elite", Root + "/Rarity/operator_rarity_elite.png", 256, 96),
            new ExpectedResource("operator_rarity_pickup_elite", Root + "/Rarity/operator_rarity_pickup_elite.png", 256, 96),
            new ExpectedResource("operator_role_striker", Root + "/RoleIcons/operator_role_striker.png", 128, 128),
            new ExpectedResource("operator_role_guard", Root + "/RoleIcons/operator_role_guard.png", 128, 128),
            new ExpectedResource("operator_role_medic", Root + "/RoleIcons/operator_role_medic.png", 128, 128),
            new ExpectedResource("operator_role_engineer", Root + "/RoleIcons/operator_role_engineer.png", 128, 128),
            new ExpectedResource("operator_role_support", Root + "/RoleIcons/operator_role_support.png", 128, 128),
            new ExpectedResource("operator_state_locked_label_plate", Root + "/Overlays/operator_state_locked_label_plate.png", 256, 96),
            new ExpectedResource("operator_state_available_label_plate", Root + "/Overlays/operator_state_available_label_plate.png", 256, 96),
            new ExpectedResource("operator_grid_section_rail", Root + "/Accents/operator_grid_section_rail.png", 1024, 96),
            new ExpectedResource("operator_grid_safe_mask", Root + "/Overlays/operator_grid_safe_mask.png", 512, 720)
        };

        private static readonly string[] ForbiddenTokens =
        {
            "accessToken",
            "refreshToken",
            "idToken",
            "accountId",
            "Authorization",
            "rawRng",
            "probability",
            "fullWeights",
            "rewardAmount",
            "economyGrant",
            "itemGrant",
            "storyProgressTruth",
            "unlockTruth",
            "server payload",
            "Rail Choice",
            "World Raid",
            "PlayerPrefs"
        };

        private readonly struct ExpectedResource
        {
            public ExpectedResource(string id, string path, int width, int height)
            {
                Id = id;
                Path = path;
                Width = width;
                Height = height;
            }

            public string Id { get; }
            public string Path { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }
}
#endif
