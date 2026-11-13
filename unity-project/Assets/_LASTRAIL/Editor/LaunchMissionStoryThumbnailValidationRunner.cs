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
    public static class LaunchMissionStoryThumbnailValidationRunner
    {
        private const string OutputDirectoryName = "LastRailMissionStoryThumbnailQa";
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-mission-story-thumbnails.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/MissionStoryThumbnails/README.md";
        private const string MissionShellPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab";
        private const string StoryProgressPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/StoryProgressSummaryScreen.prefab";
        private const string CodexArchivePrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/CodexArchiveScreen.prefab";

        public static void ValidateMissionStoryThumbnailResources()
        {
            var failures = new List<string>();
            var warnings = new List<string>();

            ValidateManifestAndReadme(failures);
            ValidateTextures(failures);
            ValidateShowcase(failures);
            ValidateMissionShellBinding(failures);
            ValidateStoryProgressAndCodexBindings(failures);

            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", OutputDirectoryName);
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, "mission-story-thumbnail-validation.json");
            File.WriteAllText(outputPath, BuildJson(failures, warnings));

            if (failures.Count > 0)
            {
                Debug.LogError("LASTRAIL mission/story thumbnail validation FAILED: " + outputPath);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("LASTRAIL mission/story thumbnail validation PASS: " + outputPath);
            EditorApplication.Exit(0);
        }

        private static void ValidateManifestAndReadme(List<string> failures)
        {
            var manifestFullPath = ProjectPath(ManifestPath);
            var readmeFullPath = ProjectPath(ReadmePath);
            RequireFile(manifestFullPath, "manifest", failures);
            RequireFile(readmeFullPath, "readme", failures);

            var combined = ReadIfExists(manifestFullPath) + "\n" + ReadIfExists(readmeFullPath);
            RequireContains(combined, "\"missionCount\": 7", "mission count", failures);
            RequireContains(combined, "\"ordinaryCount\": 6", "ordinary mission count", failures);
            RequireContains(combined, "\"storyRaidCount\": 1", "story raid count", failures);
            RequireContains(combined, "Unity procedural texture builder", "source note", failures);
            RequireContains(combined, "presentation-only", "presentation boundary", failures);

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
            foreach (var mission in LaunchMissionStoryThumbnailResourceBuilder.Missions)
            {
                ValidateSprite(mission.ThumbnailPath, 1024, 576, failures);
            }

            foreach (var overlay in LaunchMissionStoryThumbnailResourceBuilder.Overlays)
            {
                ValidateSprite(overlay.Path, overlay.Width, overlay.Height, failures);
            }
        }

        private static void ValidateShowcase(List<string> failures)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LaunchMissionStoryThumbnailResourceBuilder.ShowcasePrefabPath);
            if (prefab == null)
            {
                failures.Add("missing showcase prefab");
                return;
            }

            ValidateCommonUiBoundaries(prefab, "showcase", failures);

            foreach (var mission in LaunchMissionStoryThumbnailResourceBuilder.Missions)
            {
                RequireSprite(prefab, mission.MissionId + "_thumbnail", failures);
            }

            RequireSprite(prefab, "mission_state_locked_overlay", failures);
            RequireSprite(prefab, "mission_state_available_accent", failures);
            RequireSprite(prefab, "mission_state_cleared_stamp", failures);
            RequireSprite(prefab, "mission_state_first_clear_badge", failures);
            RequireSprite(prefab, "mission_state_risk_accent", failures);
            RequireSprite(prefab, "mission_state_boss_only_badge", failures);

            if (prefab.GetComponentsInChildren<TMP_Text>(true).Length < 8)
            {
                failures.Add("showcase has too few TMP text surfaces");
            }

            if (prefab.GetComponentsInChildren<Image>(true).Length < 20)
            {
                failures.Add("showcase has too few image surfaces");
            }
        }

        private static void ValidateMissionShellBinding(List<string> failures)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MissionShellPrefabPath);
            if (prefab == null)
            {
                failures.Add("missing MissionShell prefab");
                return;
            }

            ValidateCommonUiBoundaries(prefab, "mission shell", failures);
            foreach (var mission in LaunchMissionStoryThumbnailResourceBuilder.Missions)
            {
                RequireSprite(prefab, mission.MissionId + "_thumbnail", failures);
            }
        }

        private static void ValidateStoryProgressAndCodexBindings(List<string> failures)
        {
            ValidateMissionThumbnailConsumer(StoryProgressPrefabPath, "story progress", failures, true);
            ValidateMissionThumbnailConsumer(CodexArchivePrefabPath, "codex archive", failures, false);
        }

        private static void ValidateMissionThumbnailConsumer(string prefabPath, string label, List<string> failures, bool expectsProgressOverlays)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                failures.Add("missing " + label + " prefab");
                return;
            }

            ValidateCommonUiBoundaries(prefab, label, failures);
            foreach (var mission in LaunchMissionStoryThumbnailResourceBuilder.Missions)
            {
                RequireSprite(prefab, mission.MissionId + "_thumbnail", failures);
            }

            if (expectsProgressOverlays)
            {
                RequireSprite(prefab, "mission_state_available_accent", failures);
                RequireSprite(prefab, "mission_state_cleared_stamp", failures);
                RequireSprite(prefab, "mission_state_first_clear_badge", failures);
            }
            else
            {
                RequireSprite(prefab, "mission_state_locked_overlay", failures);
                RequireSprite(prefab, "mission_state_boss_only_badge", failures);
            }
        }

        private static void ValidateCommonUiBoundaries(GameObject prefab, string label, List<string> failures)
        {
            if (prefab.GetComponentInChildren<UnityEngine.UI.Text>(true) != null)
            {
                failures.Add(label + " contains legacy Text");
            }

            if (prefab.GetComponentInChildren<StandaloneInputModule>(true) != null)
            {
                failures.Add(label + " contains StandaloneInputModule");
            }
        }

        private static void RequireSprite(GameObject prefab, string spriteName, List<string> failures)
        {
            foreach (var image in prefab.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != null && image.sprite.name == spriteName)
                {
                    if (image.raycastTarget)
                    {
                        failures.Add("visual sprite blocks touch: " + spriteName);
                    }

                    return;
                }
            }

            failures.Add("missing sprite binding: " + spriteName);
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
                failures.Add("missing importer: " + assetPath);
                return;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                failures.Add(assetPath + " is not a Sprite");
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
                "\"missionCount\":7," +
                "\"ordinaryCount\":6," +
                "\"storyRaidCount\":1," +
                "\"overlayCount\":6," +
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
